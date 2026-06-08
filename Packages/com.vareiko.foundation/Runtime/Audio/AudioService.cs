using System;
using System.Collections.Generic;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Signals;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Audio
{
    public sealed class AudioService : IAudioService, IInitializable, IDisposable
    {
        private readonly IFoundationSignalBus _signalBus;
        private readonly ISettingsService _settingsService;
        private readonly List<IDisposable> _signalSubscriptions = new List<IDisposable>();

        private GameObject _root;
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        [Inject]
        public AudioService([InjectOptional] ISettingsService settingsService = null, [InjectOptional] IFoundationSignalBus signalBus = null)
        {
            _signalBus = signalBus;
            _settingsService = settingsService;
        }

        public void Initialize()
        {
            if (_signalBus != null)
            {
                _signalSubscriptions.Add(_signalBus.Subscribe<SettingsChangedSignal>(HandleSettingsChanged));
            }

            if (_settingsService != null && _settingsService.Current != null)
            {
                SetVolumeState(
                    _settingsService.Current.MasterVolume,
                    _settingsService.Current.MusicVolume,
                    _settingsService.Current.SfxVolume);
            }
            else
            {
                SetVolumeState(1f, 1f, 1f);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _signalSubscriptions.Count; i++)
            {
                _signalSubscriptions[i].Dispose();
            }
            _signalSubscriptions.Clear();

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
                EnsureRuntimeRoot();
            }

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
                EnsureRuntimeRoot();
            }

            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _masterVolume * _sfxVolume);
        }

        private void EnsureRuntimeRoot()
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
            RefreshSourceVolumes();
        }

        private void HandleSettingsChanged(SettingsChangedSignal signal)
        {
            GameSettings settings = signal.Settings;
            if (settings == null)
            {
                return;
            }

            SetVolumeState(settings.MasterVolume, settings.MusicVolume, settings.SfxVolume);
            FireVolumeSignal();
        }

        private void SetVolumeState(float master, float music, float sfx)
        {
            _masterVolume = Mathf.Clamp01(master);
            _musicVolume = Mathf.Clamp01(music);
            _sfxVolume = Mathf.Clamp01(sfx);
            RefreshSourceVolumes();
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
            _signalBus?.Publish(new AudioVolumesChangedSignal(_masterVolume, _musicVolume, _sfxVolume));
        }
    }
}
