using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Iap
{
    public sealed class UnityInAppPurchaseServiceTests
    {
#if !UNITY_PURCHASING
        [Test]
        public async Task Initialize_WithoutUnityPurchasingDefine_ReturnsProviderUnavailable()
        {
            IapConfig config = CreateUnityIapConfig();
            try
            {
                UnityInAppPurchaseService service = new UnityInAppPurchaseService(config, null);

                InAppPurchaseInitializeResult init = await service.InitializeAsync();

                Assert.That(init.Success, Is.False);
                Assert.That(init.ErrorCode, Is.EqualTo(InAppPurchaseErrorCode.ProviderUnavailable));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
#endif

        [Test]
        public async Task Purchase_WhenNotInitialized_ReturnsNotInitialized()
        {
            IapConfig config = CreateUnityIapConfig();
            try
            {
                UnityInAppPurchaseService service = new UnityInAppPurchaseService(config, null);

                InAppPurchaseResult purchase = await service.PurchaseAsync("coins.small");

                Assert.That(purchase.Success, Is.False);
                Assert.That(purchase.ErrorCode, Is.EqualTo(InAppPurchaseErrorCode.NotInitialized));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Installer_WhenProviderUnityIap_BindsUnityIapService()
        {
            IapConfig config = CreateUnityIapConfig();
            try
            {
                DiContainer container = new DiContainer();

                FoundationIapInstaller.Install(container, config);

                IInAppPurchaseService service = container.Resolve<IInAppPurchaseService>();
                Assert.That(service, Is.TypeOf<UnityInAppPurchaseService>());
                Assert.That(service.Provider, Is.EqualTo(InAppPurchaseProviderType.UnityIap));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static IapConfig CreateUnityIapConfig()
        {
            IapConfig config = ScriptableObject.CreateInstance<IapConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", InAppPurchaseProviderType.UnityIap);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_simulateAlreadyOwnedAsFailure", true);

            IapConfig.ProductDefinition coins = new IapConfig.ProductDefinition();
            ReflectionTestUtil.SetPrivateField(coins, "_productId", "coins.small");
            ReflectionTestUtil.SetPrivateField(coins, "_productType", InAppPurchaseProductType.Consumable);
            ReflectionTestUtil.SetPrivateField(coins, "_localizedTitle", "Coins Small");
            ReflectionTestUtil.SetPrivateField(coins, "_price", 0.99d);

            ReflectionTestUtil.SetPrivateField(config, "_products", new List<IapConfig.ProductDefinition> { coins });
            return config;
        }
    }
}
