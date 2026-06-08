using System;
using MessagePipe;

namespace Vareiko.Foundation.Signals
{
    /// <summary>
    /// MessagePipe-backed <see cref="IFoundationSignalBus"/>. Resolves typed publishers and
    /// subscribers through <see cref="GlobalMessagePipe"/>, whose provider is set once during
    /// root scope build (<c>RegisterBuildCallback(c =&gt; GlobalMessagePipe.SetProvider(...))</c>).
    /// A message type must have a broker registered (<c>RegisterMessageBroker&lt;T&gt;</c>) — an
    /// unregistered type fails fast, which is intentional.
    /// </summary>
    public sealed class MessagePipeSignalBus : IFoundationSignalBus
    {
        public void Publish<T>(T message)
        {
            GlobalMessagePipe.GetPublisher<T>().Publish(message);
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            return GlobalMessagePipe.GetSubscriber<T>().Subscribe(handler);
        }
    }
}
