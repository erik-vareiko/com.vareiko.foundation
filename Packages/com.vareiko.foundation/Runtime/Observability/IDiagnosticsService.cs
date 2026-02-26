namespace Vareiko.Foundation.Observability
{
    public interface IDiagnosticsService
    {
        DiagnosticsSnapshot Snapshot { get; }
    }
}
