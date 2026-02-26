using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Localization;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Localization
{
    public sealed class LocalizationServiceTests
    {
        [Test]
        public void Get_UsesCurrentLanguageThenFallbackTable()
        {
            LocalizationConfig config = ScriptableObject.CreateInstance<LocalizationConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_defaultLanguage", "ru");
            ReflectionTestUtil.SetPrivateField(config, "_fallbackLanguage", "en");
            ReflectionTestUtil.SetPrivateField(config, "_allowSystemLanguageFallback", false);
            ReflectionTestUtil.SetPrivateField(config, "_allowCommandLineOverride", false);
            ReflectionTestUtil.SetPrivateField(config, "_tables", BuildTables());

            LocalizationService service = new LocalizationService(config, null);
            service.Initialize();

            Assert.That(service.CurrentLanguage, Is.EqualTo("ru"));
            Assert.That(service.Get("menu.play"), Is.EqualTo("Play"));
            Assert.That(service.Get("menu.quit"), Is.EqualTo("Выход"));
            Assert.That(service.Get("menu.unknown", "Fallback"), Is.EqualTo("Fallback"));
        }

        [Test]
        public void TrySetLanguage_FailsForUnknownLanguage()
        {
            LocalizationConfig config = ScriptableObject.CreateInstance<LocalizationConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_defaultLanguage", "en");
            ReflectionTestUtil.SetPrivateField(config, "_fallbackLanguage", "en");
            ReflectionTestUtil.SetPrivateField(config, "_allowSystemLanguageFallback", false);
            ReflectionTestUtil.SetPrivateField(config, "_allowCommandLineOverride", false);
            ReflectionTestUtil.SetPrivateField(config, "_tables", BuildTables());

            LocalizationService service = new LocalizationService(config, null);
            service.Initialize();

            Assert.That(service.TrySetLanguage("jp"), Is.False);
            Assert.That(service.CurrentLanguage, Is.EqualTo("en"));
        }

        private static List<LocalizationConfig.LocalizationTable> BuildTables()
        {
            LocalizationConfig.LocalizationTable english = new LocalizationConfig.LocalizationTable();
            ReflectionTestUtil.SetPrivateField(english, "_languageCode", "en");
            ReflectionTestUtil.SetPrivateField(english, "_entries", new List<LocalizationConfig.LocalizationEntry>
            {
                new LocalizationConfig.LocalizationEntry { Key = "menu.play", Value = "Play" },
                new LocalizationConfig.LocalizationEntry { Key = "menu.quit", Value = "Quit" }
            });

            LocalizationConfig.LocalizationTable russian = new LocalizationConfig.LocalizationTable();
            ReflectionTestUtil.SetPrivateField(russian, "_languageCode", "ru");
            ReflectionTestUtil.SetPrivateField(russian, "_entries", new List<LocalizationConfig.LocalizationEntry>
            {
                new LocalizationConfig.LocalizationEntry { Key = "menu.quit", Value = "Выход" }
            });

            return new List<LocalizationConfig.LocalizationTable> { english, russian };
        }
    }
}
