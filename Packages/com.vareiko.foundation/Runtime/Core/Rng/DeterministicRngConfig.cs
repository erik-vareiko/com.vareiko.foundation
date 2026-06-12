using UnityEngine;

namespace Vareiko.Foundation.Rng
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Deterministic RNG Config")]
    public sealed class DeterministicRngConfig : ScriptableObject
    {
        [SerializeField] private int _defaultRootSeed = 1;
        [SerializeField] private bool _allowReseedAtRuntime;
        [SerializeField] private bool _enableRngDiagnostics;

        public int DefaultRootSeed => _defaultRootSeed;
        public bool AllowReseedAtRuntime => _allowReseedAtRuntime;
        public bool EnableRngDiagnostics => _enableRngDiagnostics;
    }
}
