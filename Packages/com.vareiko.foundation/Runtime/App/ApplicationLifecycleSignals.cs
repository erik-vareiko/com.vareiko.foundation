namespace Vareiko.Foundation.App
{
    public readonly struct ApplicationPauseChangedSignal
    {
        public readonly bool IsPaused;

        public ApplicationPauseChangedSignal(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    public readonly struct ApplicationFocusChangedSignal
    {
        public readonly bool HasFocus;

        public ApplicationFocusChangedSignal(bool hasFocus)
        {
            HasFocus = hasFocus;
        }
    }

    public readonly struct ApplicationQuitSignal
    {
    }
}
