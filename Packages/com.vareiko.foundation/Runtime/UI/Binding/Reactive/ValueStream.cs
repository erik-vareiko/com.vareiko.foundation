using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.UI
{
    public sealed class ValueStream<T> : IValueStream<T>
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();

        public bool HasValue { get; private set; }
        public T Value { get; private set; }

        public IDisposable Subscribe(Action<T> listener, bool invokeImmediately = true)
        {
            if (listener == null)
            {
                return EmptyDisposable.Instance;
            }

            _listeners.Add(listener);
            if (invokeImmediately && HasValue)
            {
                listener(Value);
            }

            return new Subscription(_listeners, listener);
        }

        public void SetValue(T value)
        {
            Value = value;
            HasValue = true;

            if (_listeners.Count == 0)
            {
                return;
            }

            Action<T>[] listeners = _listeners.ToArray();
            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i]?.Invoke(value);
            }
        }

        public void Clear()
        {
            Value = default;
            HasValue = false;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<Action<T>> _listeners;
            private Action<T> _listener;

            public Subscription(List<Action<T>> listeners, Action<T> listener)
            {
                _listeners = listeners;
                _listener = listener;
            }

            public void Dispose()
            {
                if (_listener == null)
                {
                    return;
                }

                _listeners.Remove(_listener);
                _listener = null;
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new EmptyDisposable();

            public void Dispose()
            {
            }
        }
    }

    public sealed class NullValueStream<T> : IReadOnlyValueStream<T>
    {
        public static readonly NullValueStream<T> Instance = new NullValueStream<T>();

        public bool HasValue => false;
        public T Value => default;

        public IDisposable Subscribe(Action<T> listener, bool invokeImmediately = true)
        {
            return EmptyDisposable.Instance;
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new EmptyDisposable();

            public void Dispose()
            {
            }
        }
    }
}
