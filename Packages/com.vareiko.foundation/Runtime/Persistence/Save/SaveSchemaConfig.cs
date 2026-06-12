using UnityEngine;

namespace Vareiko.Foundation.Save
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Save Schema Config")]
    public sealed class SaveSchemaConfig : ScriptableObject
    {
        [SerializeField] private int _currentVersion = 1;

        public int CurrentVersion => _currentVersion < 1 ? 1 : _currentVersion;
    }
}
