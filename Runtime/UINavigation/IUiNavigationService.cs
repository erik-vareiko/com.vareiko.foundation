namespace Vareiko.Foundation.UINavigation
{
    public interface IUiNavigationService
    {
        bool Push(string screenId);
        bool Replace(string screenId);
        bool Pop();
        void Clear();
        string Current { get; }
    }
}
