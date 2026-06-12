using UnityEngine;

namespace Vareiko.Foundation.Attribution
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Attribution Config")]
    public sealed class AttributionConfig : ScriptableObject
    {
        [SerializeField] private AttributionProviderType _provider = AttributionProviderType.None;
        [SerializeField] private bool _autoInitializeOnStart;
        [SerializeField] private bool _requireTrackingConsent = true;

        public AttributionProviderType Provider => _provider;
        public bool AutoInitializeOnStart => _autoInitializeOnStart;
        public bool RequireTrackingConsent => _requireTrackingConsent;
    }
}
