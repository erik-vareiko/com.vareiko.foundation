using System;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.Signals;
using VContainer;

namespace Vareiko.Foundation.UI
{
    public sealed class UIStringTextBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private Text _target;

        private IFoundationSignalBus _signalBus;
        private IUIValueEventService _valueService;
        private IDisposable _subscription;

        [Inject]
        public void Construct(IFoundationSignalBus signalBus, IUIValueEventService valueService)
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
                IReadOnlyValueStream<string> stream = _valueService.ObserveString(key);
                _subscription = stream.Subscribe(Apply, true);
            }
            else if (_signalBus != null)
            {
                _subscription = _signalBus.Subscribe<UIStringValueChangedSignal>(OnValueChanged);
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
        }

        private void OnValueChanged(UIStringValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            Apply(signal.Value);
        }

        private void Apply(string value)
        {
            if (_target == null)
            {
                return;
            }

            _target.text = value ?? string.Empty;
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
