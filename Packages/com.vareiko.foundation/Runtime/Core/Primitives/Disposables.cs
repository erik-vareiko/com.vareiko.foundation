using System;
using System.Collections.Generic;

namespace Vareiko.Foundation
{
    /// <summary>Runs the given action once on Dispose (idempotent).</summary>
    public sealed class DisposableAction : IDisposable
    {
        private Action _onDispose;

        public DisposableAction(Action onDispose)
        {
            if (onDispose == null)
            {
                throw new ArgumentNullException(nameof(onDispose));
            }

            _onDispose = onDispose;
        }

        public void Dispose()
        {
            Action action = _onDispose;
            _onDispose = null;
            action?.Invoke();
        }
    }

    /// <summary>
    /// Owns a set of disposables and disposes them together — the signal-subscription bag
    /// pattern used across foundation services. Adding to a disposed bag disposes the item
    /// immediately.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _items = new List<IDisposable>();
        private bool _disposed;

        public int Count => _items.Count;
        public bool IsDisposed => _disposed;

        public void Add(IDisposable item)
        {
            if (item == null)
            {
                return;
            }

            if (_disposed)
            {
                item.Dispose();
                return;
            }

            _items.Add(item);
        }

        /// <summary>Disposes all held items but keeps the bag usable.</summary>
        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Dispose();
            }

            _items.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Clear();
        }
    }
}
