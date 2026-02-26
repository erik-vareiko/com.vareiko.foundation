namespace Vareiko.Foundation.UI
{
    public readonly struct UiReadySignal
    {
        public readonly int ScreenCount;

        public UiReadySignal(int screenCount)
        {
            ScreenCount = screenCount;
        }
    }

    public readonly struct UiScreenShownSignal
    {
        public readonly string ScreenId;

        public UiScreenShownSignal(string screenId)
        {
            ScreenId = screenId;
        }
    }

    public readonly struct UiScreenHiddenSignal
    {
        public readonly string ScreenId;

        public UiScreenHiddenSignal(string screenId)
        {
            ScreenId = screenId;
        }
    }
}
