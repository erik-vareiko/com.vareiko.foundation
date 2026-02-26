using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Localization
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Localization Config")]
    public sealed class LocalizationConfig : ScriptableObject
    {
        [Serializable]
        public struct LocalizationEntry
        {
            public string Key;
            [TextArea(1, 4)] public string Value;
        }

        [Serializable]
        public sealed class LocalizationTable
        {
            [SerializeField] private string _languageCode = "en";
            [SerializeField] private List<LocalizationEntry> _entries = new List<LocalizationEntry>();

            public string LanguageCode => string.IsNullOrWhiteSpace(_languageCode) ? "en" : _languageCode.Trim();
            public IReadOnlyList<LocalizationEntry> Entries => _entries;
        }

        [SerializeField] private string _defaultLanguage = "en";
        [SerializeField] private string _fallbackLanguage = "en";
        [SerializeField] private bool _allowSystemLanguageFallback = true;
        [SerializeField] private bool _allowCommandLineOverride = true;
        [SerializeField] private string _commandLineArgName = "lang";
        [SerializeField] private List<LocalizationTable> _tables = new List<LocalizationTable>();

        public string DefaultLanguage => string.IsNullOrWhiteSpace(_defaultLanguage) ? "en" : _defaultLanguage.Trim();
        public string FallbackLanguage => string.IsNullOrWhiteSpace(_fallbackLanguage) ? "en" : _fallbackLanguage.Trim();
        public bool AllowSystemLanguageFallback => _allowSystemLanguageFallback;
        public bool AllowCommandLineOverride => _allowCommandLineOverride;
        public string CommandLineArgName => string.IsNullOrWhiteSpace(_commandLineArgName) ? "lang" : _commandLineArgName.Trim();
        public IReadOnlyList<LocalizationTable> Tables => _tables;
    }
}
