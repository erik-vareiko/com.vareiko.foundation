using System;
using System.Collections.Generic;
using Vareiko.Foundation.SceneFlow;
using Vareiko.Foundation.Signals;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Loading
{
    public sealed class LoadingService : ILoadingService, IInitializable, System.IDisposable
    {
        private readonly IFoundationSignalBus _signalBus;
        private readonly List<IDisposable> _signalSubscriptions = new List<IDisposable>();

        private bool _manualMode;
        private bool _isLoading;
        private float _progress;
        private string _activeOperation;

        [Inject]
        public LoadingService([InjectOptional] IFoundationSignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        public bool IsLoading => _isLoading;
        public float Progress => _progress;
        public string ActiveOperation => _activeOperation;

        public void Initialize()
        {
            if (_signalBus == null)
            {
                return;
            }

            _signalSubscriptions.Add(_signalBus.Subscribe<SceneLoadStartedSignal>(OnSceneLoadStarted));
            _signalSubscriptions.Add(_signalBus.Subscribe<SceneLoadProgressSignal>(OnSceneLoadProgress));
            _signalSubscriptions.Add(_signalBus.Subscribe<SceneLoadCompletedSignal>(OnSceneLoadCompleted));
            _signalSubscriptions.Add(_signalBus.Subscribe<SceneUnloadStartedSignal>(OnSceneUnloadStarted));
            _signalSubscriptions.Add(_signalBus.Subscribe<SceneUnloadCompletedSignal>(OnSceneUnloadCompleted));
        }

        public void Dispose()
        {
            if (_signalBus == null)
            {
                return;
            }

            for (int i = 0; i < _signalSubscriptions.Count; i++)
            {
                _signalSubscriptions[i].Dispose();
            }
            _signalSubscriptions.Clear();
        }

        public void BeginManual(string operationName)
        {
            _manualMode = true;
            ApplyState(true, 0f, string.IsNullOrWhiteSpace(operationName) ? "ManualOperation" : operationName);
        }

        public void SetManualProgress(float progress)
        {
            if (!_manualMode)
            {
                return;
            }

            ApplyState(true, Mathf.Clamp01(progress), _activeOperation);
        }

        public void CompleteManual()
        {
            if (!_manualMode)
            {
                return;
            }

            _manualMode = false;
            ApplyState(false, 1f, string.Empty);
        }

        private void OnSceneLoadStarted(SceneLoadStartedSignal signal)
        {
            if (_manualMode)
            {
                return;
            }

            ApplyState(true, 0f, signal.SceneName);
        }

        private void OnSceneLoadProgress(SceneLoadProgressSignal signal)
        {
            if (_manualMode)
            {
                return;
            }

            ApplyState(true, Mathf.Clamp01(signal.Progress), signal.SceneName);
        }

        private void OnSceneLoadCompleted(SceneLoadCompletedSignal signal)
        {
            if (_manualMode)
            {
                return;
            }

            ApplyState(false, 1f, signal.SceneName);
        }

        private void OnSceneUnloadStarted(SceneUnloadStartedSignal signal)
        {
            if (_manualMode)
            {
                return;
            }

            ApplyState(true, 0f, signal.SceneName);
        }

        private void OnSceneUnloadCompleted(SceneUnloadCompletedSignal signal)
        {
            if (_manualMode)
            {
                return;
            }

            ApplyState(false, 1f, signal.SceneName);
        }

        private void ApplyState(bool isLoading, float progress, string operationName)
        {
            bool changed = _isLoading != isLoading ||
                           !Mathf.Approximately(_progress, progress) ||
                           !string.Equals(_activeOperation, operationName, System.StringComparison.Ordinal);

            _isLoading = isLoading;
            _progress = Mathf.Clamp01(progress);
            _activeOperation = operationName ?? string.Empty;

            if (changed)
            {
                _signalBus?.Publish(new LoadingStateChangedSignal(_isLoading, _progress, _activeOperation));
            }
        }
    }
}
