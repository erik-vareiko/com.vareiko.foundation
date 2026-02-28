using System;

namespace Vareiko.Foundation.Push
{
    public static class UnityPushNotificationBridge
    {
        private static string _lastDeviceToken = string.Empty;

        public static event Action<string> DeviceTokenReceived;

        public static string LastDeviceToken => _lastDeviceToken;

        public static void ReportDeviceToken(string deviceToken)
        {
            string normalized = string.IsNullOrWhiteSpace(deviceToken) ? string.Empty : deviceToken.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            _lastDeviceToken = normalized;
            DeviceTokenReceived?.Invoke(normalized);
        }
    }
}
