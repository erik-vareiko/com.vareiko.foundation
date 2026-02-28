using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Time;
using Zenject;

namespace Vareiko.Foundation.Monetization
{
    public sealed class MonetizationPolicyService : IMonetizationPolicyService, IInitializable
    {
        private readonly MonetizationPolicyConfig _config;
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly SignalBus _signalBus;

        private readonly Dictionary<string, PlacementPolicySnapshot> _placementPolicies = new Dictionary<string, PlacementPolicySnapshot>(StringComparer.Ordinal);
        private readonly Dictionary<string, ProductPolicySnapshot> _productPolicies = new Dictionary<string, ProductPolicySnapshot>(StringComparer.Ordinal);
        private readonly Dictionary<string, SessionCounter> _adCounters = new Dictionary<string, SessionCounter>(StringComparer.Ordinal);
        private readonly Dictionary<string, SessionCounter> _iapCounters = new Dictionary<string, SessionCounter>(StringComparer.Ordinal);

        [Inject]
        public MonetizationPolicyService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] MonetizationPolicyConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _placementPolicies.Clear();
            _productPolicies.Clear();
            _adCounters.Clear();
            _iapCounters.Clear();

            if (_config == null)
            {
                return;
            }

            IReadOnlyList<MonetizationPolicyConfig.PlacementPolicy> placementPolicies = _config.PlacementPolicies;
            if (placementPolicies != null)
            {
                for (int i = 0; i < placementPolicies.Count; i++)
                {
                    MonetizationPolicyConfig.PlacementPolicy policy = placementPolicies[i];
                    if (policy == null || !policy.Enabled || string.IsNullOrWhiteSpace(policy.PlacementId))
                    {
                        continue;
                    }

                    _placementPolicies[policy.PlacementId] = new PlacementPolicySnapshot(
                        policy.PlacementType,
                        policy.CooldownSeconds,
                        policy.MaxShowsPerSession);
                }
            }

            IReadOnlyList<MonetizationPolicyConfig.ProductPolicy> productPolicies = _config.ProductPolicies;
            if (productPolicies != null)
            {
                for (int i = 0; i < productPolicies.Count; i++)
                {
                    MonetizationPolicyConfig.ProductPolicy policy = productPolicies[i];
                    if (policy == null || !policy.Enabled || string.IsNullOrWhiteSpace(policy.ProductId))
                    {
                        continue;
                    }

                    _productPolicies[policy.ProductId] = new ProductPolicySnapshot(
                        policy.CooldownSeconds,
                        policy.MaxPurchasesPerSession);
                }
            }
        }

        public UniTask<MonetizationAdDecision> CanShowAdAsync(string placementId, AdPlacementType placementType, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedPlacementId;
            if (!TryNormalizeId(placementId, out normalizedPlacementId))
            {
                MonetizationAdDecision invalid = MonetizationAdDecision.Block(string.Empty, placementType, MonetizationPolicyBlockReason.InvalidInput, "Placement id is empty.");
                EmitAdBlocked(invalid);
                return UniTask.FromResult(invalid);
            }

            if (!TryResolvePlacementPolicy(normalizedPlacementId, placementType, out PlacementPolicySnapshot policy, out MonetizationAdDecision blockedByPolicy))
            {
                EmitAdBlocked(blockedByPolicy);
                return UniTask.FromResult(blockedByPolicy);
            }

            SessionCounter counter;
            _adCounters.TryGetValue(normalizedPlacementId, out counter);

            if (policy.MaxPerSession > 0 && counter.Count >= policy.MaxPerSession)
            {
                MonetizationAdDecision capped = MonetizationAdDecision.Block(
                    normalizedPlacementId,
                    placementType,
                    MonetizationPolicyBlockReason.SessionCapReached,
                    "Ad show session cap is reached.");
                EmitAdBlocked(capped);
                return UniTask.FromResult(capped);
            }

            if (counter.HasEntry && policy.CooldownSeconds > 0f)
            {
                float elapsed = _timeProvider.UnscaledTime - counter.LastActionTime;
                if (elapsed < policy.CooldownSeconds)
                {
                    float retryAfter = policy.CooldownSeconds - elapsed;
                    MonetizationAdDecision cooldown = MonetizationAdDecision.Block(
                        normalizedPlacementId,
                        placementType,
                        MonetizationPolicyBlockReason.CooldownActive,
                        "Ad placement cooldown is active.",
                        retryAfter);
                    EmitAdBlocked(cooldown);
                    return UniTask.FromResult(cooldown);
                }
            }

            return UniTask.FromResult(MonetizationAdDecision.Allow(normalizedPlacementId, placementType));
        }

        public UniTask RecordAdShownAsync(string placementId, AdPlacementType placementType, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedPlacementId;
            if (!TryNormalizeId(placementId, out normalizedPlacementId))
            {
                return UniTask.CompletedTask;
            }

            SessionCounter counter;
            _adCounters.TryGetValue(normalizedPlacementId, out counter);
            counter.Count = Math.Max(0, counter.Count) + 1;
            counter.LastActionTime = _timeProvider.UnscaledTime;
            counter.HasEntry = true;
            _adCounters[normalizedPlacementId] = counter;
            _signalBus?.Fire(new MonetizationAdRecordedSignal(normalizedPlacementId, placementType, counter.Count));
            return UniTask.CompletedTask;
        }

        public UniTask<MonetizationIapDecision> CanStartPurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedProductId;
            if (!TryNormalizeId(productId, out normalizedProductId))
            {
                MonetizationIapDecision invalid = MonetizationIapDecision.Block(string.Empty, MonetizationPolicyBlockReason.InvalidInput, "Product id is empty.");
                EmitIapBlocked(invalid);
                return UniTask.FromResult(invalid);
            }

            if (!TryResolveProductPolicy(normalizedProductId, out ProductPolicySnapshot policy, out MonetizationIapDecision blockedByPolicy))
            {
                EmitIapBlocked(blockedByPolicy);
                return UniTask.FromResult(blockedByPolicy);
            }

            SessionCounter counter;
            _iapCounters.TryGetValue(normalizedProductId, out counter);

            if (policy.MaxPerSession > 0 && counter.Count >= policy.MaxPerSession)
            {
                MonetizationIapDecision capped = MonetizationIapDecision.Block(
                    normalizedProductId,
                    MonetizationPolicyBlockReason.SessionCapReached,
                    "IAP purchase session cap is reached.");
                EmitIapBlocked(capped);
                return UniTask.FromResult(capped);
            }

            if (counter.HasEntry && policy.CooldownSeconds > 0f)
            {
                float elapsed = _timeProvider.UnscaledTime - counter.LastActionTime;
                if (elapsed < policy.CooldownSeconds)
                {
                    float retryAfter = policy.CooldownSeconds - elapsed;
                    MonetizationIapDecision cooldown = MonetizationIapDecision.Block(
                        normalizedProductId,
                        MonetizationPolicyBlockReason.CooldownActive,
                        "IAP purchase cooldown is active.",
                        retryAfter);
                    EmitIapBlocked(cooldown);
                    return UniTask.FromResult(cooldown);
                }
            }

            return UniTask.FromResult(MonetizationIapDecision.Allow(normalizedProductId));
        }

        public UniTask RecordPurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedProductId;
            if (!TryNormalizeId(productId, out normalizedProductId))
            {
                return UniTask.CompletedTask;
            }

            SessionCounter counter;
            _iapCounters.TryGetValue(normalizedProductId, out counter);
            counter.Count = Math.Max(0, counter.Count) + 1;
            counter.LastActionTime = _timeProvider.UnscaledTime;
            counter.HasEntry = true;
            _iapCounters[normalizedProductId] = counter;
            _signalBus?.Fire(new MonetizationIapRecordedSignal(normalizedProductId, counter.Count));
            return UniTask.CompletedTask;
        }

        public UniTask ResetSessionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _adCounters.Clear();
            _iapCounters.Clear();
            _signalBus?.Fire(new MonetizationSessionResetSignal());
            return UniTask.CompletedTask;
        }

        private bool TryResolvePlacementPolicy(
            string placementId,
            AdPlacementType placementType,
            out PlacementPolicySnapshot policy,
            out MonetizationAdDecision blocked)
        {
            PlacementPolicySnapshot configuredPolicy;
            if (_placementPolicies.TryGetValue(placementId, out configuredPolicy))
            {
                if (configuredPolicy.PlacementType != placementType)
                {
                    blocked = MonetizationAdDecision.Block(
                        placementId,
                        placementType,
                        MonetizationPolicyBlockReason.InvalidInput,
                        "Placement type does not match configured policy.");
                    policy = default;
                    return false;
                }

                policy = configuredPolicy;
                blocked = default;
                return true;
            }

            if (_config != null && _config.RequireExplicitPlacementPolicy)
            {
                blocked = MonetizationAdDecision.Block(
                    placementId,
                    placementType,
                    MonetizationPolicyBlockReason.PlacementNotConfigured,
                    "Placement is not configured in monetization policy.");
                policy = default;
                return false;
            }

            policy = placementType == AdPlacementType.Rewarded
                ? new PlacementPolicySnapshot(placementType, GetDefaultRewardedCooldown(), GetDefaultRewardedMaxPerSession())
                : new PlacementPolicySnapshot(placementType, GetDefaultInterstitialCooldown(), GetDefaultInterstitialMaxPerSession());
            blocked = default;
            return true;
        }

        private bool TryResolveProductPolicy(string productId, out ProductPolicySnapshot policy, out MonetizationIapDecision blocked)
        {
            ProductPolicySnapshot configuredPolicy;
            if (_productPolicies.TryGetValue(productId, out configuredPolicy))
            {
                policy = configuredPolicy;
                blocked = default;
                return true;
            }

            if (_config != null && _config.RequireExplicitProductPolicy)
            {
                blocked = MonetizationIapDecision.Block(
                    productId,
                    MonetizationPolicyBlockReason.ProductNotConfigured,
                    "Product is not configured in monetization policy.");
                policy = default;
                return false;
            }

            policy = new ProductPolicySnapshot(GetDefaultIapCooldown(), GetDefaultIapMaxPerSession());
            blocked = default;
            return true;
        }

        private float GetDefaultInterstitialCooldown()
        {
            return _config != null ? _config.DefaultInterstitialCooldownSeconds : 45f;
        }

        private int GetDefaultInterstitialMaxPerSession()
        {
            return _config != null ? _config.DefaultInterstitialMaxShowsPerSession : 20;
        }

        private float GetDefaultRewardedCooldown()
        {
            return _config != null ? _config.DefaultRewardedCooldownSeconds : 5f;
        }

        private int GetDefaultRewardedMaxPerSession()
        {
            return _config != null ? _config.DefaultRewardedMaxShowsPerSession : 50;
        }

        private float GetDefaultIapCooldown()
        {
            return _config != null ? _config.DefaultIapCooldownSeconds : 1f;
        }

        private int GetDefaultIapMaxPerSession()
        {
            return _config != null ? _config.DefaultIapMaxPurchasesPerSession : 50;
        }

        private static bool TryNormalizeId(string id, out string normalized)
        {
            normalized = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            return normalized.Length > 0;
        }

        private void EmitAdBlocked(MonetizationAdDecision decision)
        {
            _signalBus?.Fire(new MonetizationAdBlockedSignal(
                decision.PlacementId,
                decision.PlacementType,
                decision.BlockReason,
                decision.Message,
                decision.RetryAfterSeconds));
        }

        private void EmitIapBlocked(MonetizationIapDecision decision)
        {
            _signalBus?.Fire(new MonetizationIapBlockedSignal(
                decision.ProductId,
                decision.BlockReason,
                decision.Message,
                decision.RetryAfterSeconds));
        }

        private readonly struct PlacementPolicySnapshot
        {
            public readonly AdPlacementType PlacementType;
            public readonly float CooldownSeconds;
            public readonly int MaxPerSession;

            public PlacementPolicySnapshot(AdPlacementType placementType, float cooldownSeconds, int maxPerSession)
            {
                PlacementType = placementType;
                CooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
                MaxPerSession = maxPerSession < 0 ? 0 : maxPerSession;
            }
        }

        private readonly struct ProductPolicySnapshot
        {
            public readonly float CooldownSeconds;
            public readonly int MaxPerSession;

            public ProductPolicySnapshot(float cooldownSeconds, int maxPerSession)
            {
                CooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
                MaxPerSession = maxPerSession < 0 ? 0 : maxPerSession;
            }
        }

        private struct SessionCounter
        {
            public int Count;
            public float LastActionTime;
            public bool HasEntry;
        }
    }
}
