using System;
using System.Collections.Generic;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    /// <summary>
    /// In-memory <see cref="IFoundationSignalBus"/> for unit tests: a functional, DI-agnostic
    /// publish/subscribe bus with no Zenject or MessagePipe dependency, so service tests survive
    /// the DI migration unchanged.
    /// </summary>
    public sealed class FakeSignalBus : IFoundationSignalBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T message)
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                return;
            }

            // Snapshot so handlers may unsubscribe during dispatch.
            Delegate[] snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                ((Action<T>)snapshot[i]).Invoke(message);
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }

            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
