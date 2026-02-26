namespace Vareiko.Foundation.Consent
{
    public readonly struct ConsentLoadedSignal
    {
        public readonly bool IsCollected;

        public ConsentLoadedSignal(bool isCollected)
        {
            IsCollected = isCollected;
        }
    }

    public readonly struct ConsentChangedSignal
    {
        public readonly ConsentScope Scope;
        public readonly bool Granted;
        public readonly bool IsCollected;

        public ConsentChangedSignal(ConsentScope scope, bool granted, bool isCollected)
        {
            Scope = scope;
            Granted = granted;
            IsCollected = isCollected;
        }
    }
}
