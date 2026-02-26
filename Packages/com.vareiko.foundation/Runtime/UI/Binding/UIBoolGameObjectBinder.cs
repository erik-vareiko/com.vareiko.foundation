using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIBoolGameObjectBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private GameObject _target;
        [SerializeField] private bool _invert;

        private SignalBus _signalBus;
        private IUIValueEventService _valueService;

        [Inject]
        public void Construct([InjectOptional] SignalBus signalBus = null, [InjectOptional] IUIValueEventService valueService = null)
        {
            _signalBus = signalBus;
            _valueService = valueService;
        }

        private void OnEnable()
        {
            if (_signalBus != null)
            {
                _signalBus.Subscribe<UIBoolValueChangedSignal>(OnValueChanged);
            }

            RefreshFromStore();
        }

        private void OnDisable()
        {
            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<UIBoolValueChangedSignal>(OnValueChanged);
            }
        }

        private void OnValueChanged(UIBoolValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            Apply(signal.Value);
        }

        private void RefreshFromStore()
        {
            if (_valueService == null || !TryNormalizeKey(out string key))
            {
                return;
            }

            if (_valueService.TryGetBool(key, out bool value))
            {
                Apply(value);
            }
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
