using System;
using UnityEngine;

namespace Vareiko.Foundation.App
{
    public sealed class UnityApplicationLifecycleSource : IApplicationLifecycleSource
    {
        private ApplicationLifecycleHook _hook;

        public event Action<bool> PauseChanged;
        public event Action<bool> FocusChanged;
        public event Action QuitRequested;

        public void Initialize()
        {
            _hook = ApplicationLifecycleHook.EnsureExists();
            _hook.PauseChanged += OnPauseChanged;
            _hook.FocusChanged += OnFocusChanged;
            _hook.QuitRequested += OnQuitRequested;
        }

        public void Dispose()
        {
            if (_hook == null)
            {
                return;
            }

            _hook.PauseChanged -= OnPauseChanged;
            _hook.FocusChanged -= OnFocusChanged;
            _hook.QuitRequested -= OnQuitRequested;
            _hook = null;
        }

        private void OnPauseChanged(bool isPaused)
        {
            PauseChanged?.Invoke(isPaused);
        }

        private void OnFocusChanged(bool hasFocus)
        {
            FocusChanged?.Invoke(hasFocus);
        }

        private void OnQuitRequested()
        {
            QuitRequested?.Invoke();
        }

        private sealed class ApplicationLifecycleHook : MonoBehaviour
        {
            private static ApplicationLifecycleHook _instance;

            public event Action<bool> PauseChanged;
            public event Action<bool> FocusChanged;
            public event Action QuitRequested;

            public static ApplicationLifecycleHook EnsureExists()
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject host = new GameObject("[Foundation] ApplicationLifecycleHook");
                UnityEngine.Object.DontDestroyOnLoad(host);
                _instance = host.AddComponent<ApplicationLifecycleHook>();
                return _instance;
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                PauseChanged?.Invoke(pauseStatus);
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                FocusChanged?.Invoke(hasFocus);
            }

            private void OnApplicationQuit()
            {
                QuitRequested?.Invoke();
            }
        }
    }
}
