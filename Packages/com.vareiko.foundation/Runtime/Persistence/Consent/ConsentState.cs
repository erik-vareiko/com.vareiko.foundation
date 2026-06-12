using System;

namespace Vareiko.Foundation.Consent
{
    [Serializable]
    public sealed class ConsentState
    {
        public bool IsCollected;
        public bool Analytics;
        public bool Personalization;
        public bool Advertising;
        public bool PushNotifications;

        public bool Get(ConsentScope scope)
        {
            switch (scope)
            {
                case ConsentScope.Analytics:
                    return Analytics;
                case ConsentScope.Personalization:
                    return Personalization;
                case ConsentScope.Advertising:
                    return Advertising;
                case ConsentScope.PushNotifications:
                    return PushNotifications;
                default:
                    return false;
            }
        }

        public void Set(ConsentScope scope, bool value)
        {
            switch (scope)
            {
                case ConsentScope.Analytics:
                    Analytics = value;
                    break;
                case ConsentScope.Personalization:
                    Personalization = value;
                    break;
                case ConsentScope.Advertising:
                    Advertising = value;
                    break;
                case ConsentScope.PushNotifications:
                    PushNotifications = value;
                    break;
            }
        }
    }
}
