using System;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Signals;
using Zenject;

namespace Vareiko.Foundation.App
{
    public sealed class AppStateMachine : IAppStateMachine, VContainer.Unity.IInitializable, IDisposable
    {
        private readonly IFoundationSignalBus _signalBus;
        private AppState _current = AppState.None;
        private IDisposable _bootFailedSubscription;

        [Inject]
        public AppStateMachine(IFoundationSignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public AppState Current => _current;

        public void Initialize()
        {
            _bootFailedSubscription = _signalBus.Subscribe<ApplicationBootFailedSignal>(OnApplicationBootFailed);
            ForceEnter(AppState.Boot);
        }

        public void Dispose()
        {
            _bootFailedSubscription?.Dispose();
            _bootFailedSubscription = null;
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
            _signalBus.Publish(new AppStateChangedSignal(previous, _current));
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
