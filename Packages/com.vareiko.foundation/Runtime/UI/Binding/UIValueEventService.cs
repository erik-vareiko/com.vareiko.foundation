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
        private readonly Dictionary<string, ValueStream<int>> _intStreams = new Dictionary<string, ValueStream<int>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ValueStream<float>> _floatStreams = new Dictionary<string, ValueStream<float>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ValueStream<bool>> _boolStreams = new Dictionary<string, ValueStream<bool>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ValueStream<string>> _stringStreams = new Dictionary<string, ValueStream<string>>(StringComparer.Ordinal);

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
            GetOrCreateStream(_intStreams, normalized).SetValue(value);
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
            GetOrCreateStream(_floatStreams, normalized).SetValue(value);
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
            GetOrCreateStream(_boolStreams, normalized).SetValue(value);
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
            GetOrCreateStream(_stringStreams, normalized).SetValue(safeValue);
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

        public IReadOnlyValueStream<int> ObserveInt(string key)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return NullValueStream<int>.Instance;
            }

            ValueStream<int> stream = GetOrCreateStream(_intStreams, normalized);
            if (_intValues.TryGetValue(normalized, out int value))
            {
                if (!stream.HasValue || stream.Value != value)
                {
                    stream.SetValue(value);
                }
            }

            return stream;
        }

        public IReadOnlyValueStream<float> ObserveFloat(string key)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return NullValueStream<float>.Instance;
            }

            ValueStream<float> stream = GetOrCreateStream(_floatStreams, normalized);
            if (_floatValues.TryGetValue(normalized, out float value))
            {
                if (!stream.HasValue || !Mathf.Approximately(stream.Value, value))
                {
                    stream.SetValue(value);
                }
            }

            return stream;
        }

        public IReadOnlyValueStream<bool> ObserveBool(string key)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return NullValueStream<bool>.Instance;
            }

            ValueStream<bool> stream = GetOrCreateStream(_boolStreams, normalized);
            if (_boolValues.TryGetValue(normalized, out bool value))
            {
                if (!stream.HasValue || stream.Value != value)
                {
                    stream.SetValue(value);
                }
            }

            return stream;
        }

        public IReadOnlyValueStream<string> ObserveString(string key)
        {
            if (!TryNormalizeKey(key, out string normalized))
            {
                return NullValueStream<string>.Instance;
            }

            ValueStream<string> stream = GetOrCreateStream(_stringStreams, normalized);
            if (_stringValues.TryGetValue(normalized, out string value))
            {
                if (!stream.HasValue || !string.Equals(stream.Value, value, StringComparison.Ordinal))
                {
                    stream.SetValue(value);
                }
            }

            return stream;
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
            ClearStream(_intStreams, normalized);
            ClearStream(_floatStreams, normalized);
            ClearStream(_boolStreams, normalized);
            ClearStream(_stringStreams, normalized);
        }

        public void ClearAll()
        {
            _intValues.Clear();
            _floatValues.Clear();
            _boolValues.Clear();
            _stringValues.Clear();
            ClearAllStreams(_intStreams);
            ClearAllStreams(_floatStreams);
            ClearAllStreams(_boolStreams);
            ClearAllStreams(_stringStreams);
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

        private static ValueStream<T> GetOrCreateStream<T>(Dictionary<string, ValueStream<T>> streams, string key)
        {
            if (!streams.TryGetValue(key, out ValueStream<T> stream))
            {
                stream = new ValueStream<T>();
                streams[key] = stream;
            }

            return stream;
        }

        private static void ClearStream<T>(Dictionary<string, ValueStream<T>> streams, string key)
        {
            if (streams.TryGetValue(key, out ValueStream<T> stream))
            {
                stream.Clear();
            }
        }

        private static void ClearAllStreams<T>(Dictionary<string, ValueStream<T>> streams)
        {
            foreach (KeyValuePair<string, ValueStream<T>> pair in streams)
            {
                pair.Value.Clear();
            }
        }
    }
}
