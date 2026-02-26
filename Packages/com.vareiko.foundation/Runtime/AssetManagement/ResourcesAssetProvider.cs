using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vareiko.Foundation.AssetManagement
{
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        public AssetProviderType ProviderType => AssetProviderType.Resources;

        public async UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return AssetLoadResult<T>.Fail("Asset key is null or empty.");
            }

            ResourceRequest request = Resources.LoadAsync<T>(key);
            if (request == null)
            {
                return AssetLoadResult<T>.Fail($"Resources request failed to start. Key={key}");
            }

            while (!request.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            T asset = request.asset as T;
            if (asset == null)
            {
                return AssetLoadResult<T>.Fail($"Resource not found. Key={key}");
            }

            return AssetLoadResult<T>.Succeed(asset);
        }

        public UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

                ResourceRequest request = Resources.LoadAsync<Object>(key);
                if (request == null)
                {
                    continue;
                }

                while (!request.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
        }
    }
}
