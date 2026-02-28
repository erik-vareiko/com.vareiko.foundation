using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Backend
{
    public sealed class CachedRemoteConfigServiceTests
    {
        [Test]
        public async Task ForceRefreshAsync_RefreshesCache_AndEmitsForcedSource()
        {
            FakeMutableRemoteConfigService inner = new FakeMutableRemoteConfigService();
            FakeTimeProvider time = new FakeTimeProvider { Time = 10f };
            SignalBus signalBus = CreateSignalBus();

            string source = string.Empty;
            signalBus.Subscribe<RemoteConfigRefreshedSignal>(signal => source = signal.Source);

            CachedRemoteConfigService service = new CachedRemoteConfigService(inner, time, null, signalBus);
            service.Initialize();
            await UniTask.DelayFrame(2);
            source = string.Empty;

            inner.Set("feature.x", "true");
            await service.ForceRefreshAsync();

            Assert.That(inner.RefreshCalls, Is.GreaterThanOrEqualTo(1));
            Assert.That(service.CachedValueCount, Is.EqualTo(1));
            Assert.That(service.TryGetString("feature.x", out string value), Is.True);
            Assert.That(value, Is.EqualTo("true"));
            Assert.That(source, Is.EqualTo("forced"));
        }

        [Test]
        public async Task InvalidateCache_ClearsValues_EmitsSignal_AndTriggersNextAutoRefresh()
        {
            FakeMutableRemoteConfigService inner = new FakeMutableRemoteConfigService();
            FakeTimeProvider time = new FakeTimeProvider { Time = 1f };
            RemoteConfigCacheConfig config = CreateCacheConfig(refreshOnInitialize: false, autoRefresh: true, intervalSeconds: 60f);
            SignalBus signalBus = CreateSignalBus();

            int clearedValueCount = -1;
            signalBus.Subscribe<RemoteConfigCacheInvalidatedSignal>(signal => clearedValueCount = signal.ClearedValueCount);

            CachedRemoteConfigService service = new CachedRemoteConfigService(inner, time, config, signalBus);
            service.Initialize();

            inner.Set("a", "1");
            await service.RefreshAsync();
            Assert.That(service.CachedValueCount, Is.EqualTo(1));

            service.InvalidateCache("manual");
            Assert.That(service.CachedValueCount, Is.EqualTo(0));
            Assert.That(clearedValueCount, Is.EqualTo(1));

            inner.Set("b", "2");
            service.Tick();
            await UniTask.DelayFrame(2);

            Assert.That(service.TryGetString("b", out string value), Is.True);
            Assert.That(value, Is.EqualTo("2"));
            Assert.That(inner.RefreshCalls, Is.GreaterThanOrEqualTo(2));
            Object.DestroyImmediate(config);
        }

        [Test]
        public async Task Tick_WhenAutoRefreshIntervalReached_EmitsAutoSource()
        {
            FakeMutableRemoteConfigService inner = new FakeMutableRemoteConfigService();
            FakeTimeProvider time = new FakeTimeProvider { Time = 0f };
            RemoteConfigCacheConfig config = CreateCacheConfig(refreshOnInitialize: false, autoRefresh: true, intervalSeconds: 5f);
            SignalBus signalBus = CreateSignalBus();

            string source = string.Empty;
            signalBus.Subscribe<RemoteConfigRefreshedSignal>(signal => source = signal.Source);

            CachedRemoteConfigService service = new CachedRemoteConfigService(inner, time, config, signalBus);
            service.Initialize();

            time.Time = 4.9f;
            service.Tick();
            await UniTask.DelayFrame(2);
            Assert.That(inner.RefreshCalls, Is.EqualTo(0));

            time.Time = 5.1f;
            service.Tick();
            await UniTask.DelayFrame(2);

            Assert.That(inner.RefreshCalls, Is.EqualTo(1));
            Assert.That(source, Is.EqualTo("auto"));
            Object.DestroyImmediate(config);
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<RemoteConfigRefreshedSignal>();
            container.DeclareSignal<RemoteConfigRefreshFailedSignal>();
            container.DeclareSignal<RemoteConfigCacheInvalidatedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static RemoteConfigCacheConfig CreateCacheConfig(bool refreshOnInitialize, bool autoRefresh, float intervalSeconds)
        {
            RemoteConfigCacheConfig config = ScriptableObject.CreateInstance<RemoteConfigCacheConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_refreshOnInitialize", refreshOnInitialize);
            ReflectionTestUtil.SetPrivateField(config, "_autoRefresh", autoRefresh);
            ReflectionTestUtil.SetPrivateField(config, "_refreshIntervalSeconds", intervalSeconds);
            return config;
        }

        private sealed class FakeMutableRemoteConfigService : IRemoteConfigService
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>(System.StringComparer.Ordinal);
            public int RefreshCalls { get; private set; }

            public bool IsReady => true;

            public UniTask RefreshAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshCalls++;
                return UniTask.CompletedTask;
            }

            public bool TryGetString(string key, out string value)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    value = string.Empty;
                    return false;
                }

                return _values.TryGetValue(key, out value);
            }

            public bool TryGetInt(string key, out int value)
            {
                value = default;
                string raw;
                if (!TryGetString(key, out raw))
                {
                    return false;
                }

                return int.TryParse(raw, out value);
            }

            public bool TryGetFloat(string key, out float value)
            {
                value = default;
                string raw;
                if (!TryGetString(key, out raw))
                {
                    return false;
                }

                return float.TryParse(raw, out value);
            }

            public IReadOnlyDictionary<string, string> Snapshot()
            {
                return new Dictionary<string, string>(_values);
            }

            public void Set(string key, string value)
            {
                _values[key] = value;
            }
        }
    }
}
