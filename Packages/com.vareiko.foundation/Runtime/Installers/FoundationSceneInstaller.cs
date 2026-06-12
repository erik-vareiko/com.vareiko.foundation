using System.Collections.Generic;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Config;
using Vareiko.Foundation.UINavigation;
using Vareiko.Foundation.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Installers
{
    public sealed class FoundationSceneInstaller : LifetimeScope
    {
        [SerializeField] private UIRegistry _uiRegistry;
        [SerializeField] private MonoBehaviour[] _bootstrapTasks;
        [SerializeField] private ConfigRegistry[] _configRegistries;
        [SerializeField] private bool _injectSceneObjects = true;

        protected override void Configure(IContainerBuilder builder)
        {
            // Zenject's SceneContext injected every MonoBehaviour in the scene; VContainer does not.
            // This callback replays that behaviour for scene-placed components with VContainer
            // [Inject] methods (UI binders, button actions, overlay presenters). It MUST be
            // registered before the first RegisterEntryPoint below: build callbacks run in
            // registration order, and the entry-point dispatcher (which runs Initialize, including
            // UIService and BootstrapRunner) is itself a build callback added by RegisterEntryPoint.
            if (_injectSceneObjects)
            {
                builder.RegisterBuildCallback(InjectSceneObjects);
            }

            FoundationBootstrapInstaller.Install(builder);
            FoundationUIInstaller.Install(builder);
            FoundationUINavigationInstaller.Install(builder);

            if (_uiRegistry != null)
            {
                builder.RegisterInstance(_uiRegistry);
            }

            RegisterBootstrapTasks(builder);
        }

        private void InjectSceneObjects(IObjectResolver resolver)
        {
            List<GameObject> roots = new List<GameObject>();
            gameObject.scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                resolver.InjectGameObject(roots[i]);
            }
        }

        private void RegisterBootstrapTasks(IContainerBuilder builder)
        {
            bool hasBootstrapTasks = _bootstrapTasks != null && _bootstrapTasks.Length > 0;
            bool hasConfigRegistries = _configRegistries != null && _configRegistries.Length > 0;
            if (!hasBootstrapTasks && !hasConfigRegistries)
            {
                return;
            }

            HashSet<IBootstrapTask> uniqueTasks = new HashSet<IBootstrapTask>();
            if (hasBootstrapTasks)
            {
                for (int i = 0; i < _bootstrapTasks.Length; i++)
                {
                    MonoBehaviour taskBehaviour = _bootstrapTasks[i];
                    if (taskBehaviour == null)
                    {
                        continue;
                    }

                    IBootstrapTask task = taskBehaviour as IBootstrapTask;
                    if (task == null || !uniqueTasks.Add(task))
                    {
                        continue;
                    }

                    // RegisterComponent (not RegisterInstance) so VContainer injects the task's
                    // [Inject] Construct method — RegisterInstance assumes a fully-built object and
                    // skips injection, which would leave required deps (e.g. ConfigRegistry's
                    // IConfigService) null and silently no-op the task at boot.
                    builder.RegisterComponent(task);
                }
            }

            if (hasConfigRegistries)
            {
                for (int i = 0; i < _configRegistries.Length; i++)
                {
                    ConfigRegistry registry = _configRegistries[i];
                    if (registry == null)
                    {
                        continue;
                    }

                    IBootstrapTask task = registry;
                    if (!uniqueTasks.Add(task))
                    {
                        continue;
                    }

                    // RegisterComponent (not RegisterInstance) so VContainer injects the task's
                    // [Inject] Construct method — RegisterInstance assumes a fully-built object and
                    // skips injection, which would leave required deps (e.g. ConfigRegistry's
                    // IConfigService) null and silently no-op the task at boot.
                    builder.RegisterComponent(task);
                }
            }
        }
    }
}
