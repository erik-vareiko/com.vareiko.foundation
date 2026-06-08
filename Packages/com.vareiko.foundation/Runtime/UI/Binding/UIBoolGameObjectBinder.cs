using System;
using UnityEngine;
using Vareiko.Foundation.Signals;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIBoolGameObjectBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private GameObject _target;
        [SerializeField] private bool _invert;

        private IFoundationSignalBus _signalBus;
        private IUIValueEventService _valueService;
        private IDisposable _subscription;

        [Inject]
        public void Construct([InjectOptional] IFoundationSignalBus signalBus = null, [InjectOptional] IUIValueEventService valueService = null)
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
                IReadOnlyValueStream<bool> stream = _valueService.ObserveBool(key);
                _subscription = stream.Subscribe(Apply, true);
            }
            else if (_signalBus != null)
            {
                _subscription = _signalBus.Subscribe<UIBoolValueChangedSignal>(OnValueChanged);
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

        private void OnValueChanged(UIBoolValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            Apply(signal.Value);
        }

        private void Apply(bool value)
        {
            GameObject target = _target != null ? _target : gameObject;
            bool visible = _invert ? !value : value;
            target.SetActive(visible);
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
