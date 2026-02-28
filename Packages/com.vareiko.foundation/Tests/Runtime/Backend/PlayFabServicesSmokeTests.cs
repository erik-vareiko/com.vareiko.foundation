using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Backend
{
    public sealed class PlayFabServicesSmokeTests
    {
        [Test]
        public async Task PlayFabBackendService_WhenNotConfigured_ReturnsExpectedFailure()
        {
            PlayFabBackendService service = new PlayFabBackendService(null, null);

            BackendAuthResult result = await service.LoginAnonymousAsync("player");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("PlayFab is not configured."));
            Assert.That(result.ErrorCode, Is.EqualTo(BackendErrorCode.ConfigurationInvalid));
            Assert.That(service.IsAuthenticated, Is.False);
            Assert.That(service.IsConfigured, Is.False);
        }

        [Test]
        public async Task PlayFabBackendService_WhenCustomIdIsEmpty_ReturnsValidationFailure()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");
                PlayFabBackendService service = new PlayFabBackendService(config, null);

                BackendAuthResult result = await service.LoginAnonymousAsync("   ");

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("CustomId is null or empty."));
                Assert.That(result.ErrorCode, Is.EqualTo(BackendErrorCode.ValidationFailed));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabBackendService_WhenAuthStateChangesOnFailure_EmitsSignal()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");

                SignalBus signalBus = CreateSignalBus();
                BackendAuthStateChangedSignal lastSignal = default;
                bool hasSignal = false;
                signalBus.Subscribe<BackendAuthStateChangedSignal>(signal =>
                {
                    lastSignal = signal;
                    hasSignal = true;
                });

                PlayFabBackendService service = new PlayFabBackendService(config, signalBus);
                ReflectionTestUtil.SetPrivateField(service, "_isAuthenticated", true);
                ReflectionTestUtil.SetPrivateField(service, "_playerId", "old");

                BackendAuthResult result = await service.LoginAnonymousAsync("");

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(BackendErrorCode.ValidationFailed));
                Assert.That(hasSignal, Is.True);
                Assert.That(lastSignal.IsAuthenticated, Is.False);
                Assert.That(lastSignal.PlayerId, Is.EqualTo(string.Empty));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabBackendService_WhenConfigured_MatchesSdkAvailability()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");

                PlayFabBackendService service = new PlayFabBackendService(config, null);
                BackendAuthResult result = await service.LoginAnonymousAsync("player42");

#if PLAYFAB_SDK
                Assert.That(result.Success, Is.True);
                Assert.That(service.IsAuthenticated, Is.True);
                Assert.That(result.PlayerId, Is.EqualTo("player42"));
                Assert.That(result.ErrorCode, Is.EqualTo(BackendErrorCode.None));
#else
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("PLAYFAB_SDK is not installed."));
                Assert.That(result.ErrorCode, Is.EqualTo(BackendErrorCode.DependencyMissing));
                Assert.That(service.IsAuthenticated, Is.False);
#endif
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabCloudFunctionService_WhenDisabled_ReturnsExpectedFailure()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_enableCloudFunctions", false);
                PlayFabCloudFunctionService service = new PlayFabCloudFunctionService(config);

                CloudFunctionResult result = await service.ExecuteAsync("doWork", "{}");

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Cloud functions are disabled in backend config."));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabCloudFunctionService_WhenEnabled_MatchesSdkAvailability()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");
                ReflectionTestUtil.SetPrivateField(config, "_enableCloudFunctions", true);
                PlayFabCloudFunctionService service = new PlayFabCloudFunctionService(config);

                CloudFunctionResult result = await service.ExecuteAsync("doWork", "{}");

#if PLAYFAB_SDK
                Assert.That(result.Success, Is.True);
                Assert.That(result.Error, Is.Empty);
#else
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("PLAYFAB_SDK is not installed."));
#endif
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabCloudFunctionService_WithEmptyName_ReturnsValidationFailure()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");
                ReflectionTestUtil.SetPrivateField(config, "_enableCloudFunctions", true);
                PlayFabCloudFunctionService service = new PlayFabCloudFunctionService(config);

                CloudFunctionResult result = await service.ExecuteAsync("  ", "{}");

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Cloud function name is null or empty."));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task PlayFabRemoteConfigService_SmokeFlow_IsStableWithoutSdk()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", "TEST_TITLE");
                ReflectionTestUtil.SetPrivateField(config, "_enableRemoteConfig", true);
                PlayFabRemoteConfigService service = new PlayFabRemoteConfigService(config);

                await service.RefreshAsync();

                Assert.That(service.IsReady, Is.True);
                Assert.That(service.TryGetString("missing", out string _), Is.False);
                Assert.That(service.TryGetInt("missing", out int _), Is.False);
                Assert.That(service.TryGetFloat("missing", out float _), Is.False);
                Assert.That(service.Snapshot().Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PlayFabServices_WithCanceledToken_Throw()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            PlayFabBackendService backend = new PlayFabBackendService(null, null);
            PlayFabCloudFunctionService cloudFunctions = new PlayFabCloudFunctionService(null);
            PlayFabRemoteConfigService remoteConfig = new PlayFabRemoteConfigService(null);

            Assert.ThrowsAsync<System.OperationCanceledException>(async () => await backend.LoginAnonymousAsync("id", cts.Token));
            Assert.ThrowsAsync<System.OperationCanceledException>(async () => await cloudFunctions.ExecuteAsync("fn", "{}", cts.Token));
            Assert.ThrowsAsync<System.OperationCanceledException>(async () => await remoteConfig.RefreshAsync(cts.Token));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<BackendAuthStateChangedSignal>();
            return container.Resolve<SignalBus>();
        }
    }
}
