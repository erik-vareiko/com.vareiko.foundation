using System.Collections.Generic;
using NUnit.Framework;
using Vareiko.Foundation.App;

namespace Vareiko.Foundation.Tests.Primitives
{
    public sealed class StateMachineTests
    {
        private enum Phase
        {
            Idle,
            Running,
            Done
        }

        [Test]
        public void TryEnter_SameState_ReturnsFalse_NoEvent()
        {
            StateMachine<Phase> machine = new StateMachine<Phase>(Phase.Idle);
            int events = 0;
            machine.StateChanged += (_, _) => events++;

            Assert.That(machine.TryEnter(Phase.Idle), Is.False);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void TryEnter_WithoutGuard_AllowsAnyTransition()
        {
            StateMachine<Phase> machine = new StateMachine<Phase>(Phase.Idle);
            Assert.That(machine.TryEnter(Phase.Done), Is.True);
            Assert.That(machine.Current, Is.EqualTo(Phase.Done));
        }

        [Test]
        public void TryEnter_GuardRejects_StateUnchanged()
        {
            StateMachine<Phase> machine = new StateMachine<Phase>(
                Phase.Idle,
                (current, next) => next != Phase.Done);

            Assert.That(machine.TryEnter(Phase.Done), Is.False);
            Assert.That(machine.Current, Is.EqualTo(Phase.Idle));
            Assert.That(machine.TryEnter(Phase.Running), Is.True);
        }

        [Test]
        public void ForceEnter_BypassesGuard_AndRaisesEvent()
        {
            StateMachine<Phase> machine = new StateMachine<Phase>(Phase.Idle, (_, _) => false);
            List<(Phase previous, Phase current)> events = new List<(Phase, Phase)>();
            machine.StateChanged += (previous, current) => events.Add((previous, current));

            machine.ForceEnter(Phase.Done);

            Assert.That(machine.Current, Is.EqualTo(Phase.Done));
            Assert.That(events, Is.EqualTo(new[] { (Phase.Idle, Phase.Done) }));
        }

        [Test]
        public void IsIn_ComparesCurrent()
        {
            StateMachine<Phase> machine = new StateMachine<Phase>(Phase.Running);
            Assert.That(machine.IsIn(Phase.Running), Is.True);
            Assert.That(machine.IsIn(Phase.Idle), Is.False);
        }
    }

    public sealed class AppStateStructTests
    {
        [Test]
        public void WellKnownStates_KeepTheirIdentity()
        {
            Assert.That(AppState.Boot == new AppState("Boot"), Is.True);
            Assert.That(AppState.Boot != AppState.Error, Is.True);
            Assert.That(AppState.None.IsNone, Is.True);
            Assert.That(default(AppState), Is.EqualTo(AppState.None));
        }

        [Test]
        public void CustomState_IsFirstClass()
        {
            AppState metaShop = new AppState("MetaShop");
            Assert.That(metaShop.IsNone, Is.False);
            Assert.That(metaShop.Id, Is.EqualTo("MetaShop"));
            Assert.That(metaShop, Is.EqualTo(new AppState(" MetaShop ")), "Ids are trimmed.");
        }

        [Test]
        public void EmptyId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _ = new AppState("  "));
        }

        [Test]
        public void AppStateMachine_AcceptsCustomStates()
        {
            AppStateMachine machine = new AppStateMachine(new TestDoubles.FakeSignalBus());
            machine.ForceEnter(AppState.Boot);

            AppState custom = new AppState("MetaShop");
            Assert.That(machine.TryEnter(custom), Is.True);
            Assert.That(machine.IsIn(custom), Is.True);
            Assert.That(machine.TryEnter(AppState.Gameplay), Is.True);
        }
    }
}
