using System;

namespace Vareiko.Foundation.UI
{
    [Obsolete("Use UIReadySignal instead.")]
    public readonly struct UiReadySignal
    {
        public readonly int ScreenCount;

        public UiReadySignal(int screenCount)
        {
            ScreenCount = screenCount;
        }
    }

    [Obsolete("Use UIScreenShownSignal instead.")]
    public readonly struct UiScreenShownSignal
    {
        public readonly string ScreenId;

        public UiScreenShownSignal(string screenId)
        {
            ScreenId = screenId ?? string.Empty;
        }
    }

    [Obsolete("Use UIScreenHiddenSignal instead.")]
    public readonly struct UiScreenHiddenSignal
    {
        public readonly string ScreenId;

        public UiScreenHiddenSignal(string screenId)
        {
            ScreenId = screenId ?? string.Empty;
        }
    }
}
