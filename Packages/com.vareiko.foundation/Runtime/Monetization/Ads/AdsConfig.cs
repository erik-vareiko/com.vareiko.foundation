using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Ads
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Ads Config")]
    public sealed class AdsConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Placement
        {
            [SerializeField] private string _placementId = "rewarded.default";
            [SerializeField] private AdPlacementType _placementType = AdPlacementType.Rewarded;
            [SerializeField] private bool _enabled = true;
            [SerializeField] private bool _simulateLoadFailure;
            [SerializeField] private bool _simulateShowFailure;
            [SerializeField] private string _rewardId = "reward.default";
            [SerializeField] private int _rewardAmount = 1;

            public string PlacementId => string.IsNullOrWhiteSpace(_placementId) ? string.Empty : _placementId.Trim();
            public AdPlacementType PlacementType => _placementType;
            public bool Enabled => _enabled;
            public bool SimulateLoadFailure => _simulateLoadFailure;
            public bool SimulateShowFailure => _simulateShowFailure;
            public string RewardId => _rewardId ?? string.Empty;
            public int RewardAmount => _rewardAmount < 0 ? 0 : _rewardAmount;
        }

        [SerializeField] private AdsProviderType _provider = AdsProviderType.None;
        [SerializeField] private bool _autoInitializeOnStart;
        [SerializeField] private bool _requireAdvertisingConsent = true;
        [SerializeField] private List<Placement> _placements = new List<Placement>();

        public AdsProviderType Provider => _provider;
        public bool AutoInitializeOnStart => _autoInitializeOnStart;
        public bool RequireAdvertisingConsent => _requireAdvertisingConsent;
        public IReadOnlyList<Placement> Placements => _placements;
    }
}
