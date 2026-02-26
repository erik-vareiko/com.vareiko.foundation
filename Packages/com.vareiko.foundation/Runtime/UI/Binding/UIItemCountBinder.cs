using System;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIItemCountBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private UIItemCollectionBinder _collection;
        [SerializeField] private int _minCount;
        [SerializeField] private int _maxCount = -1;
        [SerializeField] private bool _absoluteValue = true;

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
            if (_collection == null)
            {
                _collection = GetComponent<UIItemCollectionBinder>();
            }
        }

        private void OnEnable()
        {
            if (_collection == null || !TryNormalizeKey(out string key))
            {
                return;
            }

            if (_valueService != null)
            {
                _subscription = _valueService.ObserveInt(key).Subscribe(ApplyCount, true);
                return;
            }

            if (_signalBus != null)
            {
                _signalBus.Subscribe<UIIntValueChangedSignal>(OnIntChanged);
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<UIIntValueChangedSignal>(OnIntChanged);
            }
        }

        private void OnIntChanged(UIIntValueChangedSignal signal)
        {
            if (!IsTargetKey(signal.Key))
            {
                return;
            }

            ApplyCount(signal.Value);
        }

        private void ApplyCount(int rawValue)
        {
            int value = _absoluteValue ? Math.Abs(rawValue) : rawValue;
            int clamped = Math.Max(_minCount, value);
            if (_maxCount >= 0)
            {
                clamped = Math.Min(_maxCount, clamped);
            }

            _collection.SetCount(clamped);
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
