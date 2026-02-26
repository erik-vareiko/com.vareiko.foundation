using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIValueEventService : IUIValueEventService
    {
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, int> _intValues = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _floatValues = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _boolValues = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _stringValues = new Dictionary<string, string>(StringComparer.Ordinal);

        [Inject]
        public UIValueEventService([InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        public void SetInt(string key, int value)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return;
            }

            if (_intValues.TryGetValue(normalized, out int current) && current == value)
            {
                return;
            }

            _intValues[normalized] = value;
            _signalBus?.Fire(new UIIntValueChangedSignal(normalized, value));
        }

        public void SetFloat(string key, float value)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return;
            }

            if (_floatValues.TryGetValue(normalized, out float current) && Mathf.Approximately(current, value))
            {
                return;
            }

            _floatValues[normalized] = value;
            _signalBus?.Fire(new UIFloatValueChangedSignal(normalized, value));
        }

        public void SetBool(string key, bool value)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return;
            }

            if (_boolValues.TryGetValue(normalized, out bool current) && current == value)
            {
                return;
            }

            _boolValues[normalized] = value;
            _signalBus?.Fire(new UIBoolValueChangedSignal(normalized, value));
        }

        public void SetString(string key, string value)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return;
            }

            string safeValue = value ?? string.Empty;
            if (_stringValues.TryGetValue(normalized, out string current) && string.Equals(current, safeValue, StringComparison.Ordinal))
            {
                return;
            }

            _stringValues[normalized] = safeValue;
            _signalBus?.Fire(new UIStringValueChangedSignal(normalized, safeValue));
        }

        public bool TryGetInt(string key, out int value)
        {
            value = default;
            return TryNormalizeKey(key, out string normalized) && _intValues.TryGetValue(normalized, out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            return TryNormalizeKey(key, out string normalized) && _floatValues.TryGetValue(normalized, out value);
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = default;
            return TryNormalizeKey(key, out string normalized) && _boolValues.TryGetValue(normalized, out value);
        }

        public bool TryGetString(string key, out string value)
        {
            value = string.Empty;
            return TryNormalizeKey(key, out string normalized) && _stringValues.TryGetValue(normalized, out value);
        }

        public void Clear(string key)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return;
            }

            _intValues.Remove(normalized);
            _floatValues.Remove(normalized);
            _boolValues.Remove(normalized);
            _stringValues.Remove(normalized);
        }

        public void ClearAll()
        {
            _intValues.Clear();
            _floatValues.Clear();
            _boolValues.Clear();
            _stringValues.Clear();
        }

        private static bool TryNormalizeKey(string key, out string normalized)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                normalized = string.Empty;
                return false;
            }

            normalized = key.Trim();
            return normalized.Length > 0;
        }
    }
}
