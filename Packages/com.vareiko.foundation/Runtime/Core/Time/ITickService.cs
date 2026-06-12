using System;

namespace Vareiko.Foundation.Time
{
    /// <summary>
    /// Central ordered update loop with timers and deferred calls — the coroutine/per-service
    /// UniTask-loop replacement. Driven by the container's player-loop in play mode; tests and
    /// custom loops drive it manually through <see cref="TickService.Advance"/>.
    /// </summary>
    public interface ITickService
    {
        /// <summary>While paused, listeners are not invoked and timers do not accumulate.</summary>
        bool IsPaused { get; set; }

        /// <summary>
        /// Registers a per-frame callback receiving the (scaled) delta time. Lower
        /// <paramref name="order"/> runs first; equal orders run in registration order.
        /// Dispose the handle to unregister (safe during a tick).
        /// </summary>
        IDisposable RegisterTick(Action<float> onTick, int order = 0);

        /// <summary>Invokes <paramref name="callback"/> once after the given delay. Dispose to cancel.</summary>
        IDisposable Delay(float delaySeconds, Action callback, bool useUnscaledTime = false);

        /// <summary>Invokes <paramref name="callback"/> every interval until disposed.</summary>
        IDisposable Repeat(float intervalSeconds, Action callback, bool useUnscaledTime = false);

        /// <summary>Invokes <paramref name="callback"/> once on the next tick. Dispose to cancel.</summary>
        IDisposable NextFrame(Action callback);
    }
}
