using NUnit.Framework;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Push;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class MonetizationObservabilityServiceTests
    {
        [Test]
        public void Signals_UpdateSnapshotCountersAndLatencyAverages()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            MonetizationObservabilityService service = new MonetizationObservabilityService(signalBus);
            service.Initialize();

            signalBus.Publish(new IapPurchaseSucceededSignal("coins", "tx1", false));
            signalBus.Publish(new IapPurchaseFailedSignal("coins", "fail", InAppPurchaseErrorCode.PurchaseFailed));
            signalBus.Publish(new IapOperationTelemetrySignal("purchase", true, 100f, InAppPurchaseErrorCode.None));
            signalBus.Publish(new IapOperationTelemetrySignal("purchase", false, 300f, InAppPurchaseErrorCode.PurchaseFailed));

            signalBus.Publish(new AdShownSignal("interstitial.default", AdPlacementType.Interstitial, false));
            signalBus.Publish(new AdShowFailedSignal("interstitial.default", AdPlacementType.Interstitial, "fail", AdsErrorCode.ShowFailed));
            signalBus.Publish(new AdsOperationTelemetrySignal("show", "interstitial.default", true, 50f, AdsErrorCode.None));
            signalBus.Publish(new AdsOperationTelemetrySignal("show", "interstitial.default", false, 150f, AdsErrorCode.ShowFailed));

            signalBus.Publish(new PushPermissionChangedSignal(PushNotificationPermissionStatus.Granted));
            signalBus.Publish(new PushPermissionChangedSignal(PushNotificationPermissionStatus.Denied));
            signalBus.Publish(new PushOperationTelemetrySignal("request_permission", true, 40f, PushNotificationErrorCode.None));
            signalBus.Publish(new PushOperationTelemetrySignal("request_permission", false, 60f, PushNotificationErrorCode.PermissionDenied));
            signalBus.Publish(new PushTopicSubscribedSignal("news"));
            signalBus.Publish(new PushTopicSubscriptionFailedSignal("offers", "err", PushNotificationErrorCode.OperationFailed));

            MonetizationObservabilitySnapshot snapshot = service.Snapshot;
            Assert.That(snapshot.IapPurchaseSuccessCount, Is.EqualTo(1));
            Assert.That(snapshot.IapPurchaseFailureCount, Is.EqualTo(1));
            Assert.That(snapshot.IapPurchaseLastLatencyMs, Is.EqualTo(300f).Within(0.001f));
            Assert.That(snapshot.IapPurchaseAvgLatencyMs, Is.EqualTo(200f).Within(0.001f));

            Assert.That(snapshot.AdShowSuccessCount, Is.EqualTo(1));
            Assert.That(snapshot.AdShowFailureCount, Is.EqualTo(1));
            Assert.That(snapshot.AdShowLastLatencyMs, Is.EqualTo(150f).Within(0.001f));
            Assert.That(snapshot.AdShowAvgLatencyMs, Is.EqualTo(100f).Within(0.001f));

            Assert.That(snapshot.PushPermissionGrantedCount, Is.EqualTo(1));
            Assert.That(snapshot.PushPermissionDeniedCount, Is.EqualTo(1));
            Assert.That(snapshot.PushPermissionLastLatencyMs, Is.EqualTo(60f).Within(0.001f));
            Assert.That(snapshot.PushPermissionAvgLatencyMs, Is.EqualTo(50f).Within(0.001f));
            Assert.That(snapshot.PushTopicSubscribeSuccessCount, Is.EqualTo(1));
            Assert.That(snapshot.PushTopicSubscribeFailureCount, Is.EqualTo(1));

            service.Dispose();
        }

        [Test]
        public void Telemetry_IgnoresIrrelevantOperations()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            MonetizationObservabilityService service = new MonetizationObservabilityService(signalBus);
            service.Initialize();

            signalBus.Publish(new IapOperationTelemetrySignal("initialize", true, 99f, InAppPurchaseErrorCode.None));
            signalBus.Publish(new AdsOperationTelemetrySignal("load", "rewarded.default", true, 66f, AdsErrorCode.None));
            signalBus.Publish(new PushOperationTelemetrySignal("subscribe", true, 55f, PushNotificationErrorCode.None));

            MonetizationObservabilitySnapshot snapshot = service.Snapshot;
            Assert.That(snapshot.IapPurchaseAvgLatencyMs, Is.EqualTo(0f));
            Assert.That(snapshot.AdShowAvgLatencyMs, Is.EqualTo(0f));
            Assert.That(snapshot.PushPermissionAvgLatencyMs, Is.EqualTo(0f));

            service.Dispose();
        }

    }
}
