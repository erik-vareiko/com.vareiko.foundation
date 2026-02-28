using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Push
{
    public sealed class NullPushNotificationService : IPushNotificationService
    {
        private static readonly IReadOnlyList<string> EmptyTopics = new List<string>(0);

        public PushNotificationProviderType Provider => PushNotificationProviderType.None;
        public bool IsConfigured => false;
        public bool IsInitialized => false;
        public PushNotificationPermissionStatus PermissionStatus => PushNotificationPermissionStatus.Unknown;

        public UniTask<PushInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(PushInitializeResult.Fail("Push notifications provider is not configured.", PushNotificationErrorCode.ConfigurationInvalid));
        }

        public UniTask<PushPermissionResult> RequestPermissionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(PushPermissionResult.Fail(PushNotificationPermissionStatus.Unknown, "Push notifications provider is not configured.", PushNotificationErrorCode.ProviderUnavailable));
        }

        public UniTask<PushDeviceTokenResult> GetDeviceTokenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(PushDeviceTokenResult.Fail("Push notifications provider is not configured.", PushNotificationErrorCode.ProviderUnavailable));
        }

        public UniTask<PushTopicResult> SubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(PushTopicResult.Fail(topic, "Push notifications provider is not configured.", PushNotificationErrorCode.ProviderUnavailable));
        }

        public UniTask<PushTopicResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(PushTopicResult.Fail(topic, "Push notifications provider is not configured.", PushNotificationErrorCode.ProviderUnavailable));
        }

        public UniTask<IReadOnlyList<string>> GetSubscribedTopicsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(EmptyTopics);
        }
    }
}
