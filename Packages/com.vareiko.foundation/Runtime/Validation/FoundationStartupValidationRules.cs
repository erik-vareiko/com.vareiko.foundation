using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Save;
using Zenject;

namespace Vareiko.Foundation.Validation
{
    public sealed class SaveSecurityStartupValidationRule : IStartupValidationRule
    {
        private const string DefaultSecretKey = "replace-with-project-secret";
        private readonly SaveSecurityConfig _config;

        [Inject]
        public SaveSecurityStartupValidationRule([InjectOptional] SaveSecurityConfig config = null)
        {
            _config = config;
        }

        public string Name => "SaveSecurityConfig";

        public StartupValidationResult Validate()
        {
            if (_config == null)
            {
                return StartupValidationResult.Warning("SaveSecurityConfig is not assigned. Default save security behavior is used.");
            }

            if (_config.EnableEncryption && IsDefaultSecret(_config.SecretKey))
            {
                return StartupValidationResult.Fail("Save encryption is enabled but SecretKey is default/empty. Configure project-specific key.");
            }

            if (!_config.EnableRollingBackups)
            {
                return StartupValidationResult.Warning("Rolling backups are disabled in SaveSecurityConfig.");
            }

            return StartupValidationResult.Success("Save security configuration is valid.");
        }

        private static bool IsDefaultSecret(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return string.Equals(value.Trim(), DefaultSecretKey, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class BackendStartupValidationRule : IStartupValidationRule
    {
        private readonly BackendConfig _config;

        [Inject]
        public BackendStartupValidationRule([InjectOptional] BackendConfig config = null)
        {
            _config = config;
        }

        public string Name => "BackendConfig";

        public StartupValidationResult Validate()
        {
            if (_config == null || _config.Provider == BackendProviderType.None)
            {
                return StartupValidationResult.Warning("Backend provider is not configured. Runtime will use null backend services.");
            }

            if (_config.Provider == BackendProviderType.PlayFab && string.IsNullOrWhiteSpace(_config.TitleId))
            {
                return StartupValidationResult.Fail("Backend provider is PlayFab but TitleId is empty.");
            }

            return StartupValidationResult.Success("Backend configuration is valid.");
        }
    }

    public sealed class ObservabilityStartupValidationRule : IStartupValidationRule
    {
        private readonly ObservabilityConfig _config;

        [Inject]
        public ObservabilityStartupValidationRule([InjectOptional] ObservabilityConfig config = null)
        {
            _config = config;
        }

        public string Name => "ObservabilityConfig";

        public StartupValidationResult Validate()
        {
            if (_config == null)
            {
                return StartupValidationResult.Warning("ObservabilityConfig is not assigned. Default observability behavior is used.");
            }

            if (!_config.CaptureUnhandledExceptions)
            {
                return StartupValidationResult.Warning("Unhandled exception capture is disabled in ObservabilityConfig.");
            }

            if (_config.CaptureUnhandledExceptions && !_config.TransitionToErrorStateOnUnhandledException)
            {
                return StartupValidationResult.Warning("Unhandled exceptions are captured but app-state transition to Error is disabled.");
            }

            return StartupValidationResult.Success("Observability configuration is valid.");
        }
    }
}
