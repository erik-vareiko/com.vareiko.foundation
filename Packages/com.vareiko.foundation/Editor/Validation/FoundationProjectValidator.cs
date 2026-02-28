using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vareiko.Foundation.Installers;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Editor.Validation
{
    public static class FoundationProjectValidator
    {
        private const string MenuPath = "Tools/Vareiko/Foundation/Validate Project";
        private const string PackageRelativeRoot = "Packages/com.vareiko.foundation";
        private const string ProjectVersionRelativePath = "ProjectSettings/ProjectVersion.txt";

        private static readonly string[] RequiredDependencies =
        {
            "com.cysharp.unitask",
            "net.bobbo.extenject",
            "com.unity.inputsystem"
        };

        private static readonly string[] MergeScanRelativeRoots =
        {
            "Packages",
            "ProjectSettings",
            "Tools",
            ".github"
        };

        private static readonly HashSet<string> MergeScanExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".md",
            ".json",
            ".asmdef",
            ".yml",
            ".yaml",
            ".txt"
        };

        [MenuItem(MenuPath)]
        public static void ValidateProject()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            ValidationReport report = new ValidationReport();

            try
            {
                ValidateReleaseGate(report);
                ValidateScenes(report);
                ValidateProjectContextPrefab(report);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            LogReport(report);
        }

        private static void ValidateScenes(ValidationReport report)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            if (sceneGuids == null || sceneGuids.Length == 0)
            {
                report.Add(ValidationSeverity.Warning, "SCN-000", "No scenes found under Assets.");
                return;
            }

            int totalSceneInstallers = 0;
            int totalProjectInstallersInScenes = 0;

            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    continue;
                }

                Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!openedScene.IsValid() || !openedScene.isLoaded)
                {
                    report.Add(ValidationSeverity.Error, "SCN-001", "Failed to open scene.", scenePath);
                    continue;
                }

                FoundationSceneInstaller[] sceneInstallers = UnityEngine.Object.FindObjectsOfType<FoundationSceneInstaller>(true);
                FoundationProjectInstaller[] projectInstallers = UnityEngine.Object.FindObjectsOfType<FoundationProjectInstaller>(true);

                totalSceneInstallers += sceneInstallers.Length;
                totalProjectInstallersInScenes += projectInstallers.Length;

                if (sceneInstallers.Length == 0)
                {
                    report.Add(ValidationSeverity.Warning, "SCN-010", "Scene has no FoundationSceneInstaller.", scenePath);
                }
                else if (sceneInstallers.Length > 1)
                {
                    report.Add(ValidationSeverity.Warning, "SCN-011", $"Scene has multiple FoundationSceneInstaller components ({sceneInstallers.Length}).", scenePath);
                }

                ValidateUiElements(scenePath, report);
            }

            if (totalSceneInstallers == 0)
            {
                report.Add(ValidationSeverity.Error, "SCN-100", "No FoundationSceneInstaller found in any scene under Assets.");
            }

            if (totalProjectInstallersInScenes == 0)
            {
                report.Add(ValidationSeverity.Info, "SCN-101", "No FoundationProjectInstaller found in scenes. This is valid if ProjectContext prefab is used.");
            }
        }

        private static void ValidateUiElements(string scenePath, ValidationReport report)
        {
            UIElement[] elements = UnityEngine.Object.FindObjectsOfType<UIElement>(true);
            UIRegistry[] registries = UnityEngine.Object.FindObjectsOfType<UIRegistry>(true);
            if (elements.Length == 0)
            {
                return;
            }

            if (registries.Length == 0)
            {
                report.Add(ValidationSeverity.Warning, "UI-001", "Scene has UIElement objects but no UIRegistry.", scenePath);
            }
            else if (registries.Length > 1)
            {
                report.Add(ValidationSeverity.Info, "UI-002", $"Scene has multiple UIRegistry components ({registries.Length}).", scenePath);
            }

            Dictionary<string, UIElement> byId = new Dictionary<string, UIElement>(StringComparer.Ordinal);
            for (int i = 0; i < elements.Length; i++)
            {
                UIElement element = elements[i];
                if (element == null)
                {
                    continue;
                }

                string id = element.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    report.Add(ValidationSeverity.Warning, "UI-010", $"UIElement '{element.name}' has empty Id.", scenePath);
                    continue;
                }

                if (byId.TryGetValue(id, out UIElement existing))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "UI-011",
                        $"Duplicate UIElement Id '{id}' in scene ('{existing.name}' and '{element.name}').",
                        scenePath);
                    continue;
                }

                byId[id] = element;
            }
        }

        private static void ValidateProjectContextPrefab(ValidationReport report)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            List<string> projectInstallerPrefabs = new List<string>();
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrWhiteSpace(prefabPath))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                FoundationProjectInstaller installer = prefab.GetComponentInChildren<FoundationProjectInstaller>(true);
                if (installer != null)
                {
                    projectInstallerPrefabs.Add(prefabPath);
                }
            }

            if (projectInstallerPrefabs.Count == 0)
            {
                report.Add(ValidationSeverity.Warning, "CTX-001", "No prefab with FoundationProjectInstaller found under Assets.");
            }
            else if (projectInstallerPrefabs.Count > 1)
            {
                report.Add(ValidationSeverity.Warning, "CTX-002", $"Multiple prefabs with FoundationProjectInstaller found ({projectInstallerPrefabs.Count}).");
                for (int i = 0; i < projectInstallerPrefabs.Count; i++)
                {
                    report.Add(ValidationSeverity.Info, "CTX-003", "Candidate ProjectContext prefab.", projectInstallerPrefabs[i]);
                }
            }
            else
            {
                report.Add(ValidationSeverity.Info, "CTX-004", "ProjectContext prefab candidate found.", projectInstallerPrefabs[0]);
            }
        }

        private static void ValidateReleaseGate(ValidationReport report)
        {
            string projectRoot = GetProjectRoot();
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                report.Add(ValidationSeverity.Error, "REL-000", "Unable to resolve Unity project root.");
                return;
            }

            string packageRoot = Path.Combine(projectRoot, PackageRelativeRoot);
            string packageJsonPath = Path.Combine(packageRoot, "package.json");
            string changelogPath = Path.Combine(packageRoot, "CHANGELOG.md");
            string projectVersionPath = Path.Combine(projectRoot, ProjectVersionRelativePath);

            bool hasRequiredFiles = true;
            if (!File.Exists(packageJsonPath))
            {
                report.Add(ValidationSeverity.Error, "REL-001", "package.json not found.", ToProjectRelativePath(packageJsonPath, projectRoot));
                hasRequiredFiles = false;
            }

            if (!File.Exists(changelogPath))
            {
                report.Add(ValidationSeverity.Error, "REL-002", "CHANGELOG.md not found.", ToProjectRelativePath(changelogPath, projectRoot));
                hasRequiredFiles = false;
            }

            if (!File.Exists(projectVersionPath))
            {
                report.Add(ValidationSeverity.Error, "REL-003", "ProjectVersion.txt not found.", ToProjectRelativePath(projectVersionPath, projectRoot));
                hasRequiredFiles = false;
            }

            if (!Directory.Exists(packageRoot))
            {
                report.Add(ValidationSeverity.Error, "REL-004", "Package root directory not found.", ToProjectRelativePath(packageRoot, projectRoot));
                hasRequiredFiles = false;
            }

            if (!hasRequiredFiles)
            {
                return;
            }

            string packageJsonRaw = SafeReadAllText(packageJsonPath, report, "REL-005", projectRoot);
            string changelogRaw = SafeReadAllText(changelogPath, report, "REL-006", projectRoot);
            string projectVersionRaw = SafeReadAllText(projectVersionPath, report, "REL-007", projectRoot);
            if (string.IsNullOrEmpty(packageJsonRaw) || string.IsNullOrEmpty(changelogRaw) || string.IsNullOrEmpty(projectVersionRaw))
            {
                return;
            }

            string packageVersion = ExtractPackageVersion(packageJsonRaw, report);
            ValidateVersionAlignment(packageVersion, changelogRaw, report);
            ValidateRequiredDependencies(packageJsonRaw, report);
            ValidateScriptMetaFiles(packageRoot, report, projectRoot);
            ValidateMergeConflictMarkers(projectRoot, report);
            ValidateUnityVersion(projectVersionRaw, report);
        }

        private static string GetProjectRoot()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return path.Replace('\\', '/');
        }

        private static string SafeReadAllText(string filePath, ValidationReport report, string code, string projectRoot)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception exception)
            {
                report.Add(
                    ValidationSeverity.Error,
                    code,
                    $"Failed to read file: {exception.Message}",
                    ToProjectRelativePath(filePath, projectRoot));
                return string.Empty;
            }
        }

        private static string ExtractPackageVersion(string packageJsonRaw, ValidationReport report)
        {
            Match match = Regex.Match(packageJsonRaw, "\"version\"\\s*:\\s*\"(?<value>[^\"]+)\"", RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                report.Add(ValidationSeverity.Error, "REL-010", "Unable to parse package version from package.json.");
                return string.Empty;
            }

            string value = match.Groups["value"].Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Add(ValidationSeverity.Error, "REL-011", "Package version in package.json is empty.");
                return string.Empty;
            }

            return value;
        }

        private static void ValidateVersionAlignment(string packageVersion, string changelogRaw, ValidationReport report)
        {
            Match changelogMatch = Regex.Match(changelogRaw, "(?m)^##\\s+([0-9]+\\.[0-9]+\\.[0-9]+)\\s*$", RegexOptions.CultureInvariant);
            if (!changelogMatch.Success)
            {
                report.Add(ValidationSeverity.Error, "REL-020", "Unable to parse top changelog version heading.");
                return;
            }

            string changelogVersion = changelogMatch.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                return;
            }

            if (!string.Equals(changelogVersion, packageVersion, StringComparison.Ordinal))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "REL-021",
                    $"Version mismatch: package.json={packageVersion}, CHANGELOG.md={changelogVersion}.");
                return;
            }

            report.Add(ValidationSeverity.Info, "REL-022", $"Version alignment OK ({packageVersion}).");
        }

        private static void ValidateRequiredDependencies(string packageJsonRaw, ValidationReport report)
        {
            Match dependenciesMatch = Regex.Match(
                packageJsonRaw,
                "\"dependencies\"\\s*:\\s*\\{(?<body>[\\s\\S]*?)\\}",
                RegexOptions.CultureInvariant);

            if (!dependenciesMatch.Success)
            {
                report.Add(ValidationSeverity.Error, "REL-030", "Dependencies object not found in package.json.");
                return;
            }

            string body = dependenciesMatch.Groups["body"].Value;
            for (int i = 0; i < RequiredDependencies.Length; i++)
            {
                string dependency = RequiredDependencies[i];
                string pattern = $"\"{Regex.Escape(dependency)}\"\\s*:";
                if (!Regex.IsMatch(body, pattern, RegexOptions.CultureInvariant))
                {
                    report.Add(ValidationSeverity.Error, "REL-031", $"Missing required dependency '{dependency}' in package.json.");
                }
            }
        }

        private static void ValidateScriptMetaFiles(string packageRoot, ValidationReport report, string projectRoot)
        {
            string[] scripts;
            try
            {
                scripts = Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                report.Add(ValidationSeverity.Error, "REL-043", $"Failed to enumerate scripts for .meta validation: {exception.Message}");
                return;
            }

            List<string> missingMeta = new List<string>();
            for (int i = 0; i < scripts.Length; i++)
            {
                string scriptPath = scripts[i];
                string metaPath = scriptPath + ".meta";
                if (!File.Exists(metaPath))
                {
                    missingMeta.Add(scriptPath);
                }
            }

            if (missingMeta.Count == 0)
            {
                return;
            }

            report.Add(ValidationSeverity.Error, "REL-040", $"Missing .meta files for scripts ({missingMeta.Count}).");
            int previewCount = Math.Min(10, missingMeta.Count);
            for (int i = 0; i < previewCount; i++)
            {
                report.Add(ValidationSeverity.Info, "REL-041", "Script without .meta.", ToProjectRelativePath(missingMeta[i], projectRoot));
            }

            if (missingMeta.Count > previewCount)
            {
                report.Add(ValidationSeverity.Info, "REL-042", $"...and {missingMeta.Count - previewCount} more missing .meta file(s).");
            }
        }

        private static void ValidateMergeConflictMarkers(string projectRoot, ValidationReport report)
        {
            List<string> hits = new List<string>();

            for (int i = 0; i < MergeScanRelativeRoots.Length; i++)
            {
                string rootPath = Path.Combine(projectRoot, MergeScanRelativeRoots[i]);
                if (!Directory.Exists(rootPath))
                {
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
                }
                catch (Exception exception)
                {
                    report.Add(
                        ValidationSeverity.Warning,
                        "REL-053",
                        $"Failed to enumerate files for merge-marker scan: {exception.Message}",
                        ToProjectRelativePath(rootPath, projectRoot));
                    continue;
                }

                for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
                {
                    string path = files[fileIndex];
                    if (!MergeScanExtensions.Contains(Path.GetExtension(path)))
                    {
                        continue;
                    }

                    if (ContainsMergeMarker(path))
                    {
                        hits.Add(path);
                    }
                }
            }

            if (hits.Count == 0)
            {
                return;
            }

            report.Add(ValidationSeverity.Error, "REL-050", $"Merge conflict markers found in {hits.Count} file(s).");
            int previewCount = Math.Min(10, hits.Count);
            for (int i = 0; i < previewCount; i++)
            {
                report.Add(ValidationSeverity.Info, "REL-051", "Merge conflict marker found.", ToProjectRelativePath(hits[i], projectRoot));
            }

            if (hits.Count > previewCount)
            {
                report.Add(ValidationSeverity.Info, "REL-052", $"...and {hits.Count - previewCount} more file(s) with merge markers.");
            }
        }

        private static bool ContainsMergeMarker(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                        {
                            continue;
                        }

                        if (line.StartsWith("<<<<<<<", StringComparison.Ordinal) ||
                            line.StartsWith("=======", StringComparison.Ordinal) ||
                            line.StartsWith(">>>>>>>", StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void ValidateUnityVersion(string projectVersionRaw, ValidationReport report)
        {
            Match versionMatch = Regex.Match(
                projectVersionRaw,
                "(?m)^m_EditorVersion:\\s*([0-9a-zA-Z\\.\\-]+)\\s*$",
                RegexOptions.CultureInvariant);

            if (!versionMatch.Success)
            {
                report.Add(ValidationSeverity.Error, "REL-060", "Unable to parse Unity version from ProjectVersion.txt.");
                return;
            }

            report.Add(ValidationSeverity.Info, "REL-061", $"Unity version parsed: {versionMatch.Groups[1].Value}.");
        }

        private static string ToProjectRelativePath(string absolutePath, string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return absolutePath.Replace('\\', '/');
            }

            string normalizedAbsolute = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string normalizedRoot = Path.GetFullPath(projectRoot).Replace('\\', '/').TrimEnd('/');
            if (normalizedAbsolute.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedAbsolute.Substring(normalizedRoot.Length).TrimStart('/');
            }

            return normalizedAbsolute;
        }

        private static void LogReport(ValidationReport report)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[Foundation Validation]");
            builder.AppendLine($"Errors: {report.ErrorCount} | Warnings: {report.WarningCount} | Info: {report.InfoCount}");

            for (int i = 0; i < report.Issues.Count; i++)
            {
                ValidationIssue issue = report.Issues[i];
                string pathSuffix = string.IsNullOrWhiteSpace(issue.Path) ? string.Empty : $" [{issue.Path}]";
                builder.AppendLine($"- {issue.Severity} {issue.Code}: {issue.Message}{pathSuffix}");
            }

            string output = builder.ToString();
            if (report.ErrorCount > 0)
            {
                Debug.LogError(output);
                EditorUtility.DisplayDialog("Foundation Validation", $"Validation finished with {report.ErrorCount} error(s). See Console.", "OK");
                return;
            }

            if (report.WarningCount > 0)
            {
                Debug.LogWarning(output);
                EditorUtility.DisplayDialog("Foundation Validation", $"Validation finished with {report.WarningCount} warning(s). See Console.", "OK");
                return;
            }

            Debug.Log(output);
            EditorUtility.DisplayDialog("Foundation Validation", "Validation passed without warnings.", "OK");
        }

        private enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }

        private readonly struct ValidationIssue
        {
            public readonly ValidationSeverity Severity;
            public readonly string Code;
            public readonly string Message;
            public readonly string Path;

            public ValidationIssue(ValidationSeverity severity, string code, string message, string path)
            {
                Severity = severity;
                Code = code ?? string.Empty;
                Message = message ?? string.Empty;
                Path = path ?? string.Empty;
            }
        }

        private sealed class ValidationReport
        {
            private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();

            public IReadOnlyList<ValidationIssue> Issues => _issues;
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }
            public int InfoCount { get; private set; }

            public void Add(ValidationSeverity severity, string code, string message, string path = "")
            {
                _issues.Add(new ValidationIssue(severity, code, message, path));
                switch (severity)
                {
                    case ValidationSeverity.Error:
                        ErrorCount++;
                        break;
                    case ValidationSeverity.Warning:
                        WarningCount++;
                        break;
                    default:
                        InfoCount++;
                        break;
                }
            }
        }
    }
}
