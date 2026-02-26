using System;

namespace Vareiko.Foundation.App
{
    public interface IApplicationLifecycleSource : IDisposable
    {
        event Action<bool> PauseChanged;
        event Action<bool> FocusChanged;
        event Action QuitRequested;

        void Initialize();
    }
}
