using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Environment
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Environment Config")]
    public sealed class EnvironmentConfig : ScriptableObject
    {
        public const string StarterProfileDev = "dev";
        public const string StarterProfileStage = "stage";
        public const string StarterProfileProd = "prod";

        [Serializable]
        public struct EnvironmentValue
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        public sealed class Profile
        {
            [SerializeField] private string _id = "dev";
            [SerializeField] private List<EnvironmentValue> _values = new List<EnvironmentValue>();

            public string Id => string.IsNullOrWhiteSpace(_id) ? "dev" : _id.Trim();
            public IReadOnlyList<EnvironmentValue> Values => _values;

            internal void SetData(string profileId, List<EnvironmentValue> values)
            {
                _id = string.IsNullOrWhiteSpace(profileId) ? StarterProfileDev : profileId.Trim();
                _values = values ?? new List<EnvironmentValue>();
            }
        }

        [SerializeField] private string _defaultProfileId = "dev";
        [SerializeField] private bool _allowCommandLineOverride = true;
        [SerializeField] private string _commandLineArgName = "env";
        [SerializeField] private List<Profile> _profiles = new List<Profile>();

        public string DefaultProfileId => string.IsNullOrWhiteSpace(_defaultProfileId) ? "dev" : _defaultProfileId.Trim();
        public bool AllowCommandLineOverride => _allowCommandLineOverride;
        public string CommandLineArgName => string.IsNullOrWhiteSpace(_commandLineArgName) ? "env" : _commandLineArgName.Trim();
        public IReadOnlyList<Profile> Profiles => _profiles;

        public void ApplyStarterPresets()
        {
            _defaultProfileId = StarterProfileDev;
            _allowCommandLineOverride = true;
            _commandLineArgName = "env";
            _profiles = new List<Profile>(3)
            {
                CreateStarterProfile(
                    StarterProfileDev,
                    new EnvironmentValue { Key = "environment.name", Value = "dev" },
                    new EnvironmentValue { Key = "api.base_url", Value = "http://localhost:8080" },
                    new EnvironmentValue { Key = "backend.provider", Value = "none" },
                    new EnvironmentValue { Key = "feature.experimental", Value = "true" },
                    new EnvironmentValue { Key = "analytics.enabled", Value = "false" },
                    new EnvironmentValue { Key = "logging.level", Value = "debug" }),
                CreateStarterProfile(
                    StarterProfileStage,
                    new EnvironmentValue { Key = "environment.name", Value = "stage" },
                    new EnvironmentValue { Key = "api.base_url", Value = "https://stage.example.com" },
                    new EnvironmentValue { Key = "backend.provider", Value = "playfab" },
                    new EnvironmentValue { Key = "feature.experimental", Value = "true" },
                    new EnvironmentValue { Key = "analytics.enabled", Value = "true" },
                    new EnvironmentValue { Key = "logging.level", Value = "info" }),
                CreateStarterProfile(
                    StarterProfileProd,
                    new EnvironmentValue { Key = "environment.name", Value = "prod" },
                    new EnvironmentValue { Key = "api.base_url", Value = "https://api.example.com" },
                    new EnvironmentValue { Key = "backend.provider", Value = "playfab" },
                    new EnvironmentValue { Key = "feature.experimental", Value = "false" },
                    new EnvironmentValue { Key = "analytics.enabled", Value = "true" },
                    new EnvironmentValue { Key = "logging.level", Value = "warning" })
            };
        }

        private static Profile CreateStarterProfile(string profileId, params EnvironmentValue[] values)
        {
            Profile profile = new Profile();
            profile.SetData(profileId, values != null ? new List<EnvironmentValue>(values) : new List<EnvironmentValue>());
            return profile;
        }
    }
}
