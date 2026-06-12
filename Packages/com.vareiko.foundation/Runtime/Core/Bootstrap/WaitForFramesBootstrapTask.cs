using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vareiko.Foundation.Bootstrap
{
    public sealed class WaitForFramesBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        [SerializeField] private int _order;
        [SerializeField] private string _taskName = "WaitForFrames";
        [SerializeField] private int _frameCount = 1;

        public int Order => _order;
        public string Name => string.IsNullOrWhiteSpace(_taskName) ? nameof(WaitForFramesBootstrapTask) : _taskName;

        public async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            int frameCount = Mathf.Max(1, _frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
