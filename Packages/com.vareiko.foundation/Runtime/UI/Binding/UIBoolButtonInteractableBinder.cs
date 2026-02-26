using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIBoolButtonInteractableBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private UIButtonView _buttonView;
        [SerializeField] private Button _button;
        [SerializeField] private bool _invert;

        private SignalBus _signalBus;
        private IUIValueEventService _valueService;
        private IDisposable _subscription;

        [Inject]
        public void Construct([InjectOptional] SignalBus signalBus = null, [InjectOptional] IUIValueEventService valueService = null)
        {
            _signalBus = signalBus;
            _valueService = valueService;
        }

        private void Awake()
        {
            if (_buttonView == null)
            {
                _buttonView = GetComponent<UIButtonView>();
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (_valueService != null && TryNormalizeKey(out string key))
            {
                _subscription = _valueService.ObserveBool(key).Subscribe(Apply, true);
                return;
            }

            if (_signalBus != null)
            {
                _signalBus.Subscribe<UIBoolValueChangedSignal>(OnValueChanged);
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;

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

        private void Apply(bool value)
        {
            bool interactable = _invert ? !value : value;
            _buttonView?.SetInteractable(interactable);

            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        private bool IsTargetKey(string key)
        {
            return TryNormalizeKey(out string normalized) && string.Equals(normalized, key, StringComparison.Ordinal);
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
