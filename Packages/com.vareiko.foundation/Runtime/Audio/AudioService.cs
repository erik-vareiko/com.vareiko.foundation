using System;
using Vareiko.Foundation.Settings;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Audio
{
    public sealed class AudioService : IAudioService, IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly ISettingsService _settingsService;

        private GameObject _root;
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        [Inject]
        public AudioService([InjectOptional] ISettingsService settingsService = null, [InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
            _settingsService = settingsService;
        }

        public void Initialize()
        {
            CreateRuntimeRoot();
            if (_signalBus != null)
            {
                _signalBus.Subscribe<SettingsChangedSignal>(HandleSettingsChanged);
            }

            if (_settingsService != null && _settingsService.Current != null)
            {
                ApplyVolumes(
                    _settingsService.Current.MasterVolume,
                    _settingsService.Current.MusicVolume,
                    _settingsService.Current.SfxVolume);
            }
            else
            {
                ApplyVolumes(1f, 1f, 1f);
            }
        }

        public void Dispose()
        {
            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<SettingsChangedSignal>(HandleSettingsChanged);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            RefreshSourceVolumes();
            FireVolumeSignal();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            RefreshSourceVolumes();
            FireVolumeSignal();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            RefreshSourceVolumes();
            FireVolumeSignal();
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float volumeScale = 1f)
        {
            if (_musicSource == null || clip == null)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = Mathf.Clamp01(volumeScale) * _masterVolume * _musicVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
            }
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _masterVolume * _sfxVolume);
        }

        private void CreateRuntimeRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("[Foundation] AudioService");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            _musicSource = _root.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;

            _sfxSource = _root.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        private void HandleSettingsChanged(SettingsChangedSignal signal)
        {
            GameSettings settings = signal.Settings;
            if (settings == null)
            {
                return;
            }

            ApplyVolumes(settings.MasterVolume, settings.MusicVolume, settings.SfxVolume);
        }

        private void ApplyVolumes(float master, float music, float sfx)
        {
            _masterVolume = Mathf.Clamp01(master);
            _musicVolume = Mathf.Clamp01(music);
            _sfxVolume = Mathf.Clamp01(sfx);
            RefreshSourceVolumes();
            FireVolumeSignal();
        }

        private void RefreshSourceVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _masterVolume * _musicVolume;
            }
        }

        private void FireVolumeSignal()
        {
            _signalBus?.Fire(new AudioVolumesChangedSignal(_masterVolume, _musicVolume, _sfxVolume));
        }
    }
}
