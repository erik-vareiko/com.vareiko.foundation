using System.Collections.Generic;
using NUnit.Framework;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.App
{
    public sealed class AppStateMachineTests
    {
        // --- Transition rules ---

        [Test]
        public void Initial_BeforeInitialize_IsNone()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            Assert.That(machine.Current, Is.EqualTo(AppState.None));
        }

        [Test]
        public void TryEnter_FromNone_OnlyAllowsBoot()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());

            Assert.That(machine.TryEnter(AppState.MainMenu), Is.False);
            Assert.That(machine.Current, Is.EqualTo(AppState.None));

            Assert.That(machine.TryEnter(AppState.Boot), Is.True);
            Assert.That(machine.Current, Is.EqualTo(AppState.Boot));
        }

        [Test]
        public void TryEnter_ToSameState_ReturnsFalse()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Boot);

            Assert.That(machine.TryEnter(AppState.Boot), Is.False);
            Assert.That(machine.Current, Is.EqualTo(AppState.Boot));
        }

        [Test]
        public void TryEnter_ToNone_IsRejected()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Gameplay);

            Assert.That(machine.TryEnter(AppState.None), Is.False);
            Assert.That(machine.Current, Is.EqualTo(AppState.Gameplay));
        }

        [Test]
        public void TryEnter_FromShutdown_IsTerminal()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Shutdown);

            Assert.That(machine.TryEnter(AppState.MainMenu), Is.False);
            Assert.That(machine.Current, Is.EqualTo(AppState.Shutdown));
        }

        [Test]
        public void TryEnter_NormalTransition_Succeeds()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Boot);

            Assert.That(machine.TryEnter(AppState.MainMenu), Is.True);
            Assert.That(machine.Current, Is.EqualTo(AppState.MainMenu));
        }

        [Test]
        public void ForceEnter_BypassesTransitionRules()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Shutdown);

            // Shutdown is terminal for TryEnter, but ForceEnter must still override.
            machine.ForceEnter(AppState.Gameplay);
            Assert.That(machine.Current, Is.EqualTo(AppState.Gameplay));
        }

        [Test]
        public void IsIn_ReflectsCurrentState()
        {
            AppStateMachine machine = new AppStateMachine(new FakeSignalBus());
            machine.ForceEnter(AppState.Pause);

            Assert.That(machine.IsIn(AppState.Pause), Is.True);
            Assert.That(machine.IsIn(AppState.Gameplay), Is.False);
        }

        // --- Signal behaviour ---

        [Test]
        public void ForceEnter_FiresAppStateChangedSignal()
        {
            FakeSignalBus bus = new FakeSignalBus();
            List<AppStateChangedSignal> changes = new List<AppStateChangedSignal>();
            bus.Subscribe<AppStateChangedSignal>(changes.Add);

            AppStateMachine machine = new AppStateMachine(bus);
            machine.ForceEnter(AppState.Boot);

            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Previous, Is.EqualTo(AppState.None));
            Assert.That(changes[0].Current, Is.EqualTo(AppState.Boot));
        }

        [Test]
        public void Initialize_EntersBootAndFiresSignal()
        {
            FakeSignalBus bus = new FakeSignalBus();
            List<AppStateChangedSignal> changes = new List<AppStateChangedSignal>();
            bus.Subscribe<AppStateChangedSignal>(changes.Add);

            AppStateMachine machine = new AppStateMachine(bus);
            machine.Initialize();

            Assert.That(machine.Current, Is.EqualTo(AppState.Boot));
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Current, Is.EqualTo(AppState.Boot));

            machine.Dispose();
        }

        [Test]
        public void BootFailedSignal_TransitionsToError()
        {
            FakeSignalBus bus = new FakeSignalBus();
            AppStateMachine machine = new AppStateMachine(bus);
            machine.Initialize();

            bus.Publish(new ApplicationBootFailedSignal("BootTask", "boom"));

            Assert.That(machine.Current, Is.EqualTo(AppState.Error));
            machine.Dispose();
        }

        [Test]
        public void BootFailedSignal_AfterDispose_IsIgnored()
        {
            FakeSignalBus bus = new FakeSignalBus();
            AppStateMachine machine = new AppStateMachine(bus);
            machine.Initialize();
            machine.Dispose();

            bus.Publish(new ApplicationBootFailedSignal("BootTask", "boom"));

            Assert.That(machine.Current, Is.EqualTo(AppState.Boot));
        }
    }
}
