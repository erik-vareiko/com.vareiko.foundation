using System.Collections.Generic;

namespace Vareiko.Foundation.Localization
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        string FallbackLanguage { get; }
        IReadOnlyList<string> SupportedLanguages { get; }
        bool HasLanguage(string languageCode);
        bool TrySetLanguage(string languageCode);
        bool TryGet(string key, out string value);
        string Get(string key, string fallback = "");
    }
}
