using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.AssetManagement;

namespace Vareiko.Foundation.Tests.AssetManagement
{
    public sealed class AddressablesAssetProviderTests
    {
        [Test]
        public void ProviderType_IsAddressables()
        {
            AddressablesAssetProvider provider = new AddressablesAssetProvider();
            Assert.That(provider.ProviderType, Is.EqualTo(AssetProviderType.Addressables));
        }

        [Test]
        public async Task LoadAsync_WithEmptyKey_ReturnsFailure()
        {
            AddressablesAssetProvider provider = new AddressablesAssetProvider();

            AssetLoadResult<Texture2D> result = await provider.LoadAsync<Texture2D>(string.Empty);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Asset, Is.Null);
            Assert.That(result.Error, Does.Contain("Asset key is null or empty"));
        }

        [Test]
        public async Task LoadAsync_WithMissingKey_ReturnsExpectedFallbackWithoutAddressablesDefine()
        {
            AddressablesAssetProvider provider = new AddressablesAssetProvider();

            AssetLoadResult<Texture2D> result = await provider.LoadAsync<Texture2D>("__missing_addressable_key__");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Asset, Is.Null);
#if FOUNDATION_ADDRESSABLES
            Assert.That(result.Error, Does.Contain("failed to load asset"));
#else
            Assert.That(result.Error, Does.Contain("Addressables support is disabled"));
#endif
        }

        [Test]
        public async Task WarmupAsync_WithEmptyCollection_CompletesWithoutErrors()
        {
            AddressablesAssetProvider provider = new AddressablesAssetProvider();
            await provider.WarmupAsync(new List<string>(0));
            Assert.Pass();
        }

        [Test]
        public void ReleaseAsync_WithCanceledToken_Throws()
        {
            AddressablesAssetProvider provider = new AddressablesAssetProvider();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<System.OperationCanceledException>(async () => await provider.ReleaseAsync(null, cts.Token));
        }
    }
}
