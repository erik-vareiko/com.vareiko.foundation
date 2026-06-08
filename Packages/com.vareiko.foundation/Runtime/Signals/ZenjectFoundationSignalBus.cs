using System;
using Zenject;

namespace Vareiko.Foundation.Signals
{
    /// <summary>
    /// Transitional <see cref="IFoundationSignalBus"/> backed by Zenject's <see cref="SignalBus"/>.
    /// Lets services depend on the facade while the project still composes through Zenject;
    /// it is replaced by <see cref="MessagePipeSignalBus"/> at the VContainer cutover.
    /// </summary>
    public sealed class ZenjectFoundationSignalBus : IFoundationSignalBus
    {
        private readonly SignalBus _signalBus;

        public ZenjectFoundationSignalBus(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void Publish<T>(T message)
        {
            _signalBus.Fire(message);
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            _signalBus.Subscribe(handler);
            return new Subscription<T>(_signalBus, handler);
        }

        private sealed class Subscription<T> : IDisposable
        {
            private SignalBus _signalBus;
            private Action<T> _handler;

            public Subscription(SignalBus signalBus, Action<T> handler)
            {
                _signalBus = signalBus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_signalBus == null)
                {
                    return;
                }

                _signalBus.Unsubscribe(_handler);
                _signalBus = null;
                _handler = null;
            }
        }
    }
}
