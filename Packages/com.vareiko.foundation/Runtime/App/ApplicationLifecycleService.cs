using System;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.App
{
    public sealed class ApplicationLifecycleService : IApplicationLifecycleService, VContainer.Unity.IInitializable, IDisposable
    {
        private readonly IFoundationSignalBus _signalBus;
        private readonly IApplicationLifecycleSource _source;
        private readonly bool _ownsSource;

        public event Action<bool> PauseChanged;
        public event Action<bool> FocusChanged;
        public event Action QuitRequested;

        public ApplicationLifecycleService(
            IFoundationSignalBus signalBus,
            IApplicationLifecycleSource source = null)
        {
            _signalBus = signalBus;
            _source = source ?? new UnityApplicationLifecycleSource();
            _ownsSource = source == null;
        }

        public bool IsPaused { get; private set; }
        public bool HasFocus { get; private set; } = true;
        public bool IsQuitting { get; private set; }

        public void Initialize()
        {
            _source.PauseChanged += OnPauseChanged;
            _source.FocusChanged += OnFocusChanged;
            _source.QuitRequested += OnQuitRequested;
            _source.Initialize();
        }

        public void Dispose()
        {
            _source.PauseChanged -= OnPauseChanged;
            _source.FocusChanged -= OnFocusChanged;
            _source.QuitRequested -= OnQuitRequested;

            if (_ownsSource)
            {
                _source.Dispose();
            }
        }

        private void OnPauseChanged(bool isPaused)
        {
            if (IsPaused == isPaused)
            {
                return;
            }

            IsPaused = isPaused;
            PauseChanged?.Invoke(isPaused);
            _signalBus.Publish(new ApplicationPauseChangedSignal(isPaused));
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (HasFocus == hasFocus)
            {
                return;
            }

            HasFocus = hasFocus;
            FocusChanged?.Invoke(hasFocus);
            _signalBus.Publish(new ApplicationFocusChangedSignal(hasFocus));
        }

        private void OnQuitRequested()
        {
            if (IsQuitting)
            {
                return;
            }

            IsQuitting = true;
            QuitRequested?.Invoke();
            _signalBus.Publish(new ApplicationQuitSignal());
        }
    }
}
