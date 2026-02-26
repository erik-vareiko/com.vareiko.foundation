using System;
using System.Globalization;
using UnityEngine;

namespace Vareiko.Foundation.UI
{
    public static class UIWindowResultPayload
    {
        public static string Serialize<T>(T value)
        {
            if (typeof(T) == typeof(string))
            {
                return value as string ?? string.Empty;
            }

            PayloadEnvelope<T> envelope = new PayloadEnvelope<T>
            {
                Value = value
            };

            return JsonUtility.ToJson(envelope);
        }

        public static bool TryDeserialize<T>(string payload, out T value)
        {
            if (typeof(T) == typeof(string))
            {
                value = (T)(object)(payload ?? string.Empty);
                return true;
            }

            if (TryParsePrimitive(payload, out value))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                value = default;
                return false;
            }

            try
            {
                PayloadEnvelope<T> envelope = JsonUtility.FromJson<PayloadEnvelope<T>>(payload);
                if (envelope == null)
                {
                    value = default;
                    return false;
                }

                value = envelope.Value;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public static bool TryGetPayload<T>(this UIWindowResult result, out T value)
        {
            return TryDeserialize(result.Payload, out value);
        }

        public static T GetPayloadOrDefault<T>(this UIWindowResult result, T fallback = default)
        {
            if (TryDeserialize(result.Payload, out T value))
            {
                return value;
            }

            return fallback;
        }

        public static UIWindowResult WithPayload<T>(this UIWindowResult result, T payload)
        {
            return new UIWindowResult(result.WindowId, result.Status, Serialize(payload));
        }

        private static bool TryParsePrimitive<T>(string payload, out T value)
        {
            Type type = typeof(T);
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

            if (string.IsNullOrWhiteSpace(payload))
            {
                value = default;
                return false;
            }

            string raw = payload.Trim();
            object parsed;

            if (effectiveType == typeof(bool))
            {
                if (!bool.TryParse(raw, out bool boolValue))
                {
                    value = default;
                    return false;
                }

                parsed = boolValue;
            }
            else if (effectiveType == typeof(int))
            {
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    value = default;
                    return false;
                }

                parsed = intValue;
            }
            else if (effectiveType == typeof(long))
            {
                if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                {
                    value = default;
                    return false;
                }

                parsed = longValue;
            }
            else if (effectiveType == typeof(float))
            {
                if (!float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = default;
                    return false;
                }

                parsed = floatValue;
            }
            else if (effectiveType == typeof(double))
            {
                if (!double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
                {
                    value = default;
                    return false;
                }

                parsed = doubleValue;
            }
            else if (effectiveType.IsEnum)
            {
                if (!Enum.TryParse(effectiveType, raw, true, out parsed))
                {
                    value = default;
                    return false;
                }
            }
            else
            {
                value = default;
                return false;
            }

            value = (T)parsed;
            return true;
        }

        [Serializable]
        private sealed class PayloadEnvelope<T>
        {
            public T Value;
        }
    }
}
