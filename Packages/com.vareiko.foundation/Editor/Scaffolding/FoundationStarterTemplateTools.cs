using System.IO;
using UnityEditor;
using UnityEngine;
using Vareiko.Foundation.Environment;

namespace Vareiko.Foundation.Editor.Scaffolding
{
    public static class FoundationStarterTemplateTools
    {
        private const string CreateMenuPath = "Tools/Vareiko/Foundation/Create Starter Environment Config";
        private const string ContextMenuPath = "CONTEXT/EnvironmentConfig/Apply Starter Presets (dev/stage/prod)";

        [MenuItem(CreateMenuPath)]
        private static void CreateStarterEnvironmentConfig()
        {
            string folder = GetActiveFolderPath();
            string candidatePath = Path.Combine(folder, "EnvironmentConfig.asset").Replace('\\', '/');
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(candidatePath);

            EnvironmentConfig config = ScriptableObject.CreateInstance<EnvironmentConfig>();
            config.ApplyStarterPresets();

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
            EditorUtility.DisplayDialog("Foundation Starter Template", $"Starter EnvironmentConfig created:\n{assetPath}", "OK");
        }

        [MenuItem(ContextMenuPath)]
        private static void ApplyStarterPresets(MenuCommand command)
        {
            EnvironmentConfig config = command != null ? command.context as EnvironmentConfig : null;
            if (config == null)
            {
                return;
            }

            Undo.RecordObject(config, "Apply Starter Environment Presets");
            config.ApplyStarterPresets();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Foundation Starter Template", "Applied starter presets: dev/stage/prod.", "OK");
        }

        private static string GetActiveFolderPath()
        {
            Object activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                return "Assets";
            }

            string path = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Assets";
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return "Assets";
            }

            return directory.Replace('\\', '/');
        }
    }
}
