namespace Vareiko.Foundation.UI
{
    public interface IUIService
    {
        bool Show(string elementId, bool instant = true);
        bool Hide(string elementId, bool instant = true);
        bool Toggle(string elementId, bool instant = true);
        void HideAll(bool instant = true);
        bool TryGetElement(string elementId, out UIElement element);
        bool TryGetScreen(string screenId, out UIScreen screen);
        bool TryGetWindow(string windowId, out UIWindow window);
    }
}
