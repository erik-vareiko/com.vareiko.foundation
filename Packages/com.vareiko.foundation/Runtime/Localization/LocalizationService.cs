using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Localization
{
    public sealed class LocalizationService : ILocalizationService, IInitializable
    {
        private readonly LocalizationConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _supportedLanguages = new List<string>();

        private string _currentLanguage = "en";
        private string _fallbackLanguage = "en";

        [Inject]
        public LocalizationService([InjectOptional] LocalizationConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
            BuildTablesLookup();
        }

        public string CurrentLanguage => _currentLanguage;
        public string FallbackLanguage => _fallbackLanguage;
        public IReadOnlyList<string> SupportedLanguages => _supportedLanguages;

        public void Initialize()
        {
            _fallbackLanguage = ResolveFallbackLanguage();
            string initialLanguage = ResolveInitialLanguage();
            if (!TrySetLanguage(initialLanguage))
            {
                if (!TrySetLanguage(_fallbackLanguage) && _supportedLanguages.Count > 0)
                {
                    TrySetLanguage(_supportedLanguages[0]);
                }
            }
        }

        public bool HasLanguage(string languageCode)
        {
            return _tables.ContainsKey(NormalizeLanguageCode(languageCode));
        }

        public bool TrySetLanguage(string languageCode)
        {
            string normalized = NormalizeLanguageCode(languageCode);
            if (!_tables.ContainsKey(normalized))
            {
                return false;
            }

            if (string.Equals(_currentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string previous = _currentLanguage;
            _currentLanguage = normalized;
            _signalBus?.Fire(new LanguageChangedSignal(previous, _currentLanguage));
            return true;
        }

        public bool TryGet(string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string normalizedKey = key.Trim();
            if (TryGetFromTable(_currentLanguage, normalizedKey, out value))
            {
                return true;
            }

            if (!string.Equals(_fallbackLanguage, _currentLanguage, StringComparison.OrdinalIgnoreCase) &&
                TryGetFromTable(_fallbackLanguage, normalizedKey, out value))
            {
                return true;
            }

            _signalBus?.Fire(new LocalizationKeyMissingSignal(_currentLanguage, normalizedKey));
            return false;
        }

        public string Get(string key, string fallback = "")
        {
            string value;
            if (TryGet(key, out value))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }

            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }

        private bool TryGetFromTable(string languageCode, string key, out string value)
        {
            value = string.Empty;
            Dictionary<string, string> table;
            if (string.IsNullOrWhiteSpace(languageCode) || !_tables.TryGetValue(languageCode, out table))
            {
                return false;
            }

            return table.TryGetValue(key, out value);
        }

        private void BuildTablesLookup()
        {
            _tables.Clear();
            _supportedLanguages.Clear();

            if (_config == null || _config.Tables == null)
            {
                return;
            }

            IReadOnlyList<LocalizationConfig.LocalizationTable> tables = _config.Tables;
            for (int i = 0; i < tables.Count; i++)
            {
                LocalizationConfig.LocalizationTable tableConfig = tables[i];
                if (tableConfig == null)
                {
                    continue;
                }

                string language = NormalizeLanguageCode(tableConfig.LanguageCode);
                Dictionary<string, string> table;
                if (!_tables.TryGetValue(language, out table))
                {
                    table = new Dictionary<string, string>(StringComparer.Ordinal);
                    _tables[language] = table;
                    _supportedLanguages.Add(language);
                }

                IReadOnlyList<LocalizationConfig.LocalizationEntry> entries = tableConfig.Entries;
                if (entries == null)
                {
                    continue;
                }

                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    LocalizationConfig.LocalizationEntry entry = entries[entryIndex];
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    table[entry.Key.Trim()] = entry.Value ?? string.Empty;
                }
            }
        }

        private string ResolveFallbackLanguage()
        {
            string configFallback = _config != null ? _config.FallbackLanguage : "en";
            if (HasLanguage(configFallback))
            {
                return NormalizeLanguageCode(configFallback);
            }

            string configDefault = _config != null ? _config.DefaultLanguage : "en";
            if (HasLanguage(configDefault))
            {
                return NormalizeLanguageCode(configDefault);
            }

            if (_supportedLanguages.Count > 0)
            {
                return _supportedLanguages[0];
            }

            return "en";
        }

        private string ResolveInitialLanguage()
        {
            string commandLineLanguage;
            if (TryGetCommandLineLanguage(out commandLineLanguage) && HasLanguage(commandLineLanguage))
            {
                return commandLineLanguage;
            }

            if (_config != null && HasLanguage(_config.DefaultLanguage))
            {
                return _config.DefaultLanguage;
            }

            if (_config == null || _config.AllowSystemLanguageFallback)
            {
                string systemLanguage = MapSystemLanguage(Application.systemLanguage);
                if (HasLanguage(systemLanguage))
                {
                    return systemLanguage;
                }
            }

            return _fallbackLanguage;
        }

        private bool TryGetCommandLineLanguage(out string languageCode)
        {
            languageCode = string.Empty;
            if (_config != null && !_config.AllowCommandLineOverride)
            {
                return false;
            }

            string argName = _config != null ? _config.CommandLineArgName : "lang";
            string shortName = "-" + argName;
            string longName = "--" + argName;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (TryParseInlineArgument(arg, longName, out languageCode) ||
                    TryParseInlineArgument(arg, shortName, out languageCode))
                {
                    return true;
                }

                if ((string.Equals(arg, longName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, shortName, StringComparison.OrdinalIgnoreCase)) &&
                    i + 1 < args.Length &&
                    !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    languageCode = args[i + 1].Trim();
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

        private static string MapSystemLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Russian:
                    return "ru";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Italian:
                    return "it";
                case SystemLanguage.Portuguese:
                    return "pt";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.Korean:
                    return "ko";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-Hans";
                case SystemLanguage.ChineseTraditional:
                    return "zh-Hant";
                case SystemLanguage.Turkish:
                    return "tr";
                default:
                    return "en";
            }
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
        }
    }
}
