namespace Vareiko.Foundation.Input
{
    public readonly struct InputSchemeChangedSignal
    {
        public readonly InputScheme Scheme;

        public InputSchemeChangedSignal(InputScheme scheme)
        {
            Scheme = scheme;
        }
    }
}
