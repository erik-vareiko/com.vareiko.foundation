using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.AssetManagement
{
    public sealed class AssetService : IAssetService
    {
        private readonly List<IAssetProvider> _providers;
        private readonly SignalBus _signalBus;
        private readonly AssetProviderType _activeProvider;

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
                _signalBus?.Fire(new AssetLoadedSignal(key, typeof(T).Name));
            }
            else
            {
                _signalBus?.Fire(new AssetLoadFailedSignal(key, loaded.Error));
            }

            return loaded;
        }

        public UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
        {
            IAssetProvider provider = ResolveProvider();
            if (provider == null)
            {
                return UniTask.FromResult(false);
            }

            return provider.ReleaseAsync(asset, cancellationToken);
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
