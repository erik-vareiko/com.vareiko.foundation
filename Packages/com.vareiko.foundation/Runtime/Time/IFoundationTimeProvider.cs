namespace Vareiko.Foundation.Time
{
    public interface IFoundationTimeProvider
    {
        float Time { get; }
        float DeltaTime { get; }
        float UnscaledTime { get; }
        float UnscaledDeltaTime { get; }
    }
}
