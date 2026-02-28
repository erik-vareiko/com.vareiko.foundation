using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Monetization;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Monetization
{
    public sealed class MonetizationPolicyServiceTests
    {
        [Test]
        public async Task CanShowAd_EnforcesCooldownAndSessionCap()
        {
            MonetizationPolicyConfig config = CreateConfig();
            try
            {
                FakeTimeProvider time = new FakeTimeProvider { UnscaledTime = 0f };
                MonetizationPolicyService service = new MonetizationPolicyService(time, config, null);
                service.Initialize();

                MonetizationAdDecision first = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                Assert.That(first.Allowed, Is.True);

                await service.RecordAdShownAsync("interstitial.default", AdPlacementType.Interstitial);
                time.UnscaledTime = 5f;
                MonetizationAdDecision cooldown = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                Assert.That(cooldown.Allowed, Is.False);
                Assert.That(cooldown.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.CooldownActive));
                Assert.That(cooldown.RetryAfterSeconds, Is.GreaterThan(0f));

                time.UnscaledTime = 11f;
                MonetizationAdDecision second = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                Assert.That(second.Allowed, Is.True);
                await service.RecordAdShownAsync("interstitial.default", AdPlacementType.Interstitial);

                time.UnscaledTime = 22f;
                MonetizationAdDecision capped = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                Assert.That(capped.Allowed, Is.False);
                Assert.That(capped.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.SessionCapReached));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task CanShowAd_WhenExplicitPolicyRequired_BlocksUnknownPlacement()
        {
            MonetizationPolicyConfig config = CreateConfig(requireExplicitPlacement: true);
            try
            {
                FakeTimeProvider time = new FakeTimeProvider();
                MonetizationPolicyService service = new MonetizationPolicyService(time, config, null);
                service.Initialize();

                MonetizationAdDecision decision = await service.CanShowAdAsync("unknown.placement", AdPlacementType.Interstitial);

                Assert.That(decision.Allowed, Is.False);
                Assert.That(decision.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.PlacementNotConfigured));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task CanStartPurchase_EnforcesCooldownAndSessionCap()
        {
            MonetizationPolicyConfig config = CreateConfig();
            try
            {
                FakeTimeProvider time = new FakeTimeProvider { UnscaledTime = 0f };
                MonetizationPolicyService service = new MonetizationPolicyService(time, config, null);
                service.Initialize();

                MonetizationIapDecision first = await service.CanStartPurchaseAsync("coins.pack");
                Assert.That(first.Allowed, Is.True);

                await service.RecordPurchaseAsync("coins.pack");
                time.UnscaledTime = 1f;
                MonetizationIapDecision cooldown = await service.CanStartPurchaseAsync("coins.pack");
                Assert.That(cooldown.Allowed, Is.False);
                Assert.That(cooldown.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.CooldownActive));

                time.UnscaledTime = 3f;
                MonetizationIapDecision second = await service.CanStartPurchaseAsync("coins.pack");
                Assert.That(second.Allowed, Is.True);
                await service.RecordPurchaseAsync("coins.pack");

                time.UnscaledTime = 6f;
                MonetizationIapDecision capped = await service.CanStartPurchaseAsync("coins.pack");
                Assert.That(capped.Allowed, Is.False);
                Assert.That(capped.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.SessionCapReached));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task CanStartPurchase_WhenExplicitPolicyRequired_BlocksUnknownProduct()
        {
            MonetizationPolicyConfig config = CreateConfig(requireExplicitProduct: true);
            try
            {
                FakeTimeProvider time = new FakeTimeProvider();
                MonetizationPolicyService service = new MonetizationPolicyService(time, config, null);
                service.Initialize();

                MonetizationIapDecision decision = await service.CanStartPurchaseAsync("unknown.product");

                Assert.That(decision.Allowed, Is.False);
                Assert.That(decision.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.ProductNotConfigured));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task ResetSession_ClearsCountersAndAllowsFlowAgain()
        {
            MonetizationPolicyConfig config = CreateConfig();
            try
            {
                FakeTimeProvider time = new FakeTimeProvider { UnscaledTime = 0f };
                MonetizationPolicyService service = new MonetizationPolicyService(time, config, null);
                service.Initialize();

                await service.RecordAdShownAsync("interstitial.default", AdPlacementType.Interstitial);
                await service.RecordAdShownAsync("interstitial.default", AdPlacementType.Interstitial);
                MonetizationAdDecision blockedBeforeReset = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                Assert.That(blockedBeforeReset.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.SessionCapReached));

                await service.RecordPurchaseAsync("coins.pack");
                await service.RecordPurchaseAsync("coins.pack");
                MonetizationIapDecision iapBlockedBeforeReset = await service.CanStartPurchaseAsync("coins.pack");
                Assert.That(iapBlockedBeforeReset.BlockReason, Is.EqualTo(MonetizationPolicyBlockReason.SessionCapReached));

                await service.ResetSessionAsync();
                time.UnscaledTime = 100f;

                MonetizationAdDecision adAfterReset = await service.CanShowAdAsync("interstitial.default", AdPlacementType.Interstitial);
                MonetizationIapDecision iapAfterReset = await service.CanStartPurchaseAsync("coins.pack");

                Assert.That(adAfterReset.Allowed, Is.True);
                Assert.That(iapAfterReset.Allowed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static MonetizationPolicyConfig CreateConfig(bool requireExplicitPlacement = false, bool requireExplicitProduct = false)
        {
            MonetizationPolicyConfig config = ScriptableObject.CreateInstance<MonetizationPolicyConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_requireExplicitPlacementPolicy", requireExplicitPlacement);
            ReflectionTestUtil.SetPrivateField(config, "_requireExplicitProductPolicy", requireExplicitProduct);
            ReflectionTestUtil.SetPrivateField(config, "_defaultInterstitialCooldownSeconds", 10f);
            ReflectionTestUtil.SetPrivateField(config, "_defaultInterstitialMaxShowsPerSession", 2);
            ReflectionTestUtil.SetPrivateField(config, "_defaultRewardedCooldownSeconds", 1f);
            ReflectionTestUtil.SetPrivateField(config, "_defaultRewardedMaxShowsPerSession", 5);
            ReflectionTestUtil.SetPrivateField(config, "_defaultIapCooldownSeconds", 2f);
            ReflectionTestUtil.SetPrivateField(config, "_defaultIapMaxPurchasesPerSession", 2);

            MonetizationPolicyConfig.PlacementPolicy interstitial = new MonetizationPolicyConfig.PlacementPolicy();
            ReflectionTestUtil.SetPrivateField(interstitial, "_placementId", "interstitial.default");
            ReflectionTestUtil.SetPrivateField(interstitial, "_placementType", AdPlacementType.Interstitial);
            ReflectionTestUtil.SetPrivateField(interstitial, "_enabled", true);
            ReflectionTestUtil.SetPrivateField(interstitial, "_cooldownSeconds", 10f);
            ReflectionTestUtil.SetPrivateField(interstitial, "_maxShowsPerSession", 2);

            MonetizationPolicyConfig.ProductPolicy product = new MonetizationPolicyConfig.ProductPolicy();
            ReflectionTestUtil.SetPrivateField(product, "_productId", "coins.pack");
            ReflectionTestUtil.SetPrivateField(product, "_enabled", true);
            ReflectionTestUtil.SetPrivateField(product, "_cooldownSeconds", 2f);
            ReflectionTestUtil.SetPrivateField(product, "_maxPurchasesPerSession", 2);

            ReflectionTestUtil.SetPrivateField(config, "_placementPolicies", new List<MonetizationPolicyConfig.PlacementPolicy> { interstitial });
            ReflectionTestUtil.SetPrivateField(config, "_productPolicies", new List<MonetizationPolicyConfig.ProductPolicy> { product });
            return config;
        }
    }
}
