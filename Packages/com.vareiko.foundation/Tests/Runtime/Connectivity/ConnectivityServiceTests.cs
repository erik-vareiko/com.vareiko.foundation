using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Connectivity
{
    public sealed class ConnectivityServiceTests
    {
        [Test]
        public void FocusRegained_RefreshesConnectivity_WhenEnabled()
        {
            FakeTimeProvider time = new FakeTimeProvider { Time = 0f };
            FakeReachabilityProvider reachability = new FakeReachabilityProvider(NetworkReachability.NotReachable);
            FakeLifecycleService lifecycle = new FakeLifecycleService();
            ConnectivityConfig config = CreateConfig(refreshOnFocusRegained: true, focusCooldownSeconds: 0f);

            ConnectivityService service = new ConnectivityService(time, config, null, reachability, lifecycle);
            service.Initialize();

            Assert.That(service.IsOnline, Is.False);

            reachability.Current = NetworkReachability.ReachableViaCarrierDataNetwork;
            lifecycle.EmitFocus(true);

            Assert.That(service.IsOnline, Is.True);
            service.Dispose();
        }

        [Test]
        public void FocusRegained_RespectsCooldown()
        {
            FakeTimeProvider time = new FakeTimeProvider { Time = 0f };
            FakeReachabilityProvider reachability = new FakeReachabilityProvider(NetworkReachability.ReachableViaLocalAreaNetwork);
            FakeLifecycleService lifecycle = new FakeLifecycleService();
            ConnectivityConfig config = CreateConfig(refreshOnFocusRegained: true, focusCooldownSeconds: 10f);

            ConnectivityService service = new ConnectivityService(time, config, null, reachability, lifecycle);
            service.Initialize();
            Assert.That(service.IsOnline, Is.True);

            lifecycle.EmitFocus(true);
            reachability.Current = NetworkReachability.NotReachable;
            lifecycle.EmitFocus(true);
            Assert.That(service.IsOnline, Is.True);

            time.Time = 11f;
            lifecycle.EmitFocus(true);
            Assert.That(service.IsOnline, Is.False);
            service.Dispose();
        }

        private static ConnectivityConfig CreateConfig(bool refreshOnFocusRegained, float focusCooldownSeconds)
        {
            ConnectivityConfig config = ScriptableObject.CreateInstance<ConnectivityConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_pollIntervalSeconds", 5f);
            ReflectionTestUtil.SetPrivateField(config, "_refreshOnFocusRegained", refreshOnFocusRegained);
            ReflectionTestUtil.SetPrivateField(config, "_focusRefreshCooldownSeconds", focusCooldownSeconds);
            return config;
        }

        private sealed class FakeReachabilityProvider : INetworkReachabilityProvider
        {
            public NetworkReachability Current;

            public FakeReachabilityProvider(NetworkReachability current)
            {
                Current = current;
            }

            public NetworkReachability GetReachability()
            {
                return Current;
            }
        }

        private sealed class FakeLifecycleService : IApplicationLifecycleService
        {
            public bool IsPaused { get; private set; }
            public bool HasFocus { get; private set; } = true;
            public bool IsQuitting { get; private set; }

            public event System.Action<bool> PauseChanged;
            public event System.Action<bool> FocusChanged;
            public event System.Action QuitRequested;

            public void EmitFocus(bool hasFocus)
            {
                HasFocus = hasFocus;
                FocusChanged?.Invoke(hasFocus);
            }

            public void EmitPause(bool isPaused)
            {
                IsPaused = isPaused;
                PauseChanged?.Invoke(isPaused);
            }

            public void EmitQuit()
            {
                IsQuitting = true;
                QuitRequested?.Invoke();
            }
        }
    }
}
