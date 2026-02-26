using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Environment
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Environment Config")]
    public sealed class EnvironmentConfig : ScriptableObject
    {
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
        }

        [SerializeField] private string _defaultProfileId = "dev";
        [SerializeField] private bool _allowCommandLineOverride = true;
        [SerializeField] private string _commandLineArgName = "env";
        [SerializeField] private List<Profile> _profiles = new List<Profile>();

        public string DefaultProfileId => string.IsNullOrWhiteSpace(_defaultProfileId) ? "dev" : _defaultProfileId.Trim();
        public bool AllowCommandLineOverride => _allowCommandLineOverride;
        public string CommandLineArgName => string.IsNullOrWhiteSpace(_commandLineArgName) ? "env" : _commandLineArgName.Trim();
        public IReadOnlyList<Profile> Profiles => _profiles;
    }
}
