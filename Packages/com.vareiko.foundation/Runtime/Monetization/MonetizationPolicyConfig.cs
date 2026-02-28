using System;
using System.Collections.Generic;
using UnityEngine;
using Vareiko.Foundation.Ads;

namespace Vareiko.Foundation.Monetization
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Monetization Policy Config")]
    public sealed class MonetizationPolicyConfig : ScriptableObject
    {
        [Serializable]
        public sealed class PlacementPolicy
        {
            [SerializeField] private string _placementId = "interstitial.default";
            [SerializeField] private AdPlacementType _placementType = AdPlacementType.Interstitial;
            [SerializeField] private bool _enabled = true;
            [SerializeField] private float _cooldownSeconds = 45f;
            [SerializeField] private int _maxShowsPerSession = 20;

            public string PlacementId => string.IsNullOrWhiteSpace(_placementId) ? string.Empty : _placementId.Trim();
            public AdPlacementType PlacementType => _placementType;
            public bool Enabled => _enabled;
            public float CooldownSeconds => _cooldownSeconds < 0f ? 0f : _cooldownSeconds;
            public int MaxShowsPerSession => _maxShowsPerSession < 0 ? 0 : _maxShowsPerSession;
        }

        [Serializable]
        public sealed class ProductPolicy
        {
            [SerializeField] private string _productId = "product.id";
            [SerializeField] private bool _enabled = true;
            [SerializeField] private float _cooldownSeconds = 1f;
            [SerializeField] private int _maxPurchasesPerSession = 50;

            public string ProductId => string.IsNullOrWhiteSpace(_productId) ? string.Empty : _productId.Trim();
            public bool Enabled => _enabled;
            public float CooldownSeconds => _cooldownSeconds < 0f ? 0f : _cooldownSeconds;
            public int MaxPurchasesPerSession => _maxPurchasesPerSession < 0 ? 0 : _maxPurchasesPerSession;
        }

        [SerializeField] private bool _requireExplicitPlacementPolicy;
        [SerializeField] private bool _requireExplicitProductPolicy;
        [SerializeField] private float _defaultInterstitialCooldownSeconds = 45f;
        [SerializeField] private int _defaultInterstitialMaxShowsPerSession = 20;
        [SerializeField] private float _defaultRewardedCooldownSeconds = 5f;
        [SerializeField] private int _defaultRewardedMaxShowsPerSession = 50;
        [SerializeField] private float _defaultIapCooldownSeconds = 1f;
        [SerializeField] private int _defaultIapMaxPurchasesPerSession = 50;
        [SerializeField] private List<PlacementPolicy> _placementPolicies = new List<PlacementPolicy>();
        [SerializeField] private List<ProductPolicy> _productPolicies = new List<ProductPolicy>();

        public bool RequireExplicitPlacementPolicy => _requireExplicitPlacementPolicy;
        public bool RequireExplicitProductPolicy => _requireExplicitProductPolicy;
        public float DefaultInterstitialCooldownSeconds => _defaultInterstitialCooldownSeconds < 0f ? 0f : _defaultInterstitialCooldownSeconds;
        public int DefaultInterstitialMaxShowsPerSession => _defaultInterstitialMaxShowsPerSession < 0 ? 0 : _defaultInterstitialMaxShowsPerSession;
        public float DefaultRewardedCooldownSeconds => _defaultRewardedCooldownSeconds < 0f ? 0f : _defaultRewardedCooldownSeconds;
        public int DefaultRewardedMaxShowsPerSession => _defaultRewardedMaxShowsPerSession < 0 ? 0 : _defaultRewardedMaxShowsPerSession;
        public float DefaultIapCooldownSeconds => _defaultIapCooldownSeconds < 0f ? 0f : _defaultIapCooldownSeconds;
        public int DefaultIapMaxPurchasesPerSession => _defaultIapMaxPurchasesPerSession < 0 ? 0 : _defaultIapMaxPurchasesPerSession;
        public IReadOnlyList<PlacementPolicy> PlacementPolicies => _placementPolicies;
        public IReadOnlyList<ProductPolicy> ProductPolicies => _productPolicies;
    }
}
