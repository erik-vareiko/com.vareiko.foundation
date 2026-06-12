using System.Reflection;
using UnityEngine;
using Vareiko.Foundation.Installers;

namespace Vareiko.Foundation.Samples.VerticalSlice
{
    /// <summary>
    /// Drop on an empty GameObject in an empty scene and press Play: wires the full foundation
    /// composition (project scope, scene scope, bootstrap task, gameplay driver) from code.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureProjectScope();

            // Scene objects must exist before the scene scope builds so its scene-wide
            // injection pass reaches them.
            LoadProfileBootstrapTask profileTask = new GameObject("SliceProfileTask").AddComponent<LoadProfileBootstrapTask>();
            SliceGameplayDriver driver = new GameObject("SliceGameplay").AddComponent<SliceGameplayDriver>();
            driver.SetProfileTask(profileTask);

            CreateSceneScope(profileTask);
        }

        private static void EnsureProjectScope()
        {
            if (FindFirstObjectByType<FoundationProjectInstaller>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject projectRoot = new GameObject("FoundationProjectScope");
            projectRoot.AddComponent<FoundationProjectInstaller>();
            DontDestroyOnLoad(projectRoot);
        }

        private static void CreateSceneScope(LoadProfileBootstrapTask profileTask)
        {
            if (FindFirstObjectByType<FoundationSceneInstaller>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[VerticalSlice] A FoundationSceneInstaller already exists; the slice bootstrap task was not registered on it.");
                return;
            }

            // The scope builds in Awake — create it inactive, assign the serialized
            // bootstrap-task list, then activate.
            GameObject sceneScopeRoot = new GameObject("FoundationSceneScope");
            sceneScopeRoot.SetActive(false);
            FoundationSceneInstaller installer = sceneScopeRoot.AddComponent<FoundationSceneInstaller>();

            FieldInfo tasksField = typeof(FoundationSceneInstaller)
                .GetField("_bootstrapTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            tasksField?.SetValue(installer, new MonoBehaviour[] { profileTask });

            sceneScopeRoot.SetActive(true);
        }
    }
}
