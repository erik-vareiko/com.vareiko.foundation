using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Bootstrap;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Config
{
    public sealed class ConfigRegistry : MonoBehaviour, IBootstrapTask
    {
        private static readonly MethodInfo RegisterMethod = typeof(IConfigService).GetMethod(nameof(IConfigService.Register));

        [Serializable]
        private struct Entry
        {
            public string Id;
            public ScriptableObject Config;
        }

        [SerializeField] private int _order;
        [SerializeField] private string _taskName = "ConfigRegistry";
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private IConfigService _configService;

        [Inject]
        public void Construct(IConfigService configService)
        {
            _configService = configService;
        }

        public int Order => _order;
        public string Name => string.IsNullOrWhiteSpace(_taskName) ? nameof(ConfigRegistry) : _taskName;

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_configService == null || _entries == null)
            {
                return UniTask.CompletedTask;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Entry entry = _entries[i];
                if (entry.Config == null)
                {
                    continue;
                }

                Register(entry.Config, entry.Id);
            }

            return UniTask.CompletedTask;
        }

        private void Register(ScriptableObject config, string id)
        {
            if (RegisterMethod == null)
            {
                return;
            }

            Type type = config.GetType();
            MethodInfo method = RegisterMethod.MakeGenericMethod(type);
            object[] args = { config, string.IsNullOrWhiteSpace(id) ? "default" : id };
            method.Invoke(_configService, args);
        }
    }
}
