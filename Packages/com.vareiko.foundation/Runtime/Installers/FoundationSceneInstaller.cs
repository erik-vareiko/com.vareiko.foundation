using System.Collections.Generic;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Config;
using Vareiko.Foundation.UINavigation;
using Vareiko.Foundation.UI;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Installers
{
    public sealed class FoundationSceneInstaller : MonoInstaller
    {
        [SerializeField] private UIRegistry _uiRegistry;
        [SerializeField] private MonoBehaviour[] _bootstrapTasks;
        [SerializeField] private ConfigRegistry[] _configRegistries;

        public override void InstallBindings()
        {
            FoundationBootstrapInstaller.Install(Container);
            FoundationUIInstaller.Install(Container);
            FoundationUINavigationInstaller.Install(Container);

            if (_uiRegistry != null)
            {
                Container.BindInstance(_uiRegistry).IfNotBound();
            }

            BindBootstrapTasks();
        }

        private void BindBootstrapTasks()
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

                    Container.Bind<IBootstrapTask>().FromInstance(task);
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

                    Container.Bind<IBootstrapTask>().FromInstance(task);
                }
            }
        }
    }
}
