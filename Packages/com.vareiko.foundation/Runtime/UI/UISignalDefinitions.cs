namespace Vareiko.Foundation.UI
{
    public readonly struct UIReadySignal
    {
        public readonly int ElementCount;
        public readonly int ScreenCount;
        public readonly int WindowCount;

        public UIReadySignal(int elementCount, int screenCount, int windowCount)
        {
            ElementCount = elementCount;
            ScreenCount = screenCount;
            WindowCount = windowCount;
        }
    }

    public readonly struct UIElementShownSignal
    {
        public readonly string ElementId;
        public readonly string ElementType;

        public UIElementShownSignal(string elementId, string elementType)
        {
            ElementId = elementId ?? string.Empty;
            ElementType = elementType ?? string.Empty;
        }
    }

    public readonly struct UIElementHiddenSignal
    {
        public readonly string ElementId;
        public readonly string ElementType;

        public UIElementHiddenSignal(string elementId, string elementType)
        {
            ElementId = elementId ?? string.Empty;
            ElementType = elementType ?? string.Empty;
        }
    }

    public readonly struct UIScreenShownSignal
    {
        public readonly string ScreenId;

        public UIScreenShownSignal(string screenId)
        {
            ScreenId = screenId ?? string.Empty;
        }
    }

    public readonly struct UIScreenHiddenSignal
    {
        public readonly string ScreenId;

        public UIScreenHiddenSignal(string screenId)
        {
            ScreenId = screenId ?? string.Empty;
        }
    }
}
