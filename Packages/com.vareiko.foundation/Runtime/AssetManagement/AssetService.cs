using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.AssetManagement
{
    public sealed class AssetService : IAssetService, System.IDisposable
    {
        private sealed class AssetReferenceEntry
        {
            public Object Asset;
            public string Key;
            public int ReferenceCount;
        }

        private readonly List<IAssetProvider> _providers;
        private readonly SignalBus _signalBus;
        private readonly AssetProviderType _activeProvider;
        private readonly Dictionary<int, AssetReferenceEntry> _assetRefs = new Dictionary<int, AssetReferenceEntry>();
        private readonly Dictionary<string, int> _keyRefs = new Dictionary<string, int>(System.StringComparer.Ordinal);

        private int _totalReferenceCount;

        [Inject]
        public AssetService(
            [InjectOptional] List<IAssetProvider> providers = null,
            [InjectOptional] AssetServiceConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _providers = providers ?? new List<IAssetProvider>(0);
            _signalBus = signalBus;
            _activeProvider = config != null ? config.Provider : AssetProviderType.Resources;
        }

        public AssetProviderType ActiveProvider => _activeProvider;
        public int TrackedAssetCount => _assetRefs.Count;
        public int TotalReferenceCount => _totalReferenceCount;

        public async UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
        {
            IAssetProvider provider = ResolveProvider();
            if (provider == null)
            {
                AssetLoadResult<T> fail = AssetLoadResult<T>.Fail("No asset provider is configured.");
                _signalBus?.Fire(new AssetLoadFailedSignal(key, fail.Error));
                return fail;
            }

            AssetLoadResult<T> loaded = await provider.LoadAsync<T>(key, cancellationToken);
            if (loaded.Success)
            {
                if (loaded.Asset != null)
                {
                    TrackLoadedAsset(key, loaded.Asset);
                }

                _signalBus?.Fire(new AssetLoadedSignal(key, typeof(T).Name));
            }
            else
            {
                _signalBus?.Fire(new AssetLoadFailedSignal(key, loaded.Error));
            }

            return loaded;
        }

        public async UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
        {
            if (asset == null)
            {
                return false;
            }

            IAssetProvider provider = ResolveProvider();
            int assetId = asset.GetInstanceID();
            AssetReferenceEntry entry;
            if (_assetRefs.TryGetValue(assetId, out entry))
            {
                entry.ReferenceCount--;
                _totalReferenceCount = Mathf.Max(0, _totalReferenceCount - 1);
                UpdateKeyReferenceCount(entry.Key, -1);

                if (entry.ReferenceCount <= 0)
                {
                    _assetRefs.Remove(assetId);
                    bool finalReleaseSuccess = provider != null && await provider.ReleaseAsync(asset, cancellationToken);
                    _signalBus?.Fire(new AssetReleasedSignal(entry.Key, 0, _assetRefs.Count));
                    return finalReleaseSuccess;
                }

                _signalBus?.Fire(new AssetReleasedSignal(entry.Key, entry.ReferenceCount, _assetRefs.Count));
                return true;
            }

            if (provider == null)
            {
                return false;
            }

            bool released = await provider.ReleaseAsync(asset, cancellationToken);
            _signalBus?.Fire(new AssetReleasedSignal(string.Empty, 0, _assetRefs.Count));
            return released;
        }

        public async UniTask WarmupAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
        {
            IAssetProvider provider = ResolveProvider();
            if (provider == null)
            {
                return;
            }

            await provider.WarmupAsync(keys, cancellationToken);
            int count = keys != null ? keys.Count : 0;
            _signalBus?.Fire(new AssetWarmupCompletedSignal(count));
        }

        public void Dispose()
        {
            if (_assetRefs.Count > 0)
            {
                IAssetProvider provider = ResolveProvider();
                List<AssetReferenceEntry> leakedEntries = new List<AssetReferenceEntry>(_assetRefs.Values);
                for (int i = 0; i < leakedEntries.Count; i++)
                {
                    AssetReferenceEntry leaked = leakedEntries[i];
                    _signalBus?.Fire(new AssetLeakDetectedSignal(leaked.Key, leaked.ReferenceCount));
                    if (provider != null && leaked.Asset != null)
                    {
                        _ = provider.ReleaseAsync(leaked.Asset);
                    }
                }
            }

            _assetRefs.Clear();
            _keyRefs.Clear();
            _totalReferenceCount = 0;
        }

        private void TrackLoadedAsset(string key, Object asset)
        {
            int assetId = asset.GetInstanceID();
            AssetReferenceEntry entry;
            if (_assetRefs.TryGetValue(assetId, out entry))
            {
                entry.ReferenceCount++;
            }
            else
            {
                entry = new AssetReferenceEntry
                {
                    Asset = asset,
                    Key = key ?? string.Empty,
                    ReferenceCount = 1
                };
                _assetRefs.Add(assetId, entry);
            }

            _totalReferenceCount++;
            UpdateKeyReferenceCount(entry.Key, 1);
            _signalBus?.Fire(new AssetReferenceChangedSignal(entry.Key, GetKeyReferenceCount(entry.Key), _assetRefs.Count));
        }

        private void UpdateKeyReferenceCount(string key, int delta)
        {
            string normalized = key ?? string.Empty;
            int current;
            _keyRefs.TryGetValue(normalized, out current);
            int next = current + delta;
            if (next <= 0)
            {
                _keyRefs.Remove(normalized);
            }
            else
            {
                _keyRefs[normalized] = next;
            }
        }

        private int GetKeyReferenceCount(string key)
        {
            int count;
            _keyRefs.TryGetValue(key ?? string.Empty, out count);
            return count;
        }

        private IAssetProvider ResolveProvider()
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                IAssetProvider provider = _providers[i];
                if (provider != null && provider.ProviderType == _activeProvider)
                {
                    return provider;
                }
            }

            for (int i = 0; i < _providers.Count; i++)
            {
                IAssetProvider provider = _providers[i];
                if (provider != null)
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
