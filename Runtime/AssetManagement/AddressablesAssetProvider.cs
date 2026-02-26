using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vareiko.Foundation.AssetManagement
{
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        public AssetProviderType ProviderType => AssetProviderType.Addressables;

        public async UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return AssetLoadResult<T>.Fail("Asset key is null or empty.");
            }

#if FOUNDATION_ADDRESSABLES
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<T> handle =
                UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);

            while (!handle.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                return AssetLoadResult<T>.Fail($"Addressables failed to load asset. Key={key}");
            }

            return AssetLoadResult<T>.Succeed(handle.Result);
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return AssetLoadResult<T>.Fail("Addressables support is disabled. Define FOUNDATION_ADDRESSABLES and install Addressables.");
#endif
        }

        public UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if FOUNDATION_ADDRESSABLES
            if (asset != null)
            {
                UnityEngine.AddressableAssets.Addressables.Release(asset);
            }
#endif
            return UniTask.FromResult(true);
        }

        public async UniTask WarmupAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Count == 0)
            {
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                AssetLoadResult<Object> loaded = await LoadAsync<Object>(key, cancellationToken);
                if (loaded.Success && loaded.Asset != null)
                {
                    await ReleaseAsync(loaded.Asset, cancellationToken);
                }
            }
        }
    }
}
