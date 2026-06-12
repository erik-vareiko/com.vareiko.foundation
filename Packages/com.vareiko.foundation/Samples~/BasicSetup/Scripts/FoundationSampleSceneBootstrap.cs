using System.Reflection;
using UnityEngine;
using Vareiko.Foundation.Installers;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Samples.BasicSetup
{
    public sealed class FoundationSampleSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _setupOnAwake = true;

        private void Awake()
        {
            if (_setupOnAwake)
            {
                EnsureFoundationWiring();
            }
        }

        [ContextMenu("Ensure Foundation Wiring")]
        public void EnsureFoundationWiring()
        {
            EnsureProjectScope();
            UIRegistry registry = EnsureUiRegistry();
            EnsureSceneScope(registry);
        }

        private static void EnsureProjectScope()
        {
            FoundationProjectInstaller projectInstaller = FindFirst<FoundationProjectInstaller>();
            if (projectInstaller == null)
            {
                GameObject projectRoot = new GameObject("FoundationProjectScope");
                projectInstaller = projectRoot.AddComponent<FoundationProjectInstaller>();
                DontDestroyOnLoad(projectRoot);
            }
        }

        private static FoundationSceneInstaller EnsureSceneScope(UIRegistry registry)
        {
            FoundationSceneInstaller installer = FindFirst<FoundationSceneInstaller>();
            if (installer != null)
            {
                return installer;
            }

            // The scope builds in Awake, so the registry reference must be assigned before the
            // GameObject activates — create it inactive, wire it up, then let it build.
            GameObject sceneScopeRoot = new GameObject("FoundationSceneScope");
            sceneScopeRoot.SetActive(false);
            installer = sceneScopeRoot.AddComponent<FoundationSceneInstaller>();
            AssignPrivate(installer, "_uiRegistry", registry);
            sceneScopeRoot.SetActive(true);
            return installer;
        }

        private static UIRegistry EnsureUiRegistry()
        {
            UIRegistry registry = FindFirst<UIRegistry>();
            if (registry == null)
            {
                GameObject uiRoot = new GameObject("UIRoot");
                registry = uiRoot.AddComponent<UIRegistry>();
            }

            return registry;
        }

        private static T FindFirst<T>() where T : Object
        {
            T[] all = FindObjectsOfType<T>(true);
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private static void AssignPrivate(object instance, string fieldName, object value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(instance, value);
        }
    }
}
