using System;
using UnityEngine;

namespace Vareiko.Foundation.Features
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Feature Flags Config")]
    public sealed class FeatureFlagsConfig : ScriptableObject
    {
        [Serializable]
        public struct BoolFeature
        {
            public string Key;
            public bool DefaultValue;
        }

        [SerializeField] private bool _refreshOnInitialize = true;
        [SerializeField] private BoolFeature[] _boolFeatures = Array.Empty<BoolFeature>();

        public bool RefreshOnInitialize => _refreshOnInitialize;
        public BoolFeature[] BoolFeatures => _boolFeatures;
    }
}
