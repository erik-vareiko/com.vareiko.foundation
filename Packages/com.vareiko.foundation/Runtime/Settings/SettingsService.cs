using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Signals;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Settings
{
    public sealed class SettingsService : ISettingsService, VContainer.Unity.IInitializable
    {
        private const string Slot = "global";
        private const string Key = "settings";

        private readonly ISaveService _saveService;
        private readonly IFoundationSignalBus _signalBus;

        private GameSettings _current = new GameSettings();
        private bool _isLoaded;

        [Inject]
        public SettingsService(ISaveService saveService, [InjectOptional] IFoundationSignalBus signalBus = null)
        {
            _saveService = saveService;
            _signalBus = signalBus;
        }

        public bool IsLoaded => _isLoaded;
        public GameSettings Current => _current;

        public void Initialize()
        {
            LoadSafeAsync().Forget();
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            GameSettings loaded = await _saveService.LoadAsync(Slot, Key, new GameSettings(), cancellationToken);
            _current = loaded ?? new GameSettings();
            _isLoaded = true;
            _signalBus?.Publish(new SettingsLoadedSignal(_current));
            _signalBus?.Publish(new SettingsChangedSignal(_current));
        }

        public UniTask SaveAsync(CancellationToken cancellationToken = default)
        {
            return _saveService.SaveAsync(Slot, Key, _current, cancellationToken);
        }

        public void Apply(GameSettings settings, bool saveImmediately = false)
        {
            if (settings == null)
            {
                return;
            }

            _current = settings;
            _signalBus?.Publish(new SettingsChangedSignal(_current));
            if (saveImmediately)
            {
                SaveAsync().Forget();
            }
        }

        private async UniTaskVoid LoadSafeAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _current = new GameSettings();
                _isLoaded = true;
                _signalBus?.Publish(new SettingsLoadedSignal(_current));
                _signalBus?.Publish(new SettingsChangedSignal(_current));
            }
        }
    }
}
