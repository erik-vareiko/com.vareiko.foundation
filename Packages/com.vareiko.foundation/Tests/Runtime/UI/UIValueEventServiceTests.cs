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
    }
}
