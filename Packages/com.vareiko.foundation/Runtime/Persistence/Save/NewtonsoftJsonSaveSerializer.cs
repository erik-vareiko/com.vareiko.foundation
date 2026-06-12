using System;
using Newtonsoft.Json;

namespace Vareiko.Foundation.Save
{
    /// <summary>
    /// Default save serializer (3.0+). Unlike <see cref="JsonUnitySaveSerializer"/> it handles
    /// dictionaries, nullables and polymorphic payloads. Writes the same
    /// <c>{"Value": ...}</c> envelope as the JsonUtility serializer, so saves written before
    /// the switch keep deserializing.
    /// </summary>
    public sealed class NewtonsoftJsonSaveSerializer : ISaveSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            // Save payloads must survive renames/refactors of host types; type names in JSON
            // would break on the first refactor, so they are never emitted.
            TypeNameHandling = TypeNameHandling.None,
        };

        public string Serialize<T>(T model)
        {
            SaveEnvelope<T> envelope = new SaveEnvelope<T>
            {
                Value = model
            };

            return JsonConvert.SerializeObject(envelope, Settings);
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
                SaveEnvelope<T> envelope = JsonConvert.DeserializeObject<SaveEnvelope<T>>(raw, Settings);
                model = envelope != null ? envelope.Value : default;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private sealed class SaveEnvelope<TValue>
        {
            public TValue Value;
        }
    }
}
