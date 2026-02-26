using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.Bootstrap;
using Zenject;

namespace Vareiko.Foundation.Features
{
    public sealed class RefreshFeatureFlagsBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        [SerializeField] private int _order = 50;
        [SerializeField] private string _taskName = "RefreshFeatureFlags";

        private IFeatureFlagService _featureFlagService;

        [Inject]
        public void Construct(IFeatureFlagService featureFlagService)
        {
            _featureFlagService = featureFlagService;
        }

        public int Order => _order;
        public string Name => string.IsNullOrWhiteSpace(_taskName) ? nameof(RefreshFeatureFlagsBootstrapTask) : _taskName;

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_featureFlagService == null)
            {
                return UniTask.CompletedTask;
            }

            return _featureFlagService.RefreshAsync(cancellationToken);
        }
    }
}
