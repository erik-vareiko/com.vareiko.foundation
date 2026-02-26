namespace Vareiko.Foundation.Localization
{
    public readonly struct LanguageChangedSignal
    {
        public readonly string PreviousLanguage;
        public readonly string CurrentLanguage;

        public LanguageChangedSignal(string previousLanguage, string currentLanguage)
        {
            PreviousLanguage = previousLanguage ?? string.Empty;
            CurrentLanguage = currentLanguage ?? string.Empty;
        }
    }

    public readonly struct LocalizationKeyMissingSignal
    {
        public readonly string Language;
        public readonly string Key;

        public LocalizationKeyMissingSignal(string language, string key)
        {
            Language = language ?? string.Empty;
            Key = key ?? string.Empty;
        }
    }
}
