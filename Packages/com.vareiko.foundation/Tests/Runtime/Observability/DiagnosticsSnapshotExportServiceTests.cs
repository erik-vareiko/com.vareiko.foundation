using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Observability;
using Zenject;

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

                SignalBus signalBus = CreateSignalBus();
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

                DiagnosticsSnapshotExportResult result = await service.ExportAsync("qa run#1");

                Assert.That(result.Success, Is.True);
                Assert.That(result.FilePath, Is.Not.Empty);
                Assert.That(File.Exists(result.FilePath), Is.True);
                Assert.That(exportedSignalReceived, Is.True);
                Assert.That(exportedPath, Is.EqualTo(result.FilePath));
                Assert.That(Path.GetFileName(result.FilePath), Does.Contain("qa_run_1"));

                string json = File.ReadAllText(result.FilePath);
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
            SignalBus signalBus = CreateSignalBus();
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

            DiagnosticsSnapshotExportResult result = await service.ExportAsync();

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("not available"));
            Assert.That(failedSignalReceived, Is.True);
            Assert.That(failedError, Does.Contain("not available"));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<DiagnosticsSnapshotExportedSignal>();
            container.DeclareSignal<DiagnosticsSnapshotExportFailedSignal>();
            return container.Resolve<SignalBus>();
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
