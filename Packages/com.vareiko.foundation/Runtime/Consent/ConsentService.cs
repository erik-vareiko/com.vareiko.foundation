using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Signals;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Consent
{
    public sealed class ConsentService : IConsentService, VContainer.Unity.IInitializable
    {
        private const string Slot = "global";
        private const string Key = "consent";

        private readonly ISaveService _saveService;
        private readonly IFoundationSignalBus _signalBus;

        private ConsentState _state = new ConsentState();
        private bool _isLoaded;

        [Inject]
        public ConsentService(ISaveService saveService, IFoundationSignalBus signalBus)
        {
            _saveService = saveService;
            _signalBus = signalBus;
        }

        public bool IsLoaded => _isLoaded;
        public bool IsConsentCollected => _state.IsCollected;

        public void Initialize()
        {
            LoadSafeAsync().Forget();
        }

        public bool HasConsent(ConsentScope scope)
        {
            return _state.Get(scope);
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            ConsentState loaded = await _saveService.LoadAsync(Slot, Key, new ConsentState(), cancellationToken);
            _state = loaded ?? new ConsentState();
            _isLoaded = true;
            _signalBus.Publish(new ConsentLoadedSignal(_state.IsCollected));
        }

        public UniTask SaveAsync(CancellationToken cancellationToken = default)
        {
            return _saveService.SaveAsync(Slot, Key, _state, cancellationToken);
        }

        public void SetConsent(ConsentScope scope, bool granted, bool saveImmediately = false)
        {
            _state.Set(scope, granted);
            FireChanged(scope);
            if (saveImmediately)
            {
                SaveAsync().Forget();
            }
        }

        public void SetConsentCollected(bool isCollected, bool saveImmediately = false)
        {
            _state.IsCollected = isCollected;
            FireChanged(ConsentScope.Analytics);
            FireChanged(ConsentScope.Personalization);
            FireChanged(ConsentScope.Advertising);
            FireChanged(ConsentScope.PushNotifications);
            if (saveImmediately)
            {
                SaveAsync().Forget();
            }
        }

        private void FireChanged(ConsentScope scope)
        {
            _signalBus.Publish(new ConsentChangedSignal(scope, _state.Get(scope), _state.IsCollected));
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
                _state = new ConsentState();
                _isLoaded = true;
                _signalBus.Publish(new ConsentLoadedSignal(false));
            }
        }
    }
}
