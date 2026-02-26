using System;

namespace Vareiko.Foundation.UI
{
    public interface IComputedValueStream<T> : IReadOnlyValueStream<T>, IDisposable
    {
    }

    public sealed class ComputedValueStream<T> : IComputedValueStream<T>
    {
        private readonly ValueStream<T> _inner = new ValueStream<T>();
        private IDisposable _subscription;

        public ComputedValueStream(IDisposable subscription)
        {
            _subscription = subscription;
        }

        public bool HasValue => _inner.HasValue;
        public T Value => _inner.Value;

        public IDisposable Subscribe(Action<T> listener, bool invokeImmediately = true)
        {
            return _inner.Subscribe(listener, invokeImmediately);
        }

        internal void SetValue(T value)
        {
            _inner.SetValue(value);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
