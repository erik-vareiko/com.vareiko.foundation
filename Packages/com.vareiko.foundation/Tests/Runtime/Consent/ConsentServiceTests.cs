using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Consent
{
    public sealed class ConsentServiceTests
    {
        // --- State behaviour ---

        [Test]
        public void NewService_IsNotLoaded_AndGrantsNothing()
        {
            ConsentService service = new ConsentService(new FakeSaveService(), new FakeSignalBus());

            Assert.That(service.IsLoaded, Is.False);
            Assert.That(service.IsConsentCollected, Is.False);
            Assert.That(service.HasConsent(ConsentScope.Analytics), Is.False);
            Assert.That(service.HasConsent(ConsentScope.Advertising), Is.False);
        }

        [Test]
        public void SetConsent_UpdatesOnlyTargetScope()
        {
            ConsentService service = new ConsentService(new FakeSaveService(), new FakeSignalBus());

            service.SetConsent(ConsentScope.Analytics, true);

            Assert.That(service.HasConsent(ConsentScope.Analytics), Is.True);
            Assert.That(service.HasConsent(ConsentScope.Advertising), Is.False);
        }

        [Test]
        public void SetConsentCollected_SetsCollectedFlag()
        {
            ConsentService service = new ConsentService(new FakeSaveService(), new FakeSignalBus());

            service.SetConsentCollected(true);

            Assert.That(service.IsConsentCollected, Is.True);
        }

        [Test]
        public async Task SaveAsync_WritesToStorage()
        {
            FakeSaveService storage = new FakeSaveService();
            ConsentService service = new ConsentService(storage, new FakeSignalBus());
            service.SetConsent(ConsentScope.Advertising, true);

            await service.SaveAsync();

            Assert.That(storage.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadAsync_RestoresPersistedState_AndMarksLoaded()
        {
            FakeSaveService storage = new FakeSaveService();
            storage.Preload(new ConsentState { IsCollected = true, Analytics = true });
            ConsentService service = new ConsentService(storage, new FakeSignalBus());

            await service.LoadAsync();

            Assert.That(service.IsLoaded, Is.True);
            Assert.That(service.IsConsentCollected, Is.True);
            Assert.That(service.HasConsent(ConsentScope.Analytics), Is.True);
            Assert.That(service.HasConsent(ConsentScope.Advertising), Is.False);
        }

        [Test]
        public async Task SaveThenLoad_RoundTripsState()
        {
            FakeSaveService storage = new FakeSaveService();
            ConsentService writer = new ConsentService(storage, new FakeSignalBus());
            writer.SetConsent(ConsentScope.PushNotifications, true);
            await writer.SaveAsync();

            ConsentService reader = new ConsentService(storage, new FakeSignalBus());
            await reader.LoadAsync();

            Assert.That(reader.HasConsent(ConsentScope.PushNotifications), Is.True);
        }

        // --- Signal behaviour ---

        [Test]
        public void SetConsent_FiresChangedSignalForScope()
        {
            FakeSignalBus bus = new FakeSignalBus();
            List<ConsentChangedSignal> changes = new List<ConsentChangedSignal>();
            bus.Subscribe<ConsentChangedSignal>(changes.Add);
            ConsentService service = new ConsentService(new FakeSaveService(), bus);

            service.SetConsent(ConsentScope.Analytics, true);

            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Scope, Is.EqualTo(ConsentScope.Analytics));
            Assert.That(changes[0].Granted, Is.True);
        }

        [Test]
        public void SetConsentCollected_FiresChangedForEveryScope()
        {
            FakeSignalBus bus = new FakeSignalBus();
            List<ConsentChangedSignal> changes = new List<ConsentChangedSignal>();
            bus.Subscribe<ConsentChangedSignal>(changes.Add);
            ConsentService service = new ConsentService(new FakeSaveService(), bus);

            service.SetConsentCollected(true);

            Assert.That(changes, Has.Count.EqualTo(4));
            Assert.That(changes.TrueForAll(s => s.IsCollected), Is.True);
        }

        [Test]
        public async Task LoadAsync_FiresLoadedSignal()
        {
            FakeSignalBus bus = new FakeSignalBus();
            List<ConsentLoadedSignal> loaded = new List<ConsentLoadedSignal>();
            bus.Subscribe<ConsentLoadedSignal>(loaded.Add);
            FakeSaveService storage = new FakeSaveService();
            storage.Preload(new ConsentState { IsCollected = true });
            ConsentService service = new ConsentService(storage, bus);

            await service.LoadAsync();

            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].IsCollected, Is.True);
        }

        private sealed class FakeSaveService : ISaveService
        {
            private readonly Dictionary<string, object> _store = new Dictionary<string, object>(StringComparer.Ordinal);

            public int SaveCount { get; private set; }

            public void Preload(ConsentState state)
            {
                _store[Key("global", "consent")] = state;
            }

            public UniTask SaveAsync<T>(string slot, string key, T model, CancellationToken cancellationToken = default)
            {
                SaveCount++;
                _store[Key(slot, key)] = model;
                return UniTask.CompletedTask;
            }

            public UniTask<T> LoadAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
            {
                if (_store.TryGetValue(Key(slot, key), out object value) && value is T typed)
                {
                    return UniTask.FromResult(typed);
                }

                return UniTask.FromResult(fallback);
            }

            public UniTask<bool> ExistsAsync(string slot, string key, CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(_store.ContainsKey(Key(slot, key)));
            }

            public UniTask DeleteAsync(string slot, string key, CancellationToken cancellationToken = default)
            {
                _store.Remove(Key(slot, key));
                return UniTask.CompletedTask;
            }

            private static string Key(string slot, string key)
            {
                return slot + "/" + key;
            }
        }
    }
}
