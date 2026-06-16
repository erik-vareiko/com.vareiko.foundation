using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Vareiko.Foundation.Time;

namespace Vareiko.Foundation.Tests.Time
{
    public sealed class TickServiceTests
    {
        private sealed class FakeTimeProvider : IFoundationTimeProvider
        {
            public float Time => 0f;
            public float DeltaTime => 0f;
            public float UnscaledTime => 0f;
            public float UnscaledDeltaTime => 0f;
        }

        private static TickService CreateService()
        {
            return new TickService(new FakeTimeProvider());
        }

        [Test]
        public void RegisterTick_InvokesListenersInOrder_ThenRegistrationOrder()
        {
            TickService service = CreateService();
            List<string> calls = new List<string>();
            service.RegisterTick(_ => calls.Add("late"), 10);
            service.RegisterTick(_ => calls.Add("early"), -10);
            service.RegisterTick(_ => calls.Add("default-a"));
            service.RegisterTick(_ => calls.Add("default-b"));

            service.Advance(0.1f, 0.1f);

            Assert.That(calls, Is.EqualTo(new[] { "early", "default-a", "default-b", "late" }));
        }

        [Test]
        public void RegisterTick_PassesDeltaTime_AndDisposeUnregisters()
        {
            TickService service = CreateService();
            float received = -1f;
            int count = 0;
            IDisposable handle = service.RegisterTick(dt =>
            {
                received = dt;
                count++;
            });

            service.Advance(0.25f, 0.25f);
            handle.Dispose();
            service.Advance(0.25f, 0.25f);

            Assert.That(received, Is.EqualTo(0.25f));
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void RegisterTick_UnregisteringDuringTick_IsSafe()
        {
            TickService service = CreateService();
            int laterCalls = 0;
            IDisposable victim = null;
            service.RegisterTick(_ => victim.Dispose(), -1);
            victim = service.RegisterTick(_ => laterCalls++);

            service.Advance(0.1f, 0.1f);
            service.Advance(0.1f, 0.1f);

            Assert.That(laterCalls, Is.Zero, "A listener disposed earlier in the same tick must not fire.");
        }

        [Test]
        public void RegisterTick_RegisteringDuringTick_FiresNextTick()
        {
            TickService service = CreateService();
            int innerCalls = 0;
            bool registered = false;
            service.RegisterTick(_ =>
            {
                if (!registered)
                {
                    registered = true;
                    service.RegisterTick(_ => innerCalls++);
                }
            });

            service.Advance(0.1f, 0.1f);
            Assert.That(innerCalls, Is.Zero);
            service.Advance(0.1f, 0.1f);
            Assert.That(innerCalls, Is.EqualTo(1));
        }

        [Test]
        public void ListenerException_DoesNotBreakOtherListeners()
        {
            TickService service = CreateService();
            int calls = 0;
            service.RegisterTick(_ => throw new InvalidOperationException("listener boom"), -1);
            service.RegisterTick(_ => calls++);

            LogAssert.ignoreFailingMessages = true;
            try
            {
                service.Advance(0.1f, 0.1f);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Delay_FiresOnceAfterAccumulatedTime()
        {
            TickService service = CreateService();
            int fired = 0;
            service.Delay(1f, () => fired++);

            service.Advance(0.5f, 0.5f);
            Assert.That(fired, Is.Zero);
            service.Advance(0.5f, 0.5f);
            Assert.That(fired, Is.EqualTo(1));
            service.Advance(5f, 5f);
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void Delay_Disposed_DoesNotFire()
        {
            TickService service = CreateService();
            int fired = 0;
            IDisposable handle = service.Delay(0.1f, () => fired++);
            handle.Dispose();

            service.Advance(1f, 1f);

            Assert.That(fired, Is.Zero);
        }

        [Test]
        public void Repeat_FiresEveryInterval_IncludingCatchUp()
        {
            TickService service = CreateService();
            int fired = 0;
            service.Repeat(1f, () => fired++);

            // Exactly representable deltas (powers of two) — the timer accumulates floats.
            service.Advance(0.5f, 0.5f);
            Assert.That(fired, Is.Zero);
            service.Advance(0.5f, 0.5f);
            Assert.That(fired, Is.EqualTo(1));
            // A long frame catches up with multiple fires.
            service.Advance(2f, 2f);
            Assert.That(fired, Is.EqualTo(3));
        }

        [Test]
        public void Repeat_NonPositiveInterval_Throws()
        {
            TickService service = CreateService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Repeat(0f, () => { }));
        }

        [Test]
        public void NextFrame_FiresOnNextAdvanceOnly()
        {
            TickService service = CreateService();
            int fired = 0;
            service.NextFrame(() => fired++);

            service.Advance(0f, 0f);
            Assert.That(fired, Is.EqualTo(1));
            service.Advance(0f, 0f);
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void NextFrame_ScheduledDuringTick_FiresOnFollowingTick()
        {
            TickService service = CreateService();
            int fired = 0;
            service.NextFrame(() => service.NextFrame(() => fired++));

            service.Advance(0f, 0f);
            Assert.That(fired, Is.Zero);
            service.Advance(0f, 0f);
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void UnscaledTimer_UsesUnscaledDelta()
        {
            TickService service = CreateService();
            int fired = 0;
            service.Delay(1f, () => fired++, useUnscaledTime: true);

            // Scaled time stands still (paused game), unscaled advances.
            service.Advance(0f, 1f);

            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void IsPaused_BlocksListenersAndTimers()
        {
            TickService service = CreateService();
            int ticks = 0;
            int fired = 0;
            service.RegisterTick(_ => ticks++);
            service.Delay(1f, () => fired++);

            service.IsPaused = true;
            service.Advance(5f, 5f);
            Assert.That(ticks, Is.Zero);
            Assert.That(fired, Is.Zero);

            service.IsPaused = false;
            service.Advance(1f, 1f);
            Assert.That(ticks, Is.EqualTo(1));
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_StopsEverything()
        {
            TickService service = CreateService();
            int ticks = 0;
            service.RegisterTick(_ => ticks++);
            service.Dispose();

            service.Advance(1f, 1f);

            Assert.That(ticks, Is.Zero);
        }
    }
}
