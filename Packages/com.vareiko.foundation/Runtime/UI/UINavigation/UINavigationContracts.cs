namespace Vareiko.Foundation.UINavigation
{
    public interface IUINavigationService
    {
        bool Push(string screenId);
        bool Replace(string screenId);
        bool Pop();
        void Clear();
        string Current { get; }
    }
}
