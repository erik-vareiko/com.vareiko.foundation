using System;
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
    public sealed class CloudCommandServiceTests
    {
        private const string IdempotencyKey = "01890f2e-5c5b-7b2f-a4ab-2f5bdcf18a22";

        [Test]
        public async Task ExecuteAsync_InvalidCommandName_ReturnsValidationError()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService();
            CloudCommandService service = new CloudCommandService(
                transport,
                CreateReliabilityConfig(),
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                new FakeCommandQueueStore(),
                new FakeConnectivityService(true, NetworkReachability.ReachableViaLocalAreaNetwork),
                null);

            CloudCommandResponse response = await service.ExecuteAsync(CreateRequest(string.Empty));

            Assert.That(response.Success, Is.False);
            Assert.That(response.ErrorCode, Is.EqualTo("Validation.CommandName"));
            Assert.That(transport.Executions.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteAsync_NullPayload_ReturnsValidationError()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService();
            CloudCommandService service = new CloudCommandService(
                transport,
                CreateReliabilityConfig(),
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                new FakeCommandQueueStore(),
                new FakeConnectivityService(true, NetworkReachability.ReachableViaLocalAreaNetwork),
                null);

            CloudCommandRequest request = new CloudCommandRequest(
                "StartRun",
                IdempotencyKey,
                "corr-1",
                "1",
                null,
                "player-1",
                1);
            CloudCommandResponse response = await service.ExecuteAsync(request);

            Assert.That(response.Success, Is.False);
            Assert.That(response.ErrorCode, Is.EqualTo("Validation.PayloadJson"));
            Assert.That(transport.Executions.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteAsync_Offline_QueuesRequest()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService();
            FakeCommandQueueStore queueStore = new FakeCommandQueueStore();
            CloudCommandService service = new CloudCommandService(
                transport,
                CreateReliabilityConfig(),
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                queueStore,
                new FakeConnectivityService(false, NetworkReachability.NotReachable),
                null);

            service.Initialize();
            CloudCommandResponse response = await service.ExecuteAsync(CreateRequest("StartRun"));
            service.Dispose();

            Assert.That(response.Success, Is.False);
            Assert.That(response.IsRetryable, Is.True);
            Assert.That(response.ErrorCode, Is.EqualTo("Queue.Offline"));
            Assert.That(queueStore.LastSaved.Count, Is.EqualTo(1));
            Assert.That(queueStore.LastSaved[0].IdempotencyKey, Is.EqualTo(IdempotencyKey));
            Assert.That(transport.Executions.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteAsync_DuplicateResponse_TreatedAsSuccess()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService
            {
                Handler = (_, __) => new CloudFunctionResult(
                    true,
                    "{\"Success\":false,\"IsRetryable\":false,\"ErrorCode\":\"Duplicate.Request\",\"ErrorMessage\":\"already processed\",\"ResponseJson\":\"{}\",\"ProcessedIdempotencyKey\":\"01890f2e-5c5b-7b2f-a4ab-2f5bdcf18a22\",\"ServerUnixMs\":123}",
                    string.Empty)
            };
            CloudCommandService service = new CloudCommandService(
                transport,
                CreateReliabilityConfig(),
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                new FakeCommandQueueStore(),
                new FakeConnectivityService(true, NetworkReachability.ReachableViaLocalAreaNetwork),
                null);

            CloudCommandResponse response = await service.ExecuteAsync(CreateRequest("ResolveBattle"));

            Assert.That(response.Success, Is.True);
            Assert.That(response.IsRetryable, Is.False);
            Assert.That(response.ProcessedIdempotencyKey, Is.EqualTo(IdempotencyKey));
        }

        [Test]
        public async Task ExecuteAsync_RetryableTransportFailure_QueuesRequest()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService
            {
                Handler = (_, __) => new CloudFunctionResult(false, string.Empty, "network timeout")
            };
            BackendReliabilityConfig reliability = CreateReliabilityConfig();
            ReflectionTestUtil.SetPrivateField(reliability, "_enableRetry", false);

            FakeCommandQueueStore queueStore = new FakeCommandQueueStore();
            CloudCommandService service = new CloudCommandService(
                transport,
                reliability,
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                queueStore,
                new FakeConnectivityService(true, NetworkReachability.ReachableViaLocalAreaNetwork),
                null);

            CloudCommandResponse response = await service.ExecuteAsync(CreateRequest("ClaimMilestoneReward"));

            Assert.That(response.Success, Is.False);
            Assert.That(response.IsRetryable, Is.True);
            Assert.That(queueStore.LastSaved.Count, Is.EqualTo(1));
            Assert.That(transport.Executions.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Initialize_RestoresQueue_AndFlushesOnReconnect()
        {
            FakeCloudFunctionService transport = new FakeCloudFunctionService
            {
                Handler = (_, __) => new CloudFunctionResult(true, "{\"Success\":true,\"IsRetryable\":false,\"ResponseJson\":\"{}\",\"ProcessedIdempotencyKey\":\"01890f2e-5c5b-7b2f-a4ab-2f5bdcf18a22\",\"ServerUnixMs\":1}", string.Empty)
            };
            FakeConnectivityService connectivity = new FakeConnectivityService(false, NetworkReachability.NotReachable);
            BackendReliabilityConfig reliability = CreateReliabilityConfig();
            FakeCommandQueueStore queueStore = new FakeCommandQueueStore(new[]
            {
                CloudCommandQueueItem.Create(
                    "CommandGateway",
                    "{\"CommandName\":\"StartRun\"}",
                    "{\"CommandName\":\"StartRun\",\"IdempotencyKey\":\"01890f2e-5c5b-7b2f-a4ab-2f5bdcf18a22\",\"CorrelationId\":\"c1\",\"RequestVersion\":\"1\",\"PayloadJson\":\"{}\",\"PlayerId\":\"p1\",\"ClientUnixMs\":1,\"Meta\":[]}",
                    IdempotencyKey,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            });
            SignalBus signalBus = CreateSignalBus();

            CloudCommandService service = new CloudCommandService(
                transport,
                reliability,
                ScriptableObject.CreateInstance<BackendCommandConfig>(),
                new CloudCommandRetryClassifier(),
                queueStore,
                connectivity,
                signalBus);

            service.Initialize();
            connectivity.SetOnline(true, NetworkReachability.ReachableViaCarrierDataNetwork);
            signalBus.Fire(new ConnectivityChangedSignal(true, connectivity.Reachability));
            await UniTask.DelayFrame(2);
            service.Dispose();

            Assert.That(transport.Executions.Count, Is.EqualTo(1));
            Assert.That(queueStore.ClearCalls, Is.GreaterThanOrEqualTo(1));
        }

        private static CloudCommandRequest CreateRequest(string commandName)
        {
            return new CloudCommandRequest(
                commandName,
                IdempotencyKey,
                "corr-1",
                "1",
                "{}",
                "player-1",
                1);
        }

        private static BackendReliabilityConfig CreateReliabilityConfig()
        {
            BackendReliabilityConfig config = ScriptableObject.CreateInstance<BackendReliabilityConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_enableRetry", true);
            ReflectionTestUtil.SetPrivateField(config, "_maxAttempts", 3);
            ReflectionTestUtil.SetPrivateField(config, "_initialDelayMs", 0);
            ReflectionTestUtil.SetPrivateField(config, "_enableCloudFunctionQueue", true);
            ReflectionTestUtil.SetPrivateField(config, "_queueFailedCloudFunctions", true);
            ReflectionTestUtil.SetPrivateField(config, "_autoFlushQueueOnReconnect", true);
            ReflectionTestUtil.SetPrivateField(config, "_enablePersistentCloudFunctionQueue", true);
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
            public readonly List<(string FunctionName, string PayloadJson)> Executions = new List<(string FunctionName, string PayloadJson)>();
            public Func<string, string, CloudFunctionResult> Handler { get; set; }

            public UniTask<CloudFunctionResult> ExecuteAsync(string functionName, string payloadJson = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Executions.Add((functionName, payloadJson));
                CloudFunctionResult result = Handler != null
                    ? Handler(functionName, payloadJson)
                    : new CloudFunctionResult(true, "{\"Success\":true,\"IsRetryable\":false,\"ResponseJson\":\"{}\",\"ProcessedIdempotencyKey\":\"\",\"ServerUnixMs\":1}", string.Empty);
                return UniTask.FromResult(result);
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

        private sealed class FakeCommandQueueStore : ICloudCommandQueueStore
        {
            private readonly List<CloudCommandQueueItem> _state;

            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public int ClearCalls { get; private set; }
            public List<CloudCommandQueueItem> LastSaved { get; private set; } = new List<CloudCommandQueueItem>();

            public FakeCommandQueueStore(IReadOnlyList<CloudCommandQueueItem> initial = null)
            {
                _state = new List<CloudCommandQueueItem>(initial ?? Array.Empty<CloudCommandQueueItem>());
            }

            public IReadOnlyList<CloudCommandQueueItem> Load()
            {
                LoadCalls++;
                return new List<CloudCommandQueueItem>(_state);
            }

            public void Save(IReadOnlyList<CloudCommandQueueItem> queue)
            {
                SaveCalls++;
                LastSaved = new List<CloudCommandQueueItem>(queue);
                _state.Clear();
                _state.AddRange(queue);
            }

            public void Clear()
            {
                ClearCalls++;
                _state.Clear();
                LastSaved = new List<CloudCommandQueueItem>();
            }
        }
    }
}
