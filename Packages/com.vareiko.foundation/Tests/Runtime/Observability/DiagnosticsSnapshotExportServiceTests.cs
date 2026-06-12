using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class DiagnosticsSnapshotExportServiceTests
    {
        [Test]
        public async Task ExportAsync_WritesSnapshotJson_AndFiresSignal()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "vf_diagnostics_" + Guid.NewGuid().ToString("N"));
            try
            {
                DiagnosticsSnapshot snapshot = new DiagnosticsSnapshot
                {
                    IsBootCompleted = true,
                    IsOnline = true,
                    RemoteConfigValues = 4,
                    LastUpdatedAt = 12.5f
                };

                FakeSignalBus signalBus = new FakeSignalBus();
                bool exportedSignalReceived = false;
                string exportedPath = string.Empty;
                signalBus.Subscribe<DiagnosticsSnapshotExportedSignal>(signal =>
                {
                    exportedSignalReceived = true;
                    exportedPath = signal.FilePath;
                });

                DiagnosticsSnapshotExportService service = new DiagnosticsSnapshotExportService(
                    new FakeDiagnosticsService(snapshot),
                    tempDirectory,
                    signalBus,
                    null);

                Vareiko.Foundation.Result<string> result = await service.ExportAsync("qa run#1");

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.Not.Empty);
                Assert.That(File.Exists(result.Value), Is.True);
                Assert.That(exportedSignalReceived, Is.True);
                Assert.That(exportedPath, Is.EqualTo(result.Value));
                Assert.That(Path.GetFileName(result.Value), Does.Contain("qa_run_1"));

                string json = File.ReadAllText(result.Value);
                Assert.That(json, Does.Contain("\"IsBootCompleted\": true"));
                Assert.That(json, Does.Contain("\"RemoteConfigValues\": 4"));
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        [Test]
        public async Task ExportAsync_WhenSnapshotMissing_ReturnsFailureAndFiresFailedSignal()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            bool failedSignalReceived = false;
            string failedError = string.Empty;
            signalBus.Subscribe<DiagnosticsSnapshotExportFailedSignal>(signal =>
            {
                failedSignalReceived = true;
                failedError = signal.Error;
            });

            DiagnosticsSnapshotExportService service = new DiagnosticsSnapshotExportService(
                new FakeDiagnosticsService(null),
                Path.GetTempPath(),
                signalBus,
                null);

            Vareiko.Foundation.Result<string> result = await service.ExportAsync();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Does.Contain("not available"));
            Assert.That(failedSignalReceived, Is.True);
            Assert.That(failedError, Does.Contain("not available"));
        }

        private sealed class FakeDiagnosticsService : IDiagnosticsService
        {
            public FakeDiagnosticsService(DiagnosticsSnapshot snapshot)
            {
                Snapshot = snapshot;
            }

            public DiagnosticsSnapshot Snapshot { get; }
        }
    }
}
