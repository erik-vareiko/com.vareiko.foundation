using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vareiko.Foundation.Save;

namespace Vareiko.Foundation.Tests.Save
{
    public sealed class NewtonsoftJsonSaveSerializerTests
    {
        [Serializable]
        private sealed class Profile
        {
            public string Name;
            public int Level;
        }

        private sealed class RichProfile
        {
            public Dictionary<string, int> Currencies = new Dictionary<string, int>();
            public int? OptionalSeed;
        }

        [Test]
        public void RoundTrip_PlainModel()
        {
            NewtonsoftJsonSaveSerializer serializer = new NewtonsoftJsonSaveSerializer();
            Profile model = new Profile { Name = "p1", Level = 7 };

            string raw = serializer.Serialize(model);
            Assert.That(serializer.TryDeserialize(raw, out Profile loaded), Is.True);
            Assert.That(loaded.Name, Is.EqualTo("p1"));
            Assert.That(loaded.Level, Is.EqualTo(7));
        }

        [Test]
        public void RoundTrip_DictionaryAndNullable_TheJsonUtilityGaps()
        {
            NewtonsoftJsonSaveSerializer serializer = new NewtonsoftJsonSaveSerializer();
            RichProfile model = new RichProfile
            {
                Currencies = new Dictionary<string, int> { ["gold"] = 100, ["gems"] = 5 },
                OptionalSeed = 42
            };

            string raw = serializer.Serialize(model);
            Assert.That(serializer.TryDeserialize(raw, out RichProfile loaded), Is.True);
            Assert.That(loaded.Currencies["gold"], Is.EqualTo(100));
            Assert.That(loaded.Currencies["gems"], Is.EqualTo(5));
            Assert.That(loaded.OptionalSeed, Is.EqualTo(42));

            model.OptionalSeed = null;
            raw = serializer.Serialize(model);
            Assert.That(serializer.TryDeserialize(raw, out loaded), Is.True);
            Assert.That(loaded.OptionalSeed, Is.Null);
        }

        [Test]
        public void Reads_LegacyJsonUtilityPayloads()
        {
            // Saves written by the pre-3.0 default must keep loading after the swap.
            JsonUnitySaveSerializer legacy = new JsonUnitySaveSerializer();
            NewtonsoftJsonSaveSerializer current = new NewtonsoftJsonSaveSerializer();
            Profile model = new Profile { Name = "veteran", Level = 99 };

            string legacyRaw = legacy.Serialize(model);
            Assert.That(current.TryDeserialize(legacyRaw, out Profile loaded), Is.True);
            Assert.That(loaded.Name, Is.EqualTo("veteran"));
            Assert.That(loaded.Level, Is.EqualTo(99));
        }

        [Test]
        public void LegacySerializer_ReadsNewtonsoftPayloads_ForSimpleModels()
        {
            // The envelope is shared both ways for JsonUtility-compatible models, keeping the
            // fallback serializer interchangeable.
            NewtonsoftJsonSaveSerializer current = new NewtonsoftJsonSaveSerializer();
            JsonUnitySaveSerializer legacy = new JsonUnitySaveSerializer();
            Profile model = new Profile { Name = "both-ways", Level = 3 };

            string raw = current.Serialize(model);
            Assert.That(legacy.TryDeserialize(raw, out Profile loaded), Is.True);
            Assert.That(loaded.Name, Is.EqualTo("both-ways"));
        }

        [Test]
        public void TryDeserialize_GarbageOrEmpty_ReturnsFalse()
        {
            NewtonsoftJsonSaveSerializer serializer = new NewtonsoftJsonSaveSerializer();
            Assert.That(serializer.TryDeserialize("{not json", out Profile _), Is.False);
            Assert.That(serializer.TryDeserialize("", out Profile _), Is.False);
            Assert.That(serializer.TryDeserialize("   ", out Profile _), Is.False);
        }

        [Test]
        public void SecureSerializer_ComposesOverNewtonsoft()
        {
            SecureSaveSerializer secure = new SecureSaveSerializer(new NewtonsoftJsonSaveSerializer());
            RichProfile model = new RichProfile
            {
                Currencies = new Dictionary<string, int> { ["gold"] = 1 }
            };

            string raw = secure.Serialize(model);
            Assert.That(secure.TryDeserialize(raw, out RichProfile loaded), Is.True);
            Assert.That(loaded.Currencies["gold"], Is.EqualTo(1));
        }
    }
}
