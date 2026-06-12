using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Observability
{
    public interface IDiagnosticsSnapshotExportService
    {
        string ExportDirectory { get; }

        /// <summary>Exports the current diagnostics snapshot; the value is the written file path.</summary>
        UniTask<Result<string>> ExportAsync(string label = null, CancellationToken cancellationToken = default);
    }
}
