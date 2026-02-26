namespace Vareiko.Foundation.Observability
{
    public interface IFoundationLogger
    {
        FoundationLogLevel MinimumLevel { get; }
        void Log(FoundationLogLevel level, string message, string category = null);
        void Debug(string message, string category = null);
        void Info(string message, string category = null);
        void Warn(string message, string category = null);
        void Error(string message, string category = null);
    }
}
