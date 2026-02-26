using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Economy;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Economy
{
    public sealed class InMemoryEconomyServiceTests
    {
        [Test]
        public async Task Initialize_AppliesSeeds_AndClampsNegativeValues()
        {
            EconomyConfig config = CreateConfig();
            try
            {
                InMemoryEconomyService service = new InMemoryEconomyService(config, null);
                service.Initialize();

                long gold = await service.GetBalanceAsync("gold");
                long gems = await service.GetBalanceAsync("gems");
                int potion = await service.GetItemCountAsync("potion");
                int badItem = await service.GetItemCountAsync("broken");

                Assert.That(gold, Is.EqualTo(100));
                Assert.That(gems, Is.EqualTo(0));
                Assert.That(potion, Is.EqualTo(3));
                Assert.That(badItem, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task CurrencyOperations_HandleSuccessAndFailure()
        {
            InMemoryEconomyService service = new InMemoryEconomyService(null, null);
            service.Initialize();

            EconomyOperationResult invalidAdd = await service.AddCurrencyAsync(string.Empty, 10);
            Assert.That(invalidAdd.Success, Is.False);

            EconomyOperationResult added = await service.AddCurrencyAsync("gold", 100);
            Assert.That(added.Success, Is.True);

            EconomyOperationResult overSpend = await service.SpendCurrencyAsync("gold", 120);
            Assert.That(overSpend.Success, Is.False);

            EconomyOperationResult spend = await service.SpendCurrencyAsync("gold", 40);
            Assert.That(spend.Success, Is.True);

            long gold = await service.GetBalanceAsync("gold");
            Assert.That(gold, Is.EqualTo(60));
        }

        [Test]
        public async Task InventoryOperations_HandleSuccessAndFailure()
        {
            InMemoryEconomyService service = new InMemoryEconomyService(null, null);
            service.Initialize();

            EconomyOperationResult invalidGrant = await service.GrantItemAsync("potion", 0);
            Assert.That(invalidGrant.Success, Is.False);

            EconomyOperationResult granted = await service.GrantItemAsync("potion", 5);
            Assert.That(granted.Success, Is.True);

            EconomyOperationResult overConsume = await service.ConsumeItemAsync("potion", 6);
            Assert.That(overConsume.Success, Is.False);

            EconomyOperationResult consumed = await service.ConsumeItemAsync("potion", 2);
            Assert.That(consumed.Success, Is.True);

            int count = await service.GetItemCountAsync("potion");
            Assert.That(count, Is.EqualTo(3));
        }

        private static EconomyConfig CreateConfig()
        {
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_currencySeeds", new List<EconomyConfig.CurrencySeed>
            {
                new EconomyConfig.CurrencySeed { CurrencyId = "gold", Amount = 100L },
                new EconomyConfig.CurrencySeed { CurrencyId = "gems", Amount = -50L }
            });
            ReflectionTestUtil.SetPrivateField(config, "_itemSeeds", new List<EconomyConfig.ItemSeed>
            {
                new EconomyConfig.ItemSeed { ItemId = "potion", Count = 3 },
                new EconomyConfig.ItemSeed { ItemId = "broken", Count = -2 }
            });
            return config;
        }
    }
}
