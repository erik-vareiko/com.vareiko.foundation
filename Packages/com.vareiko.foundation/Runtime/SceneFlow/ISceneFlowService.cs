using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Vareiko.Foundation.SceneFlow
{
    public interface ISceneFlowService
    {
        bool IsBusy { get; }
        string ActiveSceneName { get; }
        UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, bool setActive = true, CancellationToken cancellationToken = default);
        UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
