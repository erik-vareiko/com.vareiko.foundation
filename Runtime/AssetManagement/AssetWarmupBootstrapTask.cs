using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Bootstrap;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.AssetManagement
{
    public sealed class AssetWarmupBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        [SerializeField] private int _order;
        [SerializeField] private string _taskName = "AssetWarmup";
        [SerializeField] private List<string> _keys = new List<string>();

        private IAssetService _assetService;

        [Inject]
        public void Construct(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public int Order => _order;
        public string Name => string.IsNullOrWhiteSpace(_taskName) ? nameof(AssetWarmupBootstrapTask) : _taskName;

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_assetService == null)
            {
                return UniTask.CompletedTask;
            }

            return _assetService.WarmupAsync(_keys, cancellationToken);
        }
    }
}
