using UnityEngine;

namespace Vareiko.Foundation.Analytics
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Analytics Config")]
    public sealed class AnalyticsConfig : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _requireConsent = true;
        [SerializeField] private bool _verboseLogging;
        [SerializeField] private int _maxBufferedEvents = 2048;

        public bool Enabled => _enabled;
        public bool RequireConsent => _requireConsent;
        public bool VerboseLogging => _verboseLogging;
        public int MaxBufferedEvents => _maxBufferedEvents;
    }
}
