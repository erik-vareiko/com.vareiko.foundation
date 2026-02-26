using System;

namespace Vareiko.Foundation.UI
{
    public interface IReadOnlyValueStream<T>
    {
        bool HasValue { get; }
        T Value { get; }
        IDisposable Subscribe(Action<T> listener, bool invokeImmediately = true);
    }
}
