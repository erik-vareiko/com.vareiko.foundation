using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Input;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Input
{
    public sealed class InputServiceTests
    {
        [Test]
        public void CurrentScheme_UsesFirstAvailable_ThenSwitchesToPreferred()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            List<InputScheme> changes = new List<InputScheme>(2);
            signalBus.Subscribe<InputSchemeChangedSignal>(signal => changes.Add(signal.Scheme));

            InputService service = new InputService(new List<IInputAdapter>
            {
                new FakeInputAdapter(InputScheme.KeyboardMouse, true, new Vector2(1f, 0f), submitPressedDown: true),
                new FakeInputAdapter(InputScheme.Gamepad, true, new Vector2(0f, 1f), pausePressedDown: true)
            }, signalBus);

            Assert.That(service.CurrentScheme, Is.EqualTo(InputScheme.KeyboardMouse));
            Assert.That(service.SubmitPressedDown, Is.True);

            service.SetPreferredScheme(InputScheme.Gamepad);

            Assert.That(service.CurrentScheme, Is.EqualTo(InputScheme.Gamepad));
            Assert.That(service.Move, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(service.PausePressedDown, Is.True);
            Assert.That(changes, Is.EqualTo(new[] { InputScheme.KeyboardMouse, InputScheme.Gamepad }));
        }

        [Test]
        public void PreferredSchemeUnavailable_FallsBackToFirstAvailable()
        {
            FakeSignalBus signalBus = new FakeSignalBus();

            InputService service = new InputService(new List<IInputAdapter>
            {
                new FakeInputAdapter(InputScheme.KeyboardMouse, true, new Vector2(0.5f, 0.5f)),
                new FakeInputAdapter(InputScheme.Gamepad, false, Vector2.zero)
            }, signalBus);

            service.SetPreferredScheme(InputScheme.Gamepad);

            Assert.That(service.CurrentScheme, Is.EqualTo(InputScheme.KeyboardMouse));
            Assert.That(service.Move, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void NoAvailableAdapters_ReturnsUnknown()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            int signalCount = 0;
            signalBus.Subscribe<InputSchemeChangedSignal>(_ => signalCount++);

            InputService service = new InputService(new List<IInputAdapter>
            {
                new FakeInputAdapter(InputScheme.Gamepad, false, Vector2.zero)
            }, signalBus);

            Assert.That(service.CurrentScheme, Is.EqualTo(InputScheme.Unknown));
            Assert.That(service.Move, Is.EqualTo(Vector2.zero));
            Assert.That(service.DashPressedDown, Is.False);
            Assert.That(signalCount, Is.EqualTo(0));
        }

        private sealed class FakeInputAdapter : IInputAdapter
        {
            public InputScheme Scheme { get; }
            public bool IsAvailable { get; }
            public Vector2 Move { get; }
            public bool DashPressedDown { get; }
            public bool PausePressedDown { get; }
            public bool SubmitPressedDown { get; }
            public bool CancelPressedDown { get; }

            public FakeInputAdapter(
                InputScheme scheme,
                bool isAvailable,
                Vector2 move,
                bool dashPressedDown = false,
                bool pausePressedDown = false,
                bool submitPressedDown = false,
                bool cancelPressedDown = false)
            {
                Scheme = scheme;
                IsAvailable = isAvailable;
                Move = move;
                DashPressedDown = dashPressedDown;
                PausePressedDown = pausePressedDown;
                SubmitPressedDown = submitPressedDown;
                CancelPressedDown = cancelPressedDown;
            }
        }
    }
}
