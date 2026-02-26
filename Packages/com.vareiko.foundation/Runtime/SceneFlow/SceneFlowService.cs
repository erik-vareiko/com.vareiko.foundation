using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Time;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Vareiko.Foundation.SceneFlow
{
    public sealed class SceneFlowService : ISceneFlowService
    {
        private readonly IFoundationTimeProvider _time;
        private readonly SignalBus _signalBus;

        private bool _isBusy;

        [Inject]
        public SceneFlowService(IFoundationTimeProvider timeProvider, [InjectOptional] SignalBus signalBus = null)
        {
            _time = timeProvider;
            _signalBus = signalBus;
        }

        public bool IsBusy => _isBusy;
        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, bool setActive = true, CancellationToken cancellationToken = default)
        {
            if (_isBusy)
            {
                throw new InvalidOperationException("SceneFlowService is busy.");
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Scene name is null or empty.", nameof(sceneName));
            }

            _isBusy = true;
            float startTime = _time.Time;
            _signalBus?.Fire(new SceneLoadStartedSignal(sceneName, mode));

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
                if (operation == null)
                {
                    throw new InvalidOperationException("Failed to start scene load operation.");
                }

                while (!operation.isDone)
                {
                    float progress = Mathf.Clamp01(operation.progress / 0.9f);
                    _signalBus?.Fire(new SceneLoadProgressSignal(sceneName, progress));
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (setActive)
                {
                    Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                    if (loadedScene.IsValid() && loadedScene.isLoaded)
                    {
                        SceneManager.SetActiveScene(loadedScene);
                    }
                }

                _signalBus?.Fire(new SceneLoadProgressSignal(sceneName, 1f));
                float duration = Mathf.Max(0f, _time.Time - startTime);
                _signalBus?.Fire(new SceneLoadCompletedSignal(sceneName, mode, duration));
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (_isBusy)
            {
                throw new InvalidOperationException("SceneFlowService is busy.");
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Scene name is null or empty.", nameof(sceneName));
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            _isBusy = true;
            float startTime = _time.Time;
            _signalBus?.Fire(new SceneUnloadStartedSignal(sceneName));

            try
            {
                AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
                if (operation == null)
                {
                    throw new InvalidOperationException("Failed to start scene unload operation.");
                }

                while (!operation.isDone)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                float duration = Mathf.Max(0f, _time.Time - startTime);
                _signalBus?.Fire(new SceneUnloadCompletedSignal(sceneName, duration));
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
