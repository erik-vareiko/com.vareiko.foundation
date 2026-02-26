using System;

namespace Vareiko.Foundation.App
{
    public interface IApplicationLifecycleService
    {
        bool IsPaused { get; }
        bool HasFocus { get; }
        bool IsQuitting { get; }

        event Action<bool> PauseChanged;
        event Action<bool> FocusChanged;
        event Action QuitRequested;
    }
}
