using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Time;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public sealed class AutosaveService : IInitializable, ITickable, System.IDisposable
    {
        private readonly AutosaveConfig _config;
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly ISettingsService _settingsService;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;

        private bool _dirtySettings;
        private bool _dirtyConsent;
        private bool _isSaving;
        private float _nextAutosaveAt;
        private AutosaveLifecycleHook _lifecycleHook;

        [Inject]
        public AutosaveService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] AutosaveConfig config = null,
            [InjectOptional] ISettingsService settingsService = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _settingsService = settingsService;
            _consentService = consentService;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _nextAutosaveAt = _timeProvider.Time + GetIntervalSeconds();

            if (_signalBus != null)
            {
                _signalBus.Subscribe<SettingsChangedSignal>(OnSettingsChanged);
                _signalBus.Subscribe<ConsentChangedSignal>(OnConsentChanged);
            }

            if (ShouldSaveOnPause())
            {
                _lifecycleHook = AutosaveLifecycleHook.EnsureExists();
                _lifecycleHook.PauseChanged += OnPauseChanged;
            }

            if (ShouldSaveOnQuit())
            {
                Application.quitting += OnApplicationQuitting;
            }
        }

        public void Dispose()
        {
            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<SettingsChangedSignal>(OnSettingsChanged);
                _signalBus.Unsubscribe<ConsentChangedSignal>(OnConsentChanged);
            }

            if (ShouldSaveOnPause())
            {
                if (_lifecycleHook != null)
                {
                    _lifecycleHook.PauseChanged -= OnPauseChanged;
                }
            }

            if (ShouldSaveOnQuit())
            {
                Application.quitting -= OnApplicationQuitting;
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
            _signalBus?.Fire(new AutosaveTriggeredSignal(reason, shouldSaveSettings, shouldSaveConsent));
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

                _signalBus?.Fire(new AutosaveCompletedSignal(reason, savedTargets));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                _signalBus?.Fire(new AutosaveFailedSignal(reason, exception.Message));
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
                Object.DontDestroyOnLoad(host);
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
