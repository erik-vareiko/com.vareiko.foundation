using NUnit.Framework;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Push;
using Zenject;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class MonetizationObservabilityServiceTests
    {
        [Test]
        public void Signals_UpdateSnapshotCountersAndLatencyAverages()
        {
            SignalBus signalBus = CreateSignalBus();
            MonetizationObservabilityService service = new MonetizationObservabilityService(signalBus);
            service.Initialize();

            signalBus.Fire(new IapPurchaseSucceededSignal("coins", "tx1", false));
            signalBus.Fire(new IapPurchaseFailedSignal("coins", "fail", InAppPurchaseErrorCode.PurchaseFailed));
            signalBus.Fire(new IapOperationTelemetrySignal("purchase", true, 100f, InAppPurchaseErrorCode.None));
            signalBus.Fire(new IapOperationTelemetrySignal("purchase", false, 300f, InAppPurchaseErrorCode.PurchaseFailed));

            signalBus.Fire(new AdShownSignal("interstitial.default", AdPlacementType.Interstitial, false));
            signalBus.Fire(new AdShowFailedSignal("interstitial.default", AdPlacementType.Interstitial, "fail", AdsErrorCode.ShowFailed));
            signalBus.Fire(new AdsOperationTelemetrySignal("show", "interstitial.default", true, 50f, AdsErrorCode.None));
            signalBus.Fire(new AdsOperationTelemetrySignal("show", "interstitial.default", false, 150f, AdsErrorCode.ShowFailed));

            signalBus.Fire(new PushPermissionChangedSignal(PushNotificationPermissionStatus.Granted));
            signalBus.Fire(new PushPermissionChangedSignal(PushNotificationPermissionStatus.Denied));
            signalBus.Fire(new PushOperationTelemetrySignal("request_permission", true, 40f, PushNotificationErrorCode.None));
            signalBus.Fire(new PushOperationTelemetrySignal("request_permission", false, 60f, PushNotificationErrorCode.PermissionDenied));
            signalBus.Fire(new PushTopicSubscribedSignal("news"));
            signalBus.Fire(new PushTopicSubscriptionFailedSignal("offers", "err", PushNotificationErrorCode.OperationFailed));

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
            SignalBus signalBus = CreateSignalBus();
            MonetizationObservabilityService service = new MonetizationObservabilityService(signalBus);
            service.Initialize();

            signalBus.Fire(new IapOperationTelemetrySignal("initialize", true, 99f, InAppPurchaseErrorCode.None));
            signalBus.Fire(new AdsOperationTelemetrySignal("load", "rewarded.default", true, 66f, AdsErrorCode.None));
            signalBus.Fire(new PushOperationTelemetrySignal("subscribe", true, 55f, PushNotificationErrorCode.None));

            MonetizationObservabilitySnapshot snapshot = service.Snapshot;
            Assert.That(snapshot.IapPurchaseAvgLatencyMs, Is.EqualTo(0f));
            Assert.That(snapshot.AdShowAvgLatencyMs, Is.EqualTo(0f));
            Assert.That(snapshot.PushPermissionAvgLatencyMs, Is.EqualTo(0f));

            service.Dispose();
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<IapPurchaseSucceededSignal>();
            container.DeclareSignal<IapPurchaseFailedSignal>();
            container.DeclareSignal<IapOperationTelemetrySignal>();
            container.DeclareSignal<AdShownSignal>();
            container.DeclareSignal<AdShowFailedSignal>();
            container.DeclareSignal<AdsOperationTelemetrySignal>();
            container.DeclareSignal<PushPermissionChangedSignal>();
            container.DeclareSignal<PushOperationTelemetrySignal>();
            container.DeclareSignal<PushTopicSubscribedSignal>();
            container.DeclareSignal<PushTopicSubscriptionFailedSignal>();
            return container.Resolve<SignalBus>();
        }
    }
}
