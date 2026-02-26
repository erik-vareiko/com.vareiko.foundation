using System;
using UnityEngine;

namespace Vareiko.Foundation.Save
{
    public sealed class JsonUnitySaveSerializer : ISaveSerializer
    {
        public string Serialize<T>(T model)
        {
            SaveEnvelope<T> envelope = new SaveEnvelope<T>
            {
                Value = model
            };

            return JsonUtility.ToJson(envelope);
        }

        public bool TryDeserialize<T>(string raw, out T model)
        {
            model = default;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            try
            {
                SaveEnvelope<T> envelope = JsonUtility.FromJson<SaveEnvelope<T>>(raw);
                model = envelope != null ? envelope.Value : default;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Serializable]
        private sealed class SaveEnvelope<TValue>
        {
            public TValue Value;
        }
    }
}
