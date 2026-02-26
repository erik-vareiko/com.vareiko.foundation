using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Input
{
    public sealed class InputService : IInputService
    {
        private readonly List<IInputAdapter> _adapters;
        private readonly SignalBus _signalBus;

        private InputScheme _preferredScheme = InputScheme.Unknown;
        private InputScheme _activeScheme = InputScheme.Unknown;

        [Inject]
        public InputService([InjectOptional] List<IInputAdapter> adapters = null, [InjectOptional] SignalBus signalBus = null)
        {
            _adapters = adapters ?? new List<IInputAdapter>(0);
            _signalBus = signalBus;
        }

        public InputScheme CurrentScheme
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null ? adapter.Scheme : InputScheme.Unknown;
            }
        }

        public Vector2 Move
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null ? adapter.Move : Vector2.zero;
            }
        }

        public bool DashPressedDown
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null && adapter.DashPressedDown;
            }
        }

        public bool PausePressedDown
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null && adapter.PausePressedDown;
            }
        }

        public bool SubmitPressedDown
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null && adapter.SubmitPressedDown;
            }
        }

        public bool CancelPressedDown
        {
            get
            {
                IInputAdapter adapter = ResolveAdapter();
                return adapter != null && adapter.CancelPressedDown;
            }
        }

        public void SetPreferredScheme(InputScheme scheme)
        {
            _preferredScheme = scheme;
            UpdateActiveScheme();
        }

        private IInputAdapter ResolveAdapter()
        {
            IInputAdapter adapter = ResolvePreferredAdapter();
            if (adapter != null)
            {
                UpdateActiveScheme(adapter.Scheme);
                return adapter;
            }

            adapter = ResolveFirstAvailable();
            if (adapter != null)
            {
                UpdateActiveScheme(adapter.Scheme);
                return adapter;
            }

            UpdateActiveScheme(InputScheme.Unknown);
            return null;
        }

        private IInputAdapter ResolvePreferredAdapter()
        {
            if (_preferredScheme == InputScheme.Unknown)
            {
                return null;
            }

            for (int i = 0; i < _adapters.Count; i++)
            {
                IInputAdapter adapter = _adapters[i];
                if (adapter != null && adapter.IsAvailable && adapter.Scheme == _preferredScheme)
                {
                    return adapter;
                }
            }

            return null;
        }

        private IInputAdapter ResolveFirstAvailable()
        {
            for (int i = 0; i < _adapters.Count; i++)
            {
                IInputAdapter adapter = _adapters[i];
                if (adapter != null && adapter.IsAvailable)
                {
                    return adapter;
                }
            }

            return null;
        }

        private void UpdateActiveScheme()
        {
            ResolveAdapter();
        }

        private void UpdateActiveScheme(InputScheme scheme)
        {
            if (_activeScheme == scheme)
            {
                return;
            }

            _activeScheme = scheme;
            _signalBus?.Fire(new InputSchemeChangedSignal(_activeScheme));
        }
    }
}
