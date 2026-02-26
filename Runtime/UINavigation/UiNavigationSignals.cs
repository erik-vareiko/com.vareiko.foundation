namespace Vareiko.Foundation.UINavigation
{
    public readonly struct UiNavigationChangedSignal
    {
        public readonly string Current;
        public readonly int Depth;

        public UiNavigationChangedSignal(string current, int depth)
        {
            Current = current;
            Depth = depth;
        }
    }
}
