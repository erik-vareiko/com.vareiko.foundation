using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Iap
{
    public sealed class SimulatedInAppPurchaseServiceTests
    {
        [Test]
        public async Task Initialize_WithValidConfig_Succeeds_AndLoadsCatalog()
        {
            IapConfig config = CreateSimulatedConfig();
            try
            {
                SimulatedInAppPurchaseService service = new SimulatedInAppPurchaseService(config, null);
                InAppPurchaseInitializeResult init = await service.InitializeAsync();
                IReadOnlyList<InAppPurchaseProductInfo> catalog = await service.GetCatalogAsync();

                Assert.That(init.Success, Is.True);
                Assert.That(service.IsInitialized, Is.True);
                Assert.That(catalog.Count, Is.EqualTo(2));
                Assert.That(catalog[0].ProductId, Is.EqualTo("coins.small"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task Purchase_WhenNotInitialized_Fails()
        {
            IapConfig config = CreateSimulatedConfig();
            try
            {
                SimulatedInAppPurchaseService service = new SimulatedInAppPurchaseService(config, null);
                InAppPurchaseResult result = await service.PurchaseAsync("coins.small");

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(InAppPurchaseErrorCode.NotInitialized));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task Purchase_NonConsumableTwice_FailsWithAlreadyOwned()
        {
            IapConfig config = CreateSimulatedConfig();
            try
            {
                SimulatedInAppPurchaseService service = new SimulatedInAppPurchaseService(config, null);
                await service.InitializeAsync();

                InAppPurchaseResult first = await service.PurchaseAsync("premium.unlock");
                InAppPurchaseResult second = await service.PurchaseAsync("premium.unlock");

                Assert.That(first.Success, Is.True);
                Assert.That(second.Success, Is.False);
                Assert.That(second.ErrorCode, Is.EqualTo(InAppPurchaseErrorCode.AlreadyOwned));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task RestorePurchases_ReturnsOwnedNonConsumables_AndFiresSignals()
        {
            IapConfig config = CreateSimulatedConfig();
            SignalBus signalBus = CreateSignalBus();
            int restoredSignals = 0;
            int restoredCountFromSignal = 0;
            signalBus.Subscribe<IapPurchaseSucceededSignal>(signal =>
            {
                if (signal.IsRestored)
                {
                    restoredSignals++;
                }
            });
            signalBus.Subscribe<IapRestoreCompletedSignal>(signal => restoredCountFromSignal = signal.RestoredCount);

            try
            {
                SimulatedInAppPurchaseService service = new SimulatedInAppPurchaseService(config, signalBus);
                await service.InitializeAsync();
                await service.PurchaseAsync("coins.small");
                await service.PurchaseAsync("premium.unlock");

                InAppPurchaseRestoreResult restore = await service.RestorePurchasesAsync();

                Assert.That(restore.Success, Is.True);
                Assert.That(restore.RestoredCount, Is.EqualTo(1));
                Assert.That(restoredSignals, Is.EqualTo(1));
                Assert.That(restoredCountFromSignal, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task NullService_ReturnsProviderUnavailableFailures()
        {
            NullInAppPurchaseService service = new NullInAppPurchaseService();

            InAppPurchaseInitializeResult init = await service.InitializeAsync();
            InAppPurchaseResult purchase = await service.PurchaseAsync("coins.small");
            InAppPurchaseRestoreResult restore = await service.RestorePurchasesAsync();

            Assert.That(init.Success, Is.False);
            Assert.That(purchase.Success, Is.False);
            Assert.That(restore.Success, Is.False);
            Assert.That(purchase.ErrorCode, Is.EqualTo(InAppPurchaseErrorCode.ProviderUnavailable));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<IapInitializedSignal>();
            container.DeclareSignal<IapPurchaseSucceededSignal>();
            container.DeclareSignal<IapPurchaseFailedSignal>();
            container.DeclareSignal<IapRestoreCompletedSignal>();
            container.DeclareSignal<IapRestoreFailedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static IapConfig CreateSimulatedConfig()
        {
            IapConfig config = ScriptableObject.CreateInstance<IapConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", InAppPurchaseProviderType.Simulated);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_simulateAlreadyOwnedAsFailure", true);

            IapConfig.ProductDefinition coins = new IapConfig.ProductDefinition();
            ReflectionTestUtil.SetPrivateField(coins, "_productId", "coins.small");
            ReflectionTestUtil.SetPrivateField(coins, "_productType", InAppPurchaseProductType.Consumable);
            ReflectionTestUtil.SetPrivateField(coins, "_localizedTitle", "Coins Small");
            ReflectionTestUtil.SetPrivateField(coins, "_price", 0.99d);

            IapConfig.ProductDefinition premium = new IapConfig.ProductDefinition();
            ReflectionTestUtil.SetPrivateField(premium, "_productId", "premium.unlock");
            ReflectionTestUtil.SetPrivateField(premium, "_productType", InAppPurchaseProductType.NonConsumable);
            ReflectionTestUtil.SetPrivateField(premium, "_localizedTitle", "Premium Unlock");
            ReflectionTestUtil.SetPrivateField(premium, "_price", 4.99d);

            ReflectionTestUtil.SetPrivateField(config, "_products", new List<IapConfig.ProductDefinition> { coins, premium });
            return config;
        }
    }
}
