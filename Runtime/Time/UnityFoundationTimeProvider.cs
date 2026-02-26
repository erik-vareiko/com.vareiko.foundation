using UnityEngine;

namespace Vareiko.Foundation.Time
{
    public sealed class UnityFoundationTimeProvider : IFoundationTimeProvider
    {
        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledTime => UnityEngine.Time.unscaledTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
    }
}
