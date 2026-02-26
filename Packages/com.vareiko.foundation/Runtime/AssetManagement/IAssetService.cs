using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vareiko.Foundation.AssetManagement
{
    public interface IAssetService
    {
        AssetProviderType ActiveProvider { get; }
        int TrackedAssetCount { get; }
        int TotalReferenceCount { get; }
        UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object;
        UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default);
        UniTask WarmupAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default);
    }
}
