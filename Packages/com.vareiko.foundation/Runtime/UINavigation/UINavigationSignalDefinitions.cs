namespace Vareiko.Foundation.UINavigation
{
    public readonly struct UINavigationChangedSignal
    {
        public readonly string Current;
        public readonly int Depth;

        public UINavigationChangedSignal(string current, int depth)
        {
            Current = current ?? string.Empty;
            Depth = depth;
        }
    }
}
