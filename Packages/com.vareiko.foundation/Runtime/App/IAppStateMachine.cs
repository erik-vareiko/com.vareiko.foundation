namespace Vareiko.Foundation.App
{
    public interface IAppStateMachine
    {
        AppState Current { get; }
        bool IsIn(AppState state);
        bool TryEnter(AppState next);
        void ForceEnter(AppState next);
    }
}
