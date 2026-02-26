using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIIntTextBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private Text _target;
        [SerializeField] private string _format = "{0}";

        private SignalBus _signalBus;
        private IUIValueEventService _valueService;
        private IDisposable _subscription;

        [Inject]
        public void Construct([InjectOptional] SignalBus signalBus = null, [InjectOptional] IUIValueEventService valueService = null)
        {
            _signalBus = signalBus;
            _valueService = valueService;
        }

        private void OnEnable()
        {
            if (_valueService != null && TryNormalizeKey(out string key))
            {
                IReadOnlyValueStream<int> stream = _valueService.ObserveInt(key);
                _subscription = stream.Subscribe(Apply, true);
            }
            else if (_signalBus != null)
            {
                _signalBus.Subscribe<UIIntValueChangedSignal>(OnValueChanged);
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<UIIntValueChangedSignal>(OnValueChanged);
            }
        }

        private void OnValueChanged(UIIntValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            Apply(signal.Value);
        }

        private void Apply(int value)
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
