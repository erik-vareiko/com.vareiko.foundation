using Vareiko.Foundation.Bootstrap;
using Zenject;

namespace Vareiko.Foundation.App
{
    public sealed class AppStateMachine : IAppStateMachine, IInitializable, System.IDisposable
    {
        private readonly SignalBus _signalBus;
        private AppState _current = AppState.None;

        [Inject]
        public AppStateMachine([InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        public AppState Current => _current;

        public void Initialize()
        {
            _signalBus?.Subscribe<ApplicationBootFailedSignal>(OnApplicationBootFailed);
            ForceEnter(AppState.Boot);
        }

        public void Dispose()
        {
            _signalBus?.Unsubscribe<ApplicationBootFailedSignal>(OnApplicationBootFailed);
        }

        public bool IsIn(AppState state)
        {
            return _current == state;
        }

        public bool TryEnter(AppState next)
        {
            if (next == _current)
            {
                return false;
            }

            if (!CanTransition(_current, next))
            {
                return false;
            }

            ForceEnter(next);
            return true;
        }

        public void ForceEnter(AppState next)
        {
            AppState previous = _current;
            _current = next;
            _signalBus?.Fire(new AppStateChangedSignal(previous, _current));
        }

        private void OnApplicationBootFailed(ApplicationBootFailedSignal signal)
        {
            if (_current == AppState.Shutdown || _current == AppState.Error)
            {
                return;
            }

            ForceEnter(AppState.Error);
        }

        private static bool CanTransition(AppState current, AppState next)
        {
            if (next == AppState.None)
            {
                return false;
            }

            if (current == AppState.None)
            {
                return next == AppState.Boot;
            }

            if (current == AppState.Shutdown)
            {
                return false;
            }

            return true;
        }
    }
}
