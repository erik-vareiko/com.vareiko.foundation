using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Input
{
    public interface IInputRebindStorage
    {
        string Load();
        void Save(string overridesJson);
        void Clear();
    }

    public sealed class PlayerPrefsInputRebindStorage : IInputRebindStorage
    {
        private const string DefaultStorageKey = "vareiko.foundation.input.rebind_overrides";
        private readonly string _storageKey;

        [Inject]
        public PlayerPrefsInputRebindStorage([InjectOptional(Id = "InputRebindStorageKey")] string storageKey = null)
        {
            _storageKey = string.IsNullOrWhiteSpace(storageKey) ? DefaultStorageKey : storageKey.Trim();
        }

        public string Load()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
            {
                return string.Empty;
            }

            return PlayerPrefs.GetString(_storageKey, string.Empty);
        }

        public void Save(string overridesJson)
        {
            if (string.IsNullOrWhiteSpace(overridesJson))
            {
                Clear();
                return;
            }

            PlayerPrefs.SetString(_storageKey, overridesJson);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(_storageKey);
            PlayerPrefs.Save();
        }
    }
}
