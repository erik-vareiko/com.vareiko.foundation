using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Time
{
    public sealed class TickService : ITickService, VContainer.Unity.ITickable, IDisposable
    {
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly List<TickListener> _listeners = new List<TickListener>();
        private readonly List<TickListener> _listenerBuffer = new List<TickListener>();
        private readonly List<TimerEntry> _timers = new List<TimerEntry>();
        private readonly List<TimerEntry> _timerBuffer = new List<TimerEntry>();

        private long _registrationSequence;
        private bool _listenersDirty;
        private bool _disposed;

        public TickService(IFoundationTimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public bool IsPaused { get; set; }

        void VContainer.Unity.ITickable.Tick()
        {
            Advance(_timeProvider.DeltaTime, _timeProvider.UnscaledDeltaTime);
        }

        public IDisposable RegisterTick(Action<float> onTick, int order = 0)
        {
            if (onTick == null)
            {
                throw new ArgumentNullException(nameof(onTick));
            }

            TickListener listener = new TickListener(this, onTick, order, _registrationSequence++);
            _listeners.Add(listener);
            _listenersDirty = true;
            return listener;
        }

        public IDisposable Delay(float delaySeconds, Action callback, bool useUnscaledTime = false)
        {
            return AddTimer(delaySeconds, callback, repeat: false, useUnscaledTime);
        }

        public IDisposable Repeat(float intervalSeconds, Action callback, bool useUnscaledTime = false)
        {
            if (intervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Repeat interval must be positive.");
            }

            return AddTimer(intervalSeconds, callback, repeat: true, useUnscaledTime);
        }

        public IDisposable NextFrame(Action callback)
        {
            return AddTimer(0f, callback, repeat: false, useUnscaledTime: true);
        }

        /// <summary>
        /// Drives one tick manually. Play mode calls this through the container's player-loop;
        /// EditMode tests and custom loops call it directly.
        /// </summary>
        public void Advance(float deltaTime, float unscaledDeltaTime)
        {
            if (_disposed || IsPaused)
            {
                return;
            }

            TickListeners(deltaTime);
            TickTimers(deltaTime, unscaledDeltaTime);
        }

        public void Dispose()
        {
            _disposed = true;
            _listeners.Clear();
            _timers.Clear();
        }

        private void TickListeners(float deltaTime)
        {
            if (_listenersDirty)
            {
                _listeners.Sort();
                _listenersDirty = false;
            }

            _listenerBuffer.Clear();
            _listenerBuffer.AddRange(_listeners);
            for (int i = 0; i < _listenerBuffer.Count; i++)
            {
                TickListener listener = _listenerBuffer[i];
                if (listener.IsRemoved)
                {
                    continue;
                }

                try
                {
                    listener.Invoke(deltaTime);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            _listeners.RemoveAll(static l => l.IsRemoved);
        }

        private void TickTimers(float deltaTime, float unscaledDeltaTime)
        {
            _timerBuffer.Clear();
            _timerBuffer.AddRange(_timers);
            for (int i = 0; i < _timerBuffer.Count; i++)
            {
                TimerEntry timer = _timerBuffer[i];
                if (timer.IsRemoved)
                {
                    continue;
                }

                timer.Accumulate(timer.UseUnscaledTime ? unscaledDeltaTime : deltaTime);
                while (!timer.IsRemoved && timer.IsDue)
                {
                    try
                    {
                        timer.Fire();
                    }
                    catch (Exception exception)
                    {
                        UnityEngine.Debug.LogException(exception);
                    }

                    if (!timer.Repeats)
                    {
                        timer.Dispose();
                    }
                }
            }

            _timers.RemoveAll(static t => t.IsRemoved);
        }

        private TimerEntry AddTimer(float seconds, Action callback, bool repeat, bool useUnscaledTime)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            TimerEntry timer = new TimerEntry(seconds, callback, repeat, useUnscaledTime);
            _timers.Add(timer);
            return timer;
        }

        private sealed class TickListener : IDisposable, IComparable<TickListener>
        {
            private readonly TickService _owner;
            private readonly Action<float> _onTick;
            private readonly int _order;
            private readonly long _sequence;

            public TickListener(TickService owner, Action<float> onTick, int order, long sequence)
            {
                _owner = owner;
                _onTick = onTick;
                _order = order;
                _sequence = sequence;
            }

            public bool IsRemoved { get; private set; }

            public void Invoke(float deltaTime)
            {
                _onTick(deltaTime);
            }

            public int CompareTo(TickListener other)
            {
                int byOrder = _order.CompareTo(other._order);
                return byOrder != 0 ? byOrder : _sequence.CompareTo(other._sequence);
            }

            public void Dispose()
            {
                IsRemoved = true;
            }
        }

        private sealed class TimerEntry : IDisposable
        {
            private readonly float _interval;
            private readonly Action _callback;
            private float _remaining;

            public TimerEntry(float seconds, Action callback, bool repeats, bool useUnscaledTime)
            {
                _interval = seconds;
                _callback = callback;
                _remaining = seconds;
                Repeats = repeats;
                UseUnscaledTime = useUnscaledTime;
            }

            public bool Repeats { get; }
            public bool UseUnscaledTime { get; }
            public bool IsRemoved { get; private set; }
            public bool IsDue => _remaining <= 0f;

            public void Accumulate(float deltaTime)
            {
                _remaining -= deltaTime;
            }

            public void Fire()
            {
                if (Repeats)
                {
                    _remaining += _interval;
                }

                _callback();
            }

            public void Dispose()
            {
                IsRemoved = true;
            }
        }
    }
}
