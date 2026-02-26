namespace Vareiko.Foundation.UI
{
    public interface IUiService
    {
        bool Show(string screenId, bool instant = true);
        bool Hide(string screenId, bool instant = true);
        bool Toggle(string screenId, bool instant = true);
        void HideAll(bool instant = true);
        bool TryGet(string screenId, out UIScreen screen);
    }
}
