using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIStringTextBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private Text _target;

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
                _signalBus.Subscribe<UIStringValueChangedSignal>(OnValueChanged);
            }

            RefreshFromStore();
        }

        private void OnDisable()
        {
            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<UIStringValueChangedSignal>(OnValueChanged);
            }
        }

        private void OnValueChanged(UIStringValueChangedSignal signal)
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

            if (_valueService.TryGetString(key, out string value))
            {
                Apply(value);
            }
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
