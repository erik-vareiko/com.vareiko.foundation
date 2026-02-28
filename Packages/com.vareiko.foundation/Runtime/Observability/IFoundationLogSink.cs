namespace Vareiko.Foundation.Observability
{
    public interface IFoundationLogSink
    {
        void Write(FoundationLogEntry entry);
    }
}
