using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Backend
{
    public sealed class ReliableCloudFunctionServiceTests
    {
        [Test]
        public async Task Initialize_RestoresPersistedQueue_AndPersistsOnNextEnqueue()
        {
            FakeCloudFunctionService inner = new FakeCloudFunctionService();
            FakeConnectivityService connectivity = new FakeConnectivityService(false, NetworkReachability.NotReachable);
            BackendReliabilityConfig config = CreateConfig(autoFlushOnReconnect: false, persistentQueue: true);
            FakeQueueStore queueStore = new FakeQueueStore(new[]
            {
                new CloudFunctionQueueItem("fn.restore", "{\"id\":1}")
            });

            ReliableCloudFunctionService service = new ReliableCloudFunctionService(inner, connectivity, config, null, queueStore);
            service.Initialize();

            await service.ExecuteAsync("fn.next", "{\"id\":2}");

            Assert.That(queueStore.LoadCalls, Is.EqualTo(1));
            Assert.That(queueStore.LastSaved.Count, Is.EqualTo(2));
            Assert.That(queueStore.LastSaved[0].FunctionName, Is.EqualTo("fn.restore"));
            Assert.That(queueStore.LastSaved[1].FunctionName, Is.EqualTo("fn.next"));
            Assert.That(inner.ExecutedFunctionNames.Count, Is.EqualTo(0));

            service.Dispose();
        }

        [Test]
        public async Task ConnectivityChangedToOnline_FlushesRestoredQueue_AndClearsStore()
        {
            FakeCloudFunctionService inner = new FakeCloudFunctionService();
            FakeConnectivityService connectivity = new FakeConnectivityService(false, NetworkReachability.NotReachable);
            BackendReliabilityConfig config = CreateConfig(autoFlushOnReconnect: true, persistentQueue: true);
            FakeQueueStore queueStore = new FakeQueueStore(new[]
            {
                new CloudFunctionQueueItem("fn.restore", "{\"id\":1}")
            });
            SignalBus signalBus = CreateSignalBus();

            ReliableCloudFunctionService service = new ReliableCloudFunctionService(inner, connectivity, config, signalBus, queueStore);
            service.Initialize();

            connectivity.SetOnline(true, NetworkReachability.ReachableViaLocalAreaNetwork);
            signalBus.Fire(new ConnectivityChangedSignal(true, connectivity.Reachability));
            await UniTask.DelayFrame(2);

            Assert.That(inner.ExecutedFunctionNames.Count, Is.EqualTo(1));
            Assert.That(inner.ExecutedFunctionNames[0], Is.EqualTo("fn.restore"));
            Assert.That(queueStore.ClearCalls, Is.GreaterThanOrEqualTo(1));

            service.Dispose();
        }

        [Test]
        public async Task PersistentQueueDisabled_DoesNotReadOrWriteStore()
        {
            FakeCloudFunctionService inner = new FakeCloudFunctionService();
            FakeConnectivityService connectivity = new FakeConnectivityService(false, NetworkReachability.NotReachable);
            BackendReliabilityConfig config = CreateConfig(autoFlushOnReconnect: false, persistentQueue: false);
            FakeQueueStore queueStore = new FakeQueueStore(new[]
            {
                new CloudFunctionQueueItem("fn.restore", "{\"id\":1}")
            });

            ReliableCloudFunctionService service = new ReliableCloudFunctionService(inner, connectivity, config, null, queueStore);
            service.Initialize();
            await service.ExecuteAsync("fn.next");

            Assert.That(queueStore.LoadCalls, Is.EqualTo(0));
            Assert.That(queueStore.SaveCalls, Is.EqualTo(0));
            Assert.That(queueStore.ClearCalls, Is.EqualTo(0));

            service.Dispose();
        }

        private static BackendReliabilityConfig CreateConfig(bool autoFlushOnReconnect, bool persistentQueue)
        {
            BackendReliabilityConfig config = ScriptableObject.CreateInstance<BackendReliabilityConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_enableCloudFunctionQueue", true);
            ReflectionTestUtil.SetPrivateField(config, "_queueFailedCloudFunctions", true);
            ReflectionTestUtil.SetPrivateField(config, "_autoFlushQueueOnReconnect", autoFlushOnReconnect);
            ReflectionTestUtil.SetPrivateField(config, "_enablePersistentCloudFunctionQueue", persistentQueue);
            ReflectionTestUtil.SetPrivateField(config, "_maxQueuedCloudFunctions", 16);
            return config;
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<ConnectivityChangedSignal>();
            container.DeclareSignal<BackendOperationRetriedSignal>();
            container.DeclareSignal<CloudFunctionQueuedSignal>();
            container.DeclareSignal<CloudFunctionQueueFlushedSignal>();
            container.DeclareSignal<CloudFunctionQueueRestoredSignal>();
            return container.Resolve<SignalBus>();
        }

        private sealed class FakeCloudFunctionService : ICloudFunctionService
        {
            public readonly List<string> ExecutedFunctionNames = new List<string>();

            public UniTask<CloudFunctionResult> ExecuteAsync(string functionName, string payloadJson = null, CancellationToken cancellationToken = default)
            {
                ExecutedFunctionNames.Add(functionName);
                return UniTask.FromResult(new CloudFunctionResult(true, "{}", string.Empty));
            }
        }

        private sealed class FakeConnectivityService : IConnectivityService
        {
            public bool IsOnline { get; private set; }
            public NetworkReachability Reachability { get; private set; }

            public FakeConnectivityService(bool isOnline, NetworkReachability reachability)
            {
                IsOnline = isOnline;
                Reachability = reachability;
            }

            public void SetOnline(bool isOnline, NetworkReachability reachability)
            {
                IsOnline = isOnline;
                Reachability = reachability;
            }

            public void Refresh()
            {
            }
        }

        private sealed class FakeQueueStore : ICloudFunctionQueueStore
        {
            private readonly List<CloudFunctionQueueItem> _state;

            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public int ClearCalls { get; private set; }
            public List<CloudFunctionQueueItem> LastSaved { get; private set; } = new List<CloudFunctionQueueItem>();

            public FakeQueueStore(IReadOnlyList<CloudFunctionQueueItem> initial)
            {
                _state = new List<CloudFunctionQueueItem>(initial ?? new CloudFunctionQueueItem[0]);
            }

            public IReadOnlyList<CloudFunctionQueueItem> Load()
            {
                LoadCalls++;
                return new List<CloudFunctionQueueItem>(_state);
            }

            public void Save(IReadOnlyList<CloudFunctionQueueItem> queue)
            {
                SaveCalls++;
                LastSaved = new List<CloudFunctionQueueItem>(queue);
                _state.Clear();
                _state.AddRange(queue);
            }

            public void Clear()
            {
                ClearCalls++;
                _state.Clear();
                LastSaved = new List<CloudFunctionQueueItem>();
            }
        }
    }
}
