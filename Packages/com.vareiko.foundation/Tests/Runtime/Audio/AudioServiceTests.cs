using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Audio;
using Vareiko.Foundation.Settings;
using Zenject;

namespace Vareiko.Foundation.Tests.Audio
{
    public sealed class AudioServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            CleanupRuntimeAudioRoots();
        }

        [Test]
        public void Initialize_UsesSettingsVolumes_AndEmitsInitialSignal()
        {
            SignalBus signalBus = CreateSignalBus();
            List<AudioVolumesChangedSignal> signals = new List<AudioVolumesChangedSignal>(2);
            signalBus.Subscribe<AudioVolumesChangedSignal>(signal => signals.Add(signal));

            FakeSettingsService settings = new FakeSettingsService(new GameSettings
            {
                MasterVolume = 0.7f,
                MusicVolume = 0.3f,
                SfxVolume = 0.4f
            });

            AudioService service = new AudioService(settings, signalBus);
            try
            {
                service.Initialize();

                Assert.That(signals.Count, Is.EqualTo(1));
                Assert.That(signals[0].Master, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(signals[0].Music, Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(signals[0].Sfx, Is.EqualTo(0.4f).Within(0.0001f));
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public void SettingsChangedSignal_UpdatesAudioVolumes()
        {
            SignalBus signalBus = CreateSignalBus();
            AudioVolumesChangedSignal last = default;
            signalBus.Subscribe<AudioVolumesChangedSignal>(signal => last = signal);

            AudioService service = new AudioService(null, signalBus);
            try
            {
                service.Initialize();

                signalBus.Fire(new SettingsChangedSignal(new GameSettings
                {
                    MasterVolume = 0.25f,
                    MusicVolume = 0.5f,
                    SfxVolume = 0.75f
                }));

                Assert.That(last.Master, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(last.Music, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(last.Sfx, Is.EqualTo(0.75f).Within(0.0001f));
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public void Setters_ClampValues_AndEmitSignals()
        {
            SignalBus signalBus = CreateSignalBus();
            AudioVolumesChangedSignal last = default;
            int fired = 0;
            signalBus.Subscribe<AudioVolumesChangedSignal>(signal =>
            {
                last = signal;
                fired++;
            });

            AudioService service = new AudioService(null, signalBus);
            try
            {
                service.Initialize();
                service.SetMasterVolume(-1f);
                service.SetMusicVolume(2f);
                service.SetSfxVolume(0.6f);
                service.PlayMusic(null);
                service.PlaySfx(null);
                service.StopMusic();

                Assert.That(fired, Is.EqualTo(4));
                Assert.That(last.Master, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(last.Music, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(last.Sfx, Is.EqualTo(0.6f).Within(0.0001f));
            }
            finally
            {
                service.Dispose();
            }
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<SettingsChangedSignal>();
            container.DeclareSignal<AudioVolumesChangedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static void CleanupRuntimeAudioRoots()
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && obj.name == "[Foundation] AudioService")
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        private sealed class FakeSettingsService : ISettingsService
        {
            public bool IsLoaded => true;
            public GameSettings Current { get; private set; }

            public FakeSettingsService(GameSettings current)
            {
                Current = current;
            }

            public UniTask LoadAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask SaveAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public void Apply(GameSettings settings, bool saveImmediately = false)
            {
                Current = settings;
            }
        }
    }
}
