using System;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.App
{
    public sealed class AppStateMachine : IAppStateMachine, VContainer.Unity.IInitializable, IDisposable
    {
        private readonly IFoundationSignalBus _signalBus;
        private readonly StateMachine<AppState> _stateMachine;
        private IDisposable _bootFailedSubscription;

        public AppStateMachine(IFoundationSignalBus signalBus)
        {
            _signalBus = signalBus;
            _stateMachine = new StateMachine<AppState>(AppState.None, CanTransition);
            _stateMachine.StateChanged += OnStateChanged;
        }

        public AppState Current => _stateMachine.Current;

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
            return _stateMachine.IsIn(state);
        }

        public bool TryEnter(AppState next)
        {
            return _stateMachine.TryEnter(next);
        }

        public void ForceEnter(AppState next)
        {
            _stateMachine.ForceEnter(next);
        }

        private void OnStateChanged(AppState previous, AppState current)
        {
            _signalBus.Publish(new AppStateChangedSignal(previous, current));
        }

        private void OnApplicationBootFailed(ApplicationBootFailedSignal signal)
        {
            if (IsIn(AppState.Shutdown) || IsIn(AppState.Error))
            {
                return;
            }

            ForceEnter(AppState.Error);
        }

        // Default lifecycle rules; host-defined states flow through the permissive branch.
        private static bool CanTransition(AppState current, AppState next)
        {
            if (next.IsNone)
            {
                return false;
            }

            if (current.IsNone)
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
