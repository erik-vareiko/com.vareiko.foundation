using NUnit.Framework;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIValueEventServiceTests
    {
        [Test]
        public void SetAndGet_Primitives_Work()
        {
            UIValueEventService service = new UIValueEventService(null);

            service.SetInt("hud.coins", 120);
            service.SetFloat("hud.hp", 77.5f);
            service.SetBool("hud.is_alive", true);
            service.SetString("hud.player_name", "Hero");

            Assert.That(service.TryGetInt("hud.coins", out int coins), Is.True);
            Assert.That(coins, Is.EqualTo(120));

            Assert.That(service.TryGetFloat("hud.hp", out float hp), Is.True);
            Assert.That(hp, Is.EqualTo(77.5f).Within(0.0001f));

            Assert.That(service.TryGetBool("hud.is_alive", out bool isAlive), Is.True);
            Assert.That(isAlive, Is.True);

            Assert.That(service.TryGetString("hud.player_name", out string playerName), Is.True);
            Assert.That(playerName, Is.EqualTo("Hero"));
        }

        [Test]
        public void Clear_RemovesOnlyRequestedKey()
        {
            UIValueEventService service = new UIValueEventService(null);

            service.SetInt("hud.coins", 120);
            service.SetInt("hud.gems", 7);

            service.Clear("hud.coins");

            Assert.That(service.TryGetInt("hud.coins", out _), Is.False);
            Assert.That(service.TryGetInt("hud.gems", out int gems), Is.True);
            Assert.That(gems, Is.EqualTo(7));
        }

        [Test]
        public void ObserveInt_EmitsCurrentAndNextValues()
        {
            UIValueEventService service = new UIValueEventService(null);
            service.SetInt("hud.coins", 10);

            IReadOnlyValueStream<int> stream = service.ObserveInt("hud.coins");
            int received = -1;
            int calls = 0;

            using (stream.Subscribe(value =>
                   {
                       received = value;
                       calls++;
                   }))
            {
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(received, Is.EqualTo(10));

                service.SetInt("hud.coins", 25);
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(received, Is.EqualTo(25));

                service.SetInt("hud.coins", 25);
                Assert.That(calls, Is.EqualTo(2));
            }
        }

        [Test]
        public void ObserveString_WaitsUntilFirstValue()
        {
            UIValueEventService service = new UIValueEventService(null);
            IReadOnlyValueStream<string> stream = service.ObserveString("hud.player_name");

            string received = string.Empty;
            int calls = 0;
            using (stream.Subscribe(value =>
                   {
                       received = value;
                       calls++;
                   }))
            {
                Assert.That(calls, Is.EqualTo(0));

                service.SetString("hud.player_name", "Rogue");
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(received, Is.EqualTo("Rogue"));
            }
        }

        [Test]
        public void ClearAll_ResetsStreamValueState()
        {
            UIValueEventService service = new UIValueEventService(null);
            service.SetBool("hud.is_alive", true);
            IReadOnlyValueStream<bool> stream = service.ObserveBool("hud.is_alive");

            Assert.That(stream.HasValue, Is.True);
            Assert.That(stream.Value, Is.True);

            service.ClearAll();

            Assert.That(stream.HasValue, Is.False);
        }
    }
}
