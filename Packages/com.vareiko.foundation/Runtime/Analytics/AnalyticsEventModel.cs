using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Analytics
{
    [Serializable]
    public sealed class AnalyticsEventModel
    {
        public string EventName;
        public float TimeFromStartup;
        public string UserId;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }
}
