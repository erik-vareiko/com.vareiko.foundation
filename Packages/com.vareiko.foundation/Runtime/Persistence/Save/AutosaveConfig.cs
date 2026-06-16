using UnityEngine;

namespace Vareiko.Foundation.Save
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Autosave Config")]
    public sealed class AutosaveConfig : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _intervalSeconds = 20f;
        [SerializeField] private bool _saveOnApplicationPause = true;
        [SerializeField] private bool _saveOnApplicationQuit = true;

        public bool Enabled => _enabled;
        public float IntervalSeconds => _intervalSeconds <= 0.25f ? 0.25f : _intervalSeconds;
        public bool SaveOnApplicationPause => _saveOnApplicationPause;
        public bool SaveOnApplicationQuit => _saveOnApplicationQuit;
    }
}
