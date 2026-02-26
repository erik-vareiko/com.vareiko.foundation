using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Analytics
{
    public interface IAnalyticsService
    {
        bool Enabled { get; }
        void SetUserId(string userId);
        void SetSessionProperty(string key, string value);
        void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties = null);
        UniTask FlushAsync(CancellationToken cancellationToken = default);
    }
}
