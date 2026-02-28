using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Observability
{
    public readonly struct DiagnosticsSnapshotExportResult
    {
        public readonly bool Success;
        public readonly string FilePath;
        public readonly string Error;

        public DiagnosticsSnapshotExportResult(bool success, string filePath, string error)
        {
            Success = success;
            FilePath = filePath ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public static DiagnosticsSnapshotExportResult Succeed(string filePath)
        {
            return new DiagnosticsSnapshotExportResult(true, filePath, string.Empty);
        }

        public static DiagnosticsSnapshotExportResult Fail(string error)
        {
            return new DiagnosticsSnapshotExportResult(false, string.Empty, error);
        }
    }

    public interface IDiagnosticsSnapshotExportService
    {
        string ExportDirectory { get; }
        UniTask<DiagnosticsSnapshotExportResult> ExportAsync(string label = null, CancellationToken cancellationToken = default);
    }
}
