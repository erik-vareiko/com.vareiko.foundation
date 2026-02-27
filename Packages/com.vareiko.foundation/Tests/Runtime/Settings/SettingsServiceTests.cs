using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Settings;

namespace Vareiko.Foundation.Tests.Settings
{
    public sealed class SettingsServiceTests
    {
        [Test]
        public async Task LoadAsync_PopulatesCurrent_AndMarksLoaded()
        {
            GameSettings loaded = new GameSettings
            {
                Language = "ru",
                MasterVolume = 0.4f,
                MusicVolume = 0.3f
            };

            FakeSaveService saveService = new FakeSaveService(loaded);
            SettingsService service = new SettingsService(saveService, null);

            await service.LoadAsync();

            Assert.That(service.IsLoaded, Is.True);
            Assert.That(service.Current.Language, Is.EqualTo("ru"));
            Assert.That(service.Current.MasterVolume, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(service.Current.MusicVolume, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(saveService.LoadCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task Apply_WithSaveImmediately_TriggersSave()
        {
            FakeSaveService saveService = new FakeSaveService(new GameSettings());
            SettingsService service = new SettingsService(saveService, null);
            await service.LoadAsync();

            GameSettings next = new GameSettings { Language = "de", VibrationEnabled = false };
            service.Apply(next, saveImmediately: true);
            await UniTask.DelayFrame(2);

            Assert.That(service.Current.Language, Is.EqualTo("de"));
            Assert.That(service.Current.VibrationEnabled, Is.False);
            Assert.That(saveService.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task Apply_Null_DoesNothing()
        {
            FakeSaveService saveService = new FakeSaveService(new GameSettings { Language = "en" });
            SettingsService service = new SettingsService(saveService, null);
            await service.LoadAsync();

            service.Apply(null, saveImmediately: true);
            await UniTask.DelayFrame(1);

            Assert.That(service.Current.Language, Is.EqualTo("en"));
            Assert.That(saveService.SaveCalls, Is.EqualTo(0));
        }

        private sealed class FakeSaveService : ISaveService
        {
            private GameSettings _loaded;
            public int SaveCalls { get; private set; }
            public int LoadCalls { get; private set; }

            public FakeSaveService(GameSettings loaded)
            {
                _loaded = loaded;
            }

            public UniTask SaveAsync<T>(string slot, string key, T model, CancellationToken cancellationToken = default)
            {
                SaveCalls++;
                if (model is GameSettings settings)
                {
                    _loaded = settings;
                }
                return UniTask.CompletedTask;
            }

            public UniTask<T> LoadAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
            {
                LoadCalls++;
                if (typeof(T) == typeof(GameSettings))
                {
                    object result = (object)_loaded ?? fallback;
                    return UniTask.FromResult((T)result);
                }

                return UniTask.FromResult(fallback);
            }

            public UniTask<bool> ExistsAsync(string slot, string key, CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(true);
            }

            public UniTask DeleteAsync(string slot, string key, CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
