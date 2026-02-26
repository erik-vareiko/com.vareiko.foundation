using System;
using System.Collections.Generic;
using System.Text;
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
