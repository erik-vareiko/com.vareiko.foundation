using System;
using System.Collections.Generic;
using System.Globalization;
using Zenject;

namespace Vareiko.Foundation.Environment
{
    public sealed class EnvironmentService : IEnvironmentService, IInitializable
    {
        private readonly EnvironmentConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, EnvironmentConfig.Profile> _profiles = new Dictionary<string, EnvironmentConfig.Profile>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _activeProfileId = "dev";

        [Inject]
        public EnvironmentService([InjectOptional] EnvironmentConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
            BuildProfilesLookup();
        }

        public string ActiveProfileId => _activeProfileId;

        public void Initialize()
        {
            string profileId = _config != null ? _config.DefaultProfileId : _activeProfileId;
            string commandLineProfile;
            if (TryGetCommandLineProfile(out commandLineProfile))
            {
                profileId = commandLineProfile;
            }

            if (!TrySetActiveProfile(profileId))
            {
                TrySetActiveProfile(_config != null ? _config.DefaultProfileId : "dev");
            }
        }

        public bool Is(string profileId)
        {
            return string.Equals(_activeProfileId, NormalizeProfileId(profileId), StringComparison.OrdinalIgnoreCase);
        }

        public bool TrySetActiveProfile(string profileId)
        {
            string normalized = NormalizeProfileId(profileId);

            if (_profiles.Count > 0)
            {
                EnvironmentConfig.Profile profile;
                if (!_profiles.TryGetValue(normalized, out profile))
                {
                    return false;
                }

                ApplyProfile(normalized, profile);
                return true;
            }

            ApplyProfile(normalized, null);
            return true;
        }

        public bool TryGetString(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            if (_values.TryGetValue(key.Trim(), out value))
            {
                return true;
            }

            value = string.Empty;
            _signalBus?.Fire(new EnvironmentValueMissingSignal(_activeProfileId, key));
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return bool.TryParse(normalized, out value);
        }

        public IReadOnlyDictionary<string, string> Snapshot()
        {
            return new Dictionary<string, string>(_values);
        }

        private void ApplyProfile(string profileId, EnvironmentConfig.Profile profile)
        {
            _activeProfileId = profileId;
            _values.Clear();

            if (profile != null && profile.Values != null)
            {
                IReadOnlyList<EnvironmentConfig.EnvironmentValue> values = profile.Values;
                for (int i = 0; i < values.Count; i++)
                {
                    EnvironmentConfig.EnvironmentValue entry = values[i];
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    _values[entry.Key.Trim()] = entry.Value ?? string.Empty;
                }
            }

            _signalBus?.Fire(new EnvironmentProfileChangedSignal(_activeProfileId));
        }

        private void BuildProfilesLookup()
        {
            _profiles.Clear();
            if (_config == null || _config.Profiles == null)
            {
                return;
            }

            IReadOnlyList<EnvironmentConfig.Profile> profiles = _config.Profiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                EnvironmentConfig.Profile profile = profiles[i];
                if (profile == null)
                {
                    continue;
                }

                string id = NormalizeProfileId(profile.Id);
                _profiles[id] = profile;
            }
        }

        private bool TryGetCommandLineProfile(out string profileId)
        {
            profileId = string.Empty;
            if (_config != null && !_config.AllowCommandLineOverride)
            {
                return false;
            }

            string argName = _config != null ? _config.CommandLineArgName : "env";
            string shortName = "-" + argName;
            string longName = "--" + argName;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (TryParseInlineArgument(arg, longName, out profileId) || TryParseInlineArgument(arg, shortName, out profileId))
                {
                    return true;
                }

                if ((string.Equals(arg, longName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, shortName, StringComparison.OrdinalIgnoreCase)) &&
                    i + 1 < args.Length &&
                    !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    profileId = args[i + 1].Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseInlineArgument(string argument, string argName, out string value)
        {
            value = string.Empty;
            string prefix = argName + "=";
            if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string parsed = argument.Substring(prefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(parsed))
            {
                return false;
            }

            value = parsed;
            return true;
        }

        private static string NormalizeProfileId(string profileId)
        {
            return string.IsNullOrWhiteSpace(profileId) ? "dev" : profileId.Trim();
        }
    }
}
