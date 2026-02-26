using System;
using NUnit.Framework;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIWindowResultPayloadTests
    {
        [Test]
        public void SerializeAndDeserialize_CustomDto_Works()
        {
            RewardPayload payload = new RewardPayload
            {
                RewardId = "daily_001",
                Amount = 3
            };

            string raw = UIWindowResultPayload.Serialize(payload);
            Assert.That(UIWindowResultPayload.TryDeserialize(raw, out RewardPayload restored), Is.True);
            Assert.That(restored.RewardId, Is.EqualTo("daily_001"));
            Assert.That(restored.Amount, Is.EqualTo(3));
        }

        [Test]
        public void TryDeserialize_PrimitiveRawPayload_Works()
        {
            Assert.That(UIWindowResultPayload.TryDeserialize("42", out int intValue), Is.True);
            Assert.That(intValue, Is.EqualTo(42));

            Assert.That(UIWindowResultPayload.TryDeserialize("true", out bool boolValue), Is.True);
            Assert.That(boolValue, Is.True);

            Assert.That(UIWindowResultPayload.TryDeserialize("1.5", out float floatValue), Is.True);
            Assert.That(floatValue, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void TryDeserialize_EnumRawPayload_Works()
        {
            Assert.That(
                UIWindowResultPayload.TryDeserialize("Confirmed", out UIWindowResultStatus status),
                Is.True);
            Assert.That(status, Is.EqualTo(UIWindowResultStatus.Confirmed));
        }

        [Test]
        public void ResultExtension_WithPayload_AndTryGetPayload_Works()
        {
            UIWindowResult result = new UIWindowResult("window.shop", UIWindowResultStatus.Confirmed)
                .WithPayload(new RewardPayload
                {
                    RewardId = "bundle_a",
                    Amount = 5
                });

            Assert.That(result.TryGetPayload(out RewardPayload payload), Is.True);
            Assert.That(payload.RewardId, Is.EqualTo("bundle_a"));
            Assert.That(payload.Amount, Is.EqualTo(5));
        }

        [Test]
        public void TryDeserialize_InvalidPayload_ReturnsFalse()
        {
            Assert.That(UIWindowResultPayload.TryDeserialize("not-json", out RewardPayload _), Is.False);
            Assert.That(UIWindowResultPayload.TryDeserialize(string.Empty, out int _), Is.False);
        }

        [Serializable]
        private struct RewardPayload
        {
            public string RewardId;
            public int Amount;
        }
    }
}
