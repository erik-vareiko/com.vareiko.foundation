using NUnit.Framework;
using Vareiko.Foundation.Rng;

namespace Vareiko.Foundation.Tests.Rng
{
    public sealed class DeterministicRngServiceTests
    {
        [Test]
        public void SameSeedAndScope_ProducesSameSequence()
        {
            DeterministicRngService a = new DeterministicRngService();
            DeterministicRngService b = new DeterministicRngService();
            a.Initialize(42);
            b.Initialize(42);

            IDeterministicRngStream streamA = a.CreateStream("run.nodes");
            IDeterministicRngStream streamB = b.CreateStream("run.nodes");

            for (int i = 0; i < 8; i++)
            {
                Assert.That(streamA.NextInt(0, 100000), Is.EqualTo(streamB.NextInt(0, 100000)));
            }
        }

        [Test]
        public void RestoreStream_ContinuesSequence()
        {
            DeterministicRngService service = new DeterministicRngService();
            service.Initialize(7);

            IDeterministicRngStream stream = service.CreateStream("battle.proc");
            int first = stream.NextInt(0, 1000);
            RngStreamState snapshot = stream.CaptureState();
            int nextFromOriginal = stream.NextInt(0, 1000);

            IDeterministicRngStream restored = service.RestoreStream("battle.proc", snapshot);
            int nextFromRestored = restored.NextInt(0, 1000);

            Assert.That(first, Is.Not.EqualTo(nextFromOriginal));
            Assert.That(nextFromRestored, Is.EqualTo(nextFromOriginal));
        }

        [Test]
        public void PickWeightedIndex_IsDeterministicForSameSeed()
        {
            float[] weights = { 0.2f, 1.3f, 4.1f, 2f };
            DeterministicRngService a = new DeterministicRngService();
            DeterministicRngService b = new DeterministicRngService();
            a.Initialize(111);
            b.Initialize(111);

            IDeterministicRngStream streamA = a.CreateStream("run.choices");
            IDeterministicRngStream streamB = b.CreateStream("run.choices");

            for (int i = 0; i < 12; i++)
            {
                Assert.That(streamA.PickWeightedIndex(weights), Is.EqualTo(streamB.PickWeightedIndex(weights)));
            }
        }

        [Test]
        public void CreateStream_SameScope_ReturnsProgressedState()
        {
            DeterministicRngService service = new DeterministicRngService();
            service.Initialize(99);

            IDeterministicRngStream first = service.CreateStream("shared");
            IDeterministicRngStream second = service.CreateStream("shared");
            Assert.That(ReferenceEquals(first, second), Is.True);

            RngStreamState stateBefore = first.CaptureState();
            _ = first.NextFloat01();
            RngStreamState stateAfterFirst = first.CaptureState();
            _ = second.NextFloat01();
            RngStreamState stateAfterSecond = second.CaptureState();

            Assert.That(stateAfterFirst.Step, Is.EqualTo(stateBefore.Step + 1));
            Assert.That(stateAfterSecond.Step, Is.EqualTo(stateAfterFirst.Step + 1));
        }
    }
}
