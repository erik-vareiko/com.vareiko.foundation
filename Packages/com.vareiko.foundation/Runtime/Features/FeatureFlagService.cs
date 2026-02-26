using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Backend;
using Zenject;

namespace Vareiko.Foundation.Features
{
    public sealed class FeatureFlagService : IFeatureFlagService, IInitializable
    {
        private readonly IRemoteConfigService _remoteConfigService;
        private readonly FeatureFlagsConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, bool> _defaultBools = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _overrides = new Dictionary<string, bool>(System.StringComparer.Ordinal);

        [Inject]
        public FeatureFlagService(
            [InjectOptional] IRemoteConfigService remoteConfigService = null,
            [InjectOptional] FeatureFlagsConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _remoteConfigService = remoteConfigService;
            _config = config;
            _signalBus = signalBus;
            BuildDefaults();
        }

        public void Initialize()
        {
            if (_config == null || !_config.RefreshOnInitialize)
            {
                return;
            }

            RefreshSafeAsync().Forget();
        }

        public bool IsEnabled(string key, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            bool overrideValue;
            if (_overrides.TryGetValue(key, out overrideValue))
            {
                return overrideValue;
            }

            string rawValue;
            if (_remoteConfigService != null && _remoteConfigService.TryGetString(key, out rawValue))
            {
                bool parsed;
                if (TryParseBool(rawValue, out parsed))
                {
                    return parsed;
                }
            }

            bool defaultValue;
            if (_defaultBools.TryGetValue(key, out defaultValue))
            {
                return defaultValue;
            }

            return fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            int value;
            if (_remoteConfigService != null && _remoteConfigService.TryGetInt(key, out value))
            {
                return value;
            }

            return fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            float value;
            if (_remoteConfigService != null && _remoteConfigService.TryGetFloat(key, out value))
            {
                return value;
            }

            return fallback;
        }

        public string GetString(string key, string fallback = "")
        {
            string value;
            if (_remoteConfigService != null && _remoteConfigService.TryGetString(key, out value))
            {
                return value;
            }

            return fallback ?? string.Empty;
        }

        public void SetLocalOverride(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _overrides[key] = value;
            _signalBus?.Fire(new FeatureFlagOverriddenSignal(key, value));
        }

        public void ClearLocalOverrides()
        {
            _overrides.Clear();
        }

        public async UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (_remoteConfigService == null)
            {
                _signalBus?.Fire(new FeatureFlagsRefreshedSignal(0));
                return;
            }

            await _remoteConfigService.RefreshAsync(cancellationToken);
            IReadOnlyDictionary<string, string> snapshot = _remoteConfigService.Snapshot();
            int count = snapshot != null ? snapshot.Count : 0;
            _signalBus?.Fire(new FeatureFlagsRefreshedSignal(count));
        }

        private async UniTaskVoid RefreshSafeAsync()
        {
            try
            {
                await RefreshAsync();
            }
            catch (System.OperationCanceledException)
            {
            }
            catch
            {
            }
        }

        private void BuildDefaults()
        {
            _defaultBools.Clear();
            if (_config == null || _config.BoolFeatures == null)
            {
                return;
            }

            FeatureFlagsConfig.BoolFeature[] features = _config.BoolFeatures;
            for (int i = 0; i < features.Length; i++)
            {
                FeatureFlagsConfig.BoolFeature feature = features[i];
                if (string.IsNullOrWhiteSpace(feature.Key))
                {
                    continue;
                }

                _defaultBools[feature.Key] = feature.DefaultValue;
            }
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = false;
                return false;
            }

            string normalized = raw.Trim();
            if (string.Equals(normalized, "1", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "true", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "on", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "yes", System.StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(normalized, "0", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "false", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "off", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "no", System.StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return bool.TryParse(normalized, out value);
        }
    }
}
