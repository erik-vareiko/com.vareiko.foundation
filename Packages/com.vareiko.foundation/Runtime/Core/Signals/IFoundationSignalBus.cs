using System;

namespace Vareiko.Foundation.Signals
{
    /// <summary>
    /// Foundation messaging facade. Replaces the Zenject <c>SignalBus</c> with the same
    /// publish/subscribe ergonomics so the ~42 service call sites stay a one-line change.
    /// Backed by MessagePipe (see <see cref="MessagePipeSignalBus"/>).
    /// </summary>
    public interface IFoundationSignalBus
    {
        /// <summary>Publishes a message to all subscribers of <typeparamref name="T"/>.</summary>
        void Publish<T>(T message);

        /// <summary>Subscribes to messages of <typeparamref name="T"/>; dispose to unsubscribe.</summary>
        IDisposable Subscribe<T>(Action<T> handler);
    }
}
