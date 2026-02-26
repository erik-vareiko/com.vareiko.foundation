using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Config
{
    public sealed class ConfigService : IConfigService
    {
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, ScriptableObject> _map = new Dictionary<string, ScriptableObject>(StringComparer.Ordinal);

        [Inject]
        public ConfigService([InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        public void Register<T>(T config, string id = "default") where T : ScriptableObject
        {
            if (config == null)
            {
                return;
            }

            string key = MakeKey(typeof(T), id);
            _map[key] = config;
            _signalBus?.Fire(new ConfigRegisteredSignal(id, typeof(T).Name));
        }

        public bool TryGet<T>(out T config, string id = "default") where T : ScriptableObject
        {
            string key = MakeKey(typeof(T), id);
            ScriptableObject value;
            if (_map.TryGetValue(key, out value))
            {
                config = value as T;
                return config != null;
            }

            config = null;
            _signalBus?.Fire(new ConfigMissingSignal(id, typeof(T).Name));
            return false;
        }

        public T GetRequired<T>(string id = "default") where T : ScriptableObject
        {
            T config;
            if (TryGet(out config, id))
            {
                return config;
            }

            throw new InvalidOperationException($"Config not found. Type={typeof(T).Name}, Id={id}");
        }

        public void Unregister<T>(string id = "default") where T : ScriptableObject
        {
            string key = MakeKey(typeof(T), id);
            _map.Remove(key);
        }

        private static string MakeKey(Type type, string id)
        {
            return type.FullName + "|" + (string.IsNullOrWhiteSpace(id) ? "default" : id);
        }
    }
}
