using System.Reflection;
using UnityEngine;
using Vareiko.Foundation.Installers;
using Vareiko.Foundation.UI;
using Zenject;

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
            EnsureProjectContext();
            FoundationSceneInstaller sceneInstaller = EnsureSceneContextAndInstaller();
            UIRegistry registry = EnsureUiRegistry();
            AssignPrivate(sceneInstaller, "_uiRegistry", registry);
        }

        private static void EnsureProjectContext()
        {
            ProjectContext projectContext = FindFirst<ProjectContext>();
            if (projectContext == null)
            {
                GameObject contextRoot = new GameObject("ProjectContext");
                projectContext = contextRoot.AddComponent<ProjectContext>();
            }

            if (projectContext.GetComponent<FoundationProjectInstaller>() == null)
            {
                projectContext.gameObject.AddComponent<FoundationProjectInstaller>();
            }
        }

        private static FoundationSceneInstaller EnsureSceneContextAndInstaller()
        {
            SceneContext sceneContext = FindFirst<SceneContext>();
            if (sceneContext == null)
            {
                GameObject sceneContextRoot = new GameObject("SceneContext");
                sceneContext = sceneContextRoot.AddComponent<SceneContext>();
            }

            FoundationSceneInstaller installer = sceneContext.GetComponent<FoundationSceneInstaller>();
            if (installer == null)
            {
                installer = sceneContext.gameObject.AddComponent<FoundationSceneInstaller>();
            }

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
