using Vareiko.Foundation.Time;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public sealed class FakeTimeProvider : IFoundationTimeProvider
    {
        public float Time { get; set; }
        public float DeltaTime { get; set; }
        public float UnscaledTime { get; set; }
        public float UnscaledDeltaTime { get; set; }
    }
}
