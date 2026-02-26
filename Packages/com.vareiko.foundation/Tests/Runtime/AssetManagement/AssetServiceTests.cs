using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.AssetManagement;

namespace Vareiko.Foundation.Tests.AssetManagement
{
    public sealed class AssetServiceTests
    {
        [Test]
        public async Task LoadAndRelease_TracksReferenceCounts()
        {
            DummyAsset asset = ScriptableObject.CreateInstance<DummyAsset>();
            FakeAssetProvider provider = new FakeAssetProvider(asset);
            AssetService service = new AssetService(new List<IAssetProvider> { provider }, null, null);

            AssetLoadResult<DummyAsset> first = await service.LoadAsync<DummyAsset>("hero");
            AssetLoadResult<DummyAsset> second = await service.LoadAsync<DummyAsset>("hero");

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(service.TrackedAssetCount, Is.EqualTo(1));
            Assert.That(service.TotalReferenceCount, Is.EqualTo(2));

            bool firstRelease = await service.ReleaseAsync(asset);
            Assert.That(firstRelease, Is.True);
            Assert.That(service.TrackedAssetCount, Is.EqualTo(1));
            Assert.That(service.TotalReferenceCount, Is.EqualTo(1));
            Assert.That(provider.ReleaseCalls, Is.EqualTo(0));

            bool secondRelease = await service.ReleaseAsync(asset);
            Assert.That(secondRelease, Is.True);
            Assert.That(service.TrackedAssetCount, Is.EqualTo(0));
            Assert.That(service.TotalReferenceCount, Is.EqualTo(0));
            Assert.That(provider.ReleaseCalls, Is.EqualTo(1));

            service.Dispose();
            Object.DestroyImmediate(asset);
        }

        [Test]
        public async Task Dispose_WhenLeakedAssetsExist_ReleasesProviderAssets()
        {
            DummyAsset asset = ScriptableObject.CreateInstance<DummyAsset>();
            FakeAssetProvider provider = new FakeAssetProvider(asset);
            AssetService service = new AssetService(new List<IAssetProvider> { provider }, null, null);

            AssetLoadResult<DummyAsset> loaded = await service.LoadAsync<DummyAsset>("hero");
            Assert.That(loaded.Success, Is.True);
            Assert.That(service.TrackedAssetCount, Is.EqualTo(1));

            service.Dispose();

            Assert.That(provider.ReleaseCalls, Is.EqualTo(1));
            Assert.That(service.TrackedAssetCount, Is.EqualTo(0));
            Assert.That(service.TotalReferenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(asset);
        }

        private sealed class DummyAsset : ScriptableObject
        {
        }

        private sealed class FakeAssetProvider : IAssetProvider
        {
            private readonly Object _asset;

            public FakeAssetProvider(Object asset)
            {
                _asset = asset;
            }

            public int ReleaseCalls { get; private set; }
            public AssetProviderType ProviderType => AssetProviderType.Resources;

            public UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                T typed = _asset as T;
                return UniTask.FromResult(typed != null
                    ? AssetLoadResult<T>.Succeed(typed)
                    : AssetLoadResult<T>.Fail("Type mismatch."));
            }

            public UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReleaseCalls++;
                return UniTask.FromResult(true);
            }

            public UniTask WarmupAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
