using UnityEngine.SceneManagement;

namespace Vareiko.Foundation.SceneFlow
{
    public readonly struct SceneLoadStartedSignal
    {
        public readonly string SceneName;
        public readonly LoadSceneMode Mode;

        public SceneLoadStartedSignal(string sceneName, LoadSceneMode mode)
        {
            SceneName = sceneName;
            Mode = mode;
        }
    }

    public readonly struct SceneLoadProgressSignal
    {
        public readonly string SceneName;
        public readonly float Progress;

        public SceneLoadProgressSignal(string sceneName, float progress)
        {
            SceneName = sceneName;
            Progress = progress;
        }
    }

    public readonly struct SceneLoadCompletedSignal
    {
        public readonly string SceneName;
        public readonly LoadSceneMode Mode;
        public readonly float DurationSeconds;

        public SceneLoadCompletedSignal(string sceneName, LoadSceneMode mode, float durationSeconds)
        {
            SceneName = sceneName;
            Mode = mode;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct SceneUnloadStartedSignal
    {
        public readonly string SceneName;

        public SceneUnloadStartedSignal(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    public readonly struct SceneUnloadCompletedSignal
    {
        public readonly string SceneName;
        public readonly float DurationSeconds;

        public SceneUnloadCompletedSignal(string sceneName, float durationSeconds)
        {
            SceneName = sceneName;
            DurationSeconds = durationSeconds;
        }
    }
}
