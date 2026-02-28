using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Push
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Push Notification Config")]
    public sealed class PushNotificationConfig : ScriptableObject
    {
        [Serializable]
        public sealed class TopicDefinition
        {
            [SerializeField] private string _topic = "general";
            [SerializeField] private bool _enabled = true;

            public string Topic => string.IsNullOrWhiteSpace(_topic) ? string.Empty : _topic.Trim();
            public bool Enabled => _enabled;
        }

        [SerializeField] private PushNotificationProviderType _provider = PushNotificationProviderType.None;
        [SerializeField] private bool _autoInitializeOnStart;
        [SerializeField] private bool _requirePushConsent = true;
        [SerializeField] private bool _autoRequestPermissionOnInitialize = true;
        [SerializeField] private bool _simulatePermissionDenied;
        [SerializeField] private bool _autoSubscribeDefaultTopics = true;
        [SerializeField] private string _simulatedDeviceToken = "SIMULATED_PUSH_TOKEN";
        [SerializeField] private List<TopicDefinition> _defaultTopics = new List<TopicDefinition>();

        public PushNotificationProviderType Provider => _provider;
        public bool AutoInitializeOnStart => _autoInitializeOnStart;
        public bool RequirePushConsent => _requirePushConsent;
        public bool AutoRequestPermissionOnInitialize => _autoRequestPermissionOnInitialize;
        public bool SimulatePermissionDenied => _simulatePermissionDenied;
        public bool AutoSubscribeDefaultTopics => _autoSubscribeDefaultTopics;
        public string SimulatedDeviceToken => _simulatedDeviceToken ?? string.Empty;
        public IReadOnlyList<TopicDefinition> DefaultTopics => _defaultTopics;
    }
}
