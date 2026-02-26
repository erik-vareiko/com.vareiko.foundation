namespace Vareiko.Foundation.App
{
    public readonly struct AppStateChangedSignal
    {
        public readonly AppState Previous;
        public readonly AppState Current;

        public AppStateChangedSignal(AppState previous, AppState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
