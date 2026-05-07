using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIFloatTextBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private Text _target;
        [SerializeField] private string _format = "{0:0.##}";

        private SignalBus _signalBus;
        private IUIValueEventService _valueService;
        private IDisposable _subscription;
        private bool _usesSignalBusSubscription;

        [Inject]
        public void Construct([InjectOptional] SignalBus signalBus = null, [InjectOptional] IUIValueEventService valueService = null)
        {
            _signalBus = signalBus;
            _valueService = valueService;
        }

        private void OnEnable()
        {
            ClearSubscription();
            if (!TryNormalizeKey(out string key))
            {
                return;
            }

            if (_valueService != null)
            {
                IReadOnlyValueStream<float> stream = _valueService.ObserveFloat(key);
                _subscription = stream.Subscribe(Apply, true);
                _usesSignalBusSubscription = false;
            }
            else if (_signalBus != null)
            {
                _signalBus.Subscribe<UIFloatValueChangedSignal>(OnValueChanged);
                _usesSignalBusSubscription = true;
            }
        }

        private void OnDisable()
        {
            ClearSubscription();
        }

        private void ClearSubscription()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (_signalBus != null && _usesSignalBusSubscription)
            {
                _signalBus.Unsubscribe<UIFloatValueChangedSignal>(OnValueChanged);
            }

            _usesSignalBusSubscription = false;
        }

        private void OnValueChanged(UIFloatValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            Apply(signal.Value);
        }

        private void Apply(float value)
        {
            if (_target == null)
            {
                return;
            }

            _target.text = string.Format(_format, value);
        }

        private bool IsTargetKey(string key)
        {
            return TryNormalizeKey(out string normalized) && string.Equals(normalized, key, System.StringComparison.Ordinal);
        }

        private bool TryNormalizeKey(out string key)
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                key = string.Empty;
                return false;
            }

            key = _key.Trim();
            return key.Length > 0;
        }
    }
}
