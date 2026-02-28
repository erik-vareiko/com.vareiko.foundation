using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Push
{
    public interface IPushNotificationService
    {
        PushNotificationProviderType Provider { get; }
        bool IsConfigured { get; }
        bool IsInitialized { get; }
        PushNotificationPermissionStatus PermissionStatus { get; }
        UniTask<PushInitializeResult> InitializeAsync(CancellationToken cancellationToken = default);
        UniTask<PushPermissionResult> RequestPermissionAsync(CancellationToken cancellationToken = default);
        UniTask<PushDeviceTokenResult> GetDeviceTokenAsync(CancellationToken cancellationToken = default);
        UniTask<PushTopicResult> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
        UniTask<PushTopicResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
        UniTask<IReadOnlyList<string>> GetSubscribedTopicsAsync(CancellationToken cancellationToken = default);
    }
}
