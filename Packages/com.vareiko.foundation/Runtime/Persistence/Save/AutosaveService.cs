using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Time;
using Vareiko.Foundation.Signals;
using UnityEngine;

namespace Vareiko.Foundation.Save
{
    public sealed class AutosaveService : VContainer.Unity.IInitializable, VContainer.Unity.ITickable, System.IDisposable
    {
        private readonly AutosaveConfig _config;
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly ISettingsService _settingsService;
        private readonly IConsentService _consentService;
        private readonly IFoundationSignalBus _signalBus;
        private readonly IApplicationLifecycleService _applicationLifecycleService;
        private readonly List<IDisposable> _signalSubscriptions = new List<IDisposable>();

        private bool _dirtySettings;
        private bool _dirtyConsent;
        private bool _isSaving;
        private float _nextAutosaveAt;
        private AutosaveLifecycleHook _lifecycleHook;

        public AutosaveService(
            IFoundationTimeProvider timeProvider,
            AutosaveConfig config = null,
            ISettingsService settingsService = null,
            IConsentService consentService = null,
            IFoundationSignalBus signalBus = null,
            IApplicationLifecycleService applicationLifecycleService = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _settingsService = settingsService;
            _consentService = consentService;
            _signalBus = signalBus;
            _applicationLifecycleService = applicationLifecycleService;
        }

        public void Initialize()
        {
            _nextAutosaveAt = _timeProvider.Time + GetIntervalSeconds();

            if (_signalBus != null)
            {
                _signalSubscriptions.Add(_signalBus.Subscribe<SettingsChangedSignal>(OnSettingsChanged));
                _signalSubscriptions.Add(_signalBus.Subscribe<ConsentChangedSignal>(OnConsentChanged));
            }

            if (ShouldSaveOnPause())
            {
                if (_applicationLifecycleService != null)
                {
                    _applicationLifecycleService.PauseChanged += OnPauseChanged;
                }
                else
                {
                    _lifecycleHook = AutosaveLifecycleHook.EnsureExists();
                    _lifecycleHook.PauseChanged += OnPauseChanged;
                }
            }

            if (ShouldSaveOnQuit())
            {
                if (_applicationLifecycleService != null)
                {
                    _applicationLifecycleService.QuitRequested += OnApplicationQuitting;
                }
                else
                {
                    Application.quitting += OnApplicationQuitting;
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _signalSubscriptions.Count; i++)
            {
                _signalSubscriptions[i].Dispose();
            }
            _signalSubscriptions.Clear();

            if (ShouldSaveOnPause())
            {
                if (_applicationLifecycleService != null)
                {
                    _applicationLifecycleService.PauseChanged -= OnPauseChanged;
                }
                else if (_lifecycleHook != null)
                {
                    _lifecycleHook.PauseChanged -= OnPauseChanged;
                }
            }

            if (ShouldSaveOnQuit())
            {
                if (_applicationLifecycleService != null)
                {
                    _applicationLifecycleService.QuitRequested -= OnApplicationQuitting;
                }
                else
                {
                    Application.quitting -= OnApplicationQuitting;
                }
            }
        }

        public void Tick()
        {
            if (!IsEnabled() || _isSaving)
            {
                return;
            }

            if (_timeProvider.Time < _nextAutosaveAt)
            {
                return;
            }

            _nextAutosaveAt = _timeProvider.Time + GetIntervalSeconds();
            FlushDirtyAsync("interval").Forget();
        }

        private void OnSettingsChanged(SettingsChangedSignal signal)
        {
            _dirtySettings = true;
        }

        private void OnConsentChanged(ConsentChangedSignal signal)
        {
            _dirtyConsent = true;
        }

        private void OnPauseChanged(bool isPaused)
        {
            if (!isPaused)
            {
                return;
            }

            FlushDirtyAsync("pause").Forget();
        }

        private void OnApplicationQuitting()
        {
            FlushDirtyAsync("quit").Forget();
        }

        private async UniTaskVoid FlushDirtyAsync(string reason)
        {
            if (_isSaving)
            {
                return;
            }

            bool shouldSaveSettings = _dirtySettings && _settingsService != null && _settingsService.IsLoaded;
            bool shouldSaveConsent = _dirtyConsent && _consentService != null && _consentService.IsLoaded;
            if (!shouldSaveSettings && !shouldSaveConsent)
            {
                return;
            }

            _isSaving = true;
            _signalBus?.Publish(new AutosaveTriggeredSignal(reason, shouldSaveSettings, shouldSaveConsent));
            int savedTargets = 0;

            try
            {
                if (shouldSaveSettings)
                {
                    await _settingsService.SaveAsync();
                    _dirtySettings = false;
                    savedTargets++;
                }

                if (shouldSaveConsent)
                {
                    await _consentService.SaveAsync();
                    _dirtyConsent = false;
                    savedTargets++;
                }

                _signalBus?.Publish(new AutosaveCompletedSignal(reason, savedTargets));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                _signalBus?.Publish(new AutosaveFailedSignal(reason, exception.Message));
            }
            finally
            {
                _isSaving = false;
            }
        }

        private bool IsEnabled()
        {
            return _config == null || _config.Enabled;
        }

        private float GetIntervalSeconds()
        {
            return _config != null ? _config.IntervalSeconds : 20f;
        }

        private bool ShouldSaveOnPause()
        {
            return _config == null || _config.SaveOnApplicationPause;
        }

        private bool ShouldSaveOnQuit()
        {
            return _config == null || _config.SaveOnApplicationQuit;
        }

        private sealed class AutosaveLifecycleHook : MonoBehaviour
        {
            private static AutosaveLifecycleHook _instance;

            public event System.Action<bool> PauseChanged;

            public static AutosaveLifecycleHook EnsureExists()
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject host = new GameObject("[Foundation] AutosaveLifecycleHook");
                // DontDestroyOnLoad is play-mode only; guard it so the container build (which runs
                // entry-point Initialize) doesn't throw under EditMode tests.
                if (Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(host);
                }
                _instance = host.AddComponent<AutosaveLifecycleHook>();
                return _instance;
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                PauseChanged?.Invoke(pauseStatus);
            }
        }
    }
}
