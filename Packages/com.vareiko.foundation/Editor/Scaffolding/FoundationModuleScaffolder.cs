using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Vareiko.Foundation.Editor.Scaffolding
{
    public sealed class FoundationModuleScaffolder : EditorWindow
    {
        private const string TemplatesRelativePath = "Editor/Scaffolding/Templates";
        private static readonly Regex ModuleNameRegex = new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex NamespaceRegex = new Regex("^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled);

        private string _moduleName = "NewModule";
        private string _namespaceRoot = "Game";
        private string _outputFolder = "Assets/Scripts";
        private bool _generateTestStub = true;
        private bool _generateIntegrationSample = true;

        [MenuItem("Tools/Vareiko/Foundation/Create Runtime Module")]
        private static void OpenWindow()
        {
            FoundationModuleScaffolder window = GetWindow<FoundationModuleScaffolder>("Foundation Scaffolder");
            window.minSize = new Vector2(480f, 220f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Generate Runtime Module Skeleton", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates runtime module files with optional test stub and integration sample installer.", MessageType.Info);

            _moduleName = EditorGUILayout.TextField("Module Name", _moduleName);
            _namespaceRoot = EditorGUILayout.TextField("Root Namespace", _namespaceRoot);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, string.Empty);
                    if (!string.IsNullOrWhiteSpace(selected))
                    {
                        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        string fullSelected = Path.GetFullPath(selected);
                        if (fullSelected.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            string relative = fullSelected.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            _outputFolder = relative.Replace('\\', '/');
                        }
                    }
                }
            }

            EditorGUILayout.Space(4f);
            _generateTestStub = EditorGUILayout.ToggleLeft("Generate Test Stub", _generateTestStub);
            _generateIntegrationSample = EditorGUILayout.ToggleLeft("Generate Integration Sample", _generateIntegrationSample);

            EditorGUILayout.Space(12f);
            EditorGUI.BeginDisabledGroup(!CanGenerate(out string validationMessage));
            if (GUILayout.Button("Generate Module", GUILayout.Height(30f)))
            {
                GenerateModule();
            }
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            }
        }

        private bool CanGenerate(out string message)
        {
            if (!ModuleNameRegex.IsMatch(_moduleName ?? string.Empty))
            {
                message = "Module name must start with a letter and contain only letters, digits or underscore.";
                return false;
            }

            if (!NamespaceRegex.IsMatch(_namespaceRoot ?? string.Empty))
            {
                message = "Root namespace must be a valid C# namespace (letters, digits, underscore, dot).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_outputFolder) || !_outputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                message = "Output folder must be inside the Unity project Assets folder.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private void GenerateModule()
        {
            string templateDirectory = GetTemplateDirectory();
            if (string.IsNullOrWhiteSpace(templateDirectory) || !Directory.Exists(templateDirectory))
            {
                EditorUtility.DisplayDialog("Foundation Scaffolder", "Template directory was not found in the package.", "OK");
                return;
            }

            string moduleFolder = Path.Combine(_outputFolder, _moduleName).Replace('\\', '/');
            if (Directory.Exists(moduleFolder))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Foundation Scaffolder",
                    $"Folder '{moduleFolder}' already exists. Overwrite generated files?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite)
                {
                    return;
                }
            }

            Directory.CreateDirectory(moduleFolder);

            string namespaceValue = $"{_namespaceRoot}.{_moduleName}";
            Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "{{MODULE_NAME}}", _moduleName },
                { "{{NAMESPACE}}", namespaceValue }
            };

            WriteFromTemplate(templateDirectory, "IService.cs.txt", Path.Combine(moduleFolder, $"I{_moduleName}Service.cs"), replacements);
            WriteFromTemplate(templateDirectory, "Service.cs.txt", Path.Combine(moduleFolder, $"{_moduleName}Service.cs"), replacements);
            WriteFromTemplate(templateDirectory, "Config.cs.txt", Path.Combine(moduleFolder, $"{_moduleName}Config.cs"), replacements);
            WriteFromTemplate(templateDirectory, "Signals.cs.txt", Path.Combine(moduleFolder, $"{_moduleName}Signals.cs"), replacements);
            WriteFromTemplate(templateDirectory, "Installer.cs.txt", Path.Combine(moduleFolder, $"Foundation{_moduleName}Installer.cs"), replacements);
            if (_generateTestStub)
            {
                string testsFolder = Path.Combine(moduleFolder, "Tests").Replace('\\', '/');
                Directory.CreateDirectory(testsFolder);
                WriteFromTemplate(templateDirectory, "Tests.cs.txt", Path.Combine(testsFolder, $"{_moduleName}ServiceTests.cs"), replacements);
            }

            if (_generateIntegrationSample)
            {
                string sampleFolder = Path.Combine(moduleFolder, "Sample").Replace('\\', '/');
                Directory.CreateDirectory(sampleFolder);
                WriteFromTemplate(templateDirectory, "SampleInstaller.cs.txt", Path.Combine(sampleFolder, $"{_moduleName}SampleInstaller.cs"), replacements);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Foundation Scaffolder", $"Module '{_moduleName}' created in '{moduleFolder}'.", "OK");
        }

        private static void WriteFromTemplate(string templateDirectory, string templateFile, string outputFile, IReadOnlyDictionary<string, string> replacements)
        {
            string sourcePath = Path.Combine(templateDirectory, templateFile);
            string content = File.ReadAllText(sourcePath);
            foreach (KeyValuePair<string, string> pair in replacements)
            {
                content = content.Replace(pair.Key, pair.Value);
            }

            File.WriteAllText(outputFile, content);
        }

        private static string GetTemplateDirectory()
        {
            PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(FoundationModuleScaffolder).Assembly);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
            {
                return string.Empty;
            }

            return Path.Combine(packageInfo.resolvedPath, TemplatesRelativePath);
        }
    }
}
