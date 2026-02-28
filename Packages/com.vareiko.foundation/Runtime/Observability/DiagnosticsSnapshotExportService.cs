using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class DiagnosticsSnapshotExportService : IDiagnosticsSnapshotExportService
    {
        private const string DefaultLabel = "snapshot";
        private const string DefaultFilePrefix = "diagnostics";

        private readonly IDiagnosticsService _diagnosticsService;
        private readonly SignalBus _signalBus;
        private readonly IFoundationLogger _logger;
        private readonly string _exportDirectory;

        [Inject]
        public DiagnosticsSnapshotExportService(
            IDiagnosticsService diagnosticsService,
            [Inject(Id = "DiagnosticsExportRootPath")] string exportDirectory,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional] IFoundationLogger logger = null)
        {
            _diagnosticsService = diagnosticsService;
            _exportDirectory = exportDirectory ?? string.Empty;
            _signalBus = signalBus;
            _logger = logger;
        }

        public string ExportDirectory => _exportDirectory;

        public UniTask<DiagnosticsSnapshotExportResult> ExportAsync(string label = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticsSnapshot snapshot = _diagnosticsService != null ? _diagnosticsService.Snapshot : null;
            if (snapshot == null)
            {
                return UniTask.FromResult(Fail("Diagnostics snapshot is not available."));
            }

            if (string.IsNullOrWhiteSpace(_exportDirectory))
            {
                return UniTask.FromResult(Fail("Diagnostics export directory is not configured."));
            }

            try
            {
                Directory.CreateDirectory(_exportDirectory);
                string fileName = BuildFileName(label);
                string filePath = Path.Combine(_exportDirectory, fileName);

                ExportEnvelope envelope = new ExportEnvelope
                {
                    ExportedAtUtc = DateTime.UtcNow.ToString("O"),
                    ApplicationVersion = Application.version ?? string.Empty,
                    UnityVersion = Application.unityVersion ?? string.Empty,
                    Snapshot = CreateSnapshotDto(snapshot)
                };

                string json = JsonUtility.ToJson(envelope, true);
                File.WriteAllText(filePath, json, Encoding.UTF8);

                _signalBus?.Fire(new DiagnosticsSnapshotExportedSignal(filePath));
                _logger?.Info($"Diagnostics snapshot exported: {filePath}", "Diagnostics");
                return UniTask.FromResult(DiagnosticsSnapshotExportResult.Succeed(filePath));
            }
            catch (Exception exception)
            {
                return UniTask.FromResult(Fail(exception.Message));
            }
        }

        private DiagnosticsSnapshotExportResult Fail(string error)
        {
            string safeError = string.IsNullOrWhiteSpace(error) ? "Diagnostics export failed." : error;
            _signalBus?.Fire(new DiagnosticsSnapshotExportFailedSignal(safeError));
            _logger?.Warn($"Diagnostics snapshot export failed: {safeError}", "Diagnostics");
            return DiagnosticsSnapshotExportResult.Fail(safeError);
        }

        private static string BuildFileName(string label)
        {
            string sanitizedLabel = SanitizeLabel(label);
            return $"{DefaultFilePrefix}_{sanitizedLabel}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json";
        }

        private static string SanitizeLabel(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return DefaultLabel;
            }

            string value = source.Trim();
            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char symbol = value[i];
                builder.Append(char.IsLetterOrDigit(symbol) ? symbol : '_');
            }

            string normalized = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? DefaultLabel : normalized;
        }

        private static DiagnosticsSnapshotDto CreateSnapshotDto(DiagnosticsSnapshot snapshot)
        {
            return new DiagnosticsSnapshotDto
            {
                IsBootCompleted = snapshot.IsBootCompleted,
                IsBootFailed = snapshot.IsBootFailed,
                LastBootError = snapshot.LastBootError ?? string.Empty,
                IsOnline = snapshot.IsOnline,
                IsLoading = snapshot.IsLoading,
                LoadingProgress = snapshot.LoadingProgress,
                IsBackendConfigured = snapshot.IsBackendConfigured,
                IsBackendAuthenticated = snapshot.IsBackendAuthenticated,
                RemoteConfigValues = snapshot.RemoteConfigValues,
                TrackedAssets = snapshot.TrackedAssets,
                AssetReferences = snapshot.AssetReferences,
                LastUpdatedAt = snapshot.LastUpdatedAt
            };
        }

        [Serializable]
        private sealed class ExportEnvelope
        {
            public string ExportedAtUtc = string.Empty;
            public string ApplicationVersion = string.Empty;
            public string UnityVersion = string.Empty;
            public DiagnosticsSnapshotDto Snapshot;
        }

        [Serializable]
        private sealed class DiagnosticsSnapshotDto
        {
            public bool IsBootCompleted;
            public bool IsBootFailed;
            public string LastBootError = string.Empty;
            public bool IsOnline;
            public bool IsLoading;
            public float LoadingProgress;
            public bool IsBackendConfigured;
            public bool IsBackendAuthenticated;
            public int RemoteConfigValues;
            public int TrackedAssets;
            public int AssetReferences;
            public float LastUpdatedAt;
        }
    }
}
