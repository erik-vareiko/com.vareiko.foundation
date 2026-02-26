using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.App;
using Vareiko.Foundation.AssetManagement;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Loading;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class FoundationDiagnosticsServiceTests
    {
        [Test]
        public void InitializeAndTick_UpdatesSnapshot_RespectingRefreshInterval()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider { Time = 10f };
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_diagnosticsRefreshIntervalSeconds", 1f);

            SignalBus signalBus = CreateSignalBus();
            int updates = 0;
            signalBus.Subscribe<DiagnosticsSnapshotUpdatedSignal>(_ => updates++);

            FakeConnectivityService connectivity = new FakeConnectivityService { IsOnline = true };
            FakeLoadingService loading = new FakeLoadingService { IsLoading = true, Progress = 0.35f };
            FakeBackendService backend = new FakeBackendService { IsConfigured = true, IsAuthenticated = false };
            FakeRemoteConfigService remoteConfig = new FakeRemoteConfigService(new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            });
            FakeAssetService assetService = new FakeAssetService { TrackedAssetCount = 3, TotalReferenceCount = 7 };

            FoundationDiagnosticsService service = new FoundationDiagnosticsService(
                timeProvider,
                config,
                connectivity,
                loading,
                backend,
                remoteConfig,
                assetService,
                signalBus);

            try
            {
                service.Initialize();

                Assert.That(updates, Is.EqualTo(1));
                Assert.That(service.Snapshot.IsOnline, Is.True);
                Assert.That(service.Snapshot.IsLoading, Is.True);
                Assert.That(service.Snapshot.LoadingProgress, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(service.Snapshot.IsBackendConfigured, Is.True);
                Assert.That(service.Snapshot.IsBackendAuthenticated, Is.False);
                Assert.That(service.Snapshot.RemoteConfigValues, Is.EqualTo(2));
                Assert.That(service.Snapshot.TrackedAssets, Is.EqualTo(3));
                Assert.That(service.Snapshot.AssetReferences, Is.EqualTo(7));
                Assert.That(service.Snapshot.LastUpdatedAt, Is.EqualTo(10f).Within(0.0001f));

                timeProvider.Time = 10.5f;
                backend.IsAuthenticated = true;
                loading.Progress = 0.8f;
                service.Tick();
                Assert.That(updates, Is.EqualTo(1));
                Assert.That(service.Snapshot.IsBackendAuthenticated, Is.False);

                timeProvider.Time = 11.1f;
                service.Tick();
                Assert.That(updates, Is.EqualTo(2));
                Assert.That(service.Snapshot.IsBackendAuthenticated, Is.True);
                Assert.That(service.Snapshot.LoadingProgress, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(service.Snapshot.LastUpdatedAt, Is.EqualTo(11.1f).Within(0.0001f));
            }
            finally
            {
                service.Dispose();
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BootAndStateSignals_UpdateBootFlags()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider { Time = 0f };
            SignalBus signalBus = CreateSignalBus();
            FoundationDiagnosticsService service = new FoundationDiagnosticsService(timeProvider, null, null, null, null, null, null, signalBus);

            service.Initialize();
            signalBus.Fire(new ApplicationBootFailedSignal("init", "fail"));

            Assert.That(service.Snapshot.IsBootFailed, Is.True);
            Assert.That(service.Snapshot.IsBootCompleted, Is.False);
            Assert.That(service.Snapshot.LastBootError, Is.EqualTo("fail"));

            signalBus.Fire(new AppStateChangedSignal(AppState.Boot, AppState.MainMenu));

            Assert.That(service.Snapshot.IsBootCompleted, Is.True);
            Assert.That(service.Snapshot.IsBootFailed, Is.False);
            Assert.That(service.Snapshot.LastBootError, Is.Empty);

            signalBus.Fire(new ApplicationBootCompletedSignal(3));
            Assert.That(service.Snapshot.IsBootCompleted, Is.True);
            Assert.That(service.Snapshot.IsBootFailed, Is.False);
            Assert.That(service.Snapshot.LastBootError, Is.Empty);

            service.Dispose();
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<ApplicationBootCompletedSignal>();
            container.DeclareSignal<ApplicationBootFailedSignal>();
            container.DeclareSignal<AppStateChangedSignal>();
            container.DeclareSignal<DiagnosticsSnapshotUpdatedSignal>();
            return container.Resolve<SignalBus>();
        }

        private sealed class FakeLoadingService : ILoadingService
        {
            public bool IsLoading { get; set; }
            public float Progress { get; set; }
            public string ActiveOperation { get; private set; } = string.Empty;

            public void BeginManual(string operationName)
            {
                ActiveOperation = operationName ?? string.Empty;
                IsLoading = true;
            }

            public void SetManualProgress(float progress)
            {
                Progress = Mathf.Clamp01(progress);
            }

            public void CompleteManual()
            {
                IsLoading = false;
                Progress = 1f;
                ActiveOperation = string.Empty;
            }
        }

        private sealed class FakeBackendService : IBackendService
        {
            public BackendProviderType Provider => BackendProviderType.None;
            public bool IsConfigured { get; set; }
            public bool IsAuthenticated { get; set; }

            public UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(new BackendAuthResult(true, customId ?? string.Empty, string.Empty));
            }

            public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(new BackendPlayerDataResult(true, new Dictionary<string, string>(0), string.Empty));
            }

            public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(true);
            }
        }

        private sealed class FakeAssetService : IAssetService
        {
            public AssetProviderType ActiveProvider => AssetProviderType.Resources;
            public int TrackedAssetCount { get; set; }
            public int TotalReferenceCount { get; set; }

            public UniTask<AssetLoadResult<T>> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(AssetLoadResult<T>.Fail("not used in test"));
            }

            public UniTask<bool> ReleaseAsync(Object asset, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(true);
            }

            public UniTask WarmupAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
