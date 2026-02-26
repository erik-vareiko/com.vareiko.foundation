using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Economy
{
    public sealed class InMemoryEconomyService : IEconomyService, IInitializable
    {
        private readonly EconomyConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, long> _balances = new Dictionary<string, long>();
        private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();

        [Inject]
        public InMemoryEconomyService([InjectOptional] EconomyConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _balances.Clear();
            _inventory.Clear();

            if (_config == null)
            {
                return;
            }

            IReadOnlyList<EconomyConfig.CurrencySeed> currencySeeds = _config.CurrencySeeds;
            for (int i = 0; i < currencySeeds.Count; i++)
            {
                EconomyConfig.CurrencySeed seed = currencySeeds[i];
                if (string.IsNullOrWhiteSpace(seed.CurrencyId))
                {
                    continue;
                }

                _balances[seed.CurrencyId] = seed.Amount < 0L ? 0L : seed.Amount;
            }

            IReadOnlyList<EconomyConfig.ItemSeed> itemSeeds = _config.ItemSeeds;
            for (int i = 0; i < itemSeeds.Count; i++)
            {
                EconomyConfig.ItemSeed seed = itemSeeds[i];
                if (string.IsNullOrWhiteSpace(seed.ItemId))
                {
                    continue;
                }

                _inventory[seed.ItemId] = Mathf.Max(0, seed.Count);
            }
        }

        public UniTask<long> GetBalanceAsync(string currencyId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long value;
            _balances.TryGetValue(currencyId ?? string.Empty, out value);
            return UniTask.FromResult(value);
        }

        public UniTask<IReadOnlyDictionary<string, long>> GetBalancesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<string, long> snapshot = new Dictionary<string, long>(_balances);
            return UniTask.FromResult(snapshot);
        }

        public UniTask<EconomyOperationResult> AddCurrencyAsync(string currencyId, long amount, string reason = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(currencyId) || amount <= 0)
            {
                return UniTask.FromResult(Fail("AddCurrency", "Invalid currency operation."));
            }

            long current;
            _balances.TryGetValue(currencyId, out current);
            long next = current + amount;
            _balances[currencyId] = next;
            _signalBus?.Fire(new CurrencyBalanceChangedSignal(currencyId, next));
            return UniTask.FromResult(EconomyOperationResult.Ok());
        }

        public UniTask<EconomyOperationResult> SpendCurrencyAsync(string currencyId, long amount, string reason = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(currencyId) || amount <= 0)
            {
                return UniTask.FromResult(Fail("SpendCurrency", "Invalid currency operation."));
            }

            long current;
            _balances.TryGetValue(currencyId, out current);
            if (current < amount)
            {
                return UniTask.FromResult(Fail("SpendCurrency", "Insufficient balance."));
            }

            long next = current - amount;
            _balances[currencyId] = next;
            _signalBus?.Fire(new CurrencyBalanceChangedSignal(currencyId, next));
            return UniTask.FromResult(EconomyOperationResult.Ok());
        }

        public UniTask<int> GetItemCountAsync(string itemId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count;
            _inventory.TryGetValue(itemId ?? string.Empty, out count);
            return UniTask.FromResult(count);
        }

        public UniTask<EconomyOperationResult> GrantItemAsync(string itemId, int count, string reason = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return UniTask.FromResult(Fail("GrantItem", "Invalid item operation."));
            }

            int current;
            _inventory.TryGetValue(itemId, out current);
            int next = current + count;
            _inventory[itemId] = next;
            _signalBus?.Fire(new InventoryItemChangedSignal(itemId, next));
            return UniTask.FromResult(EconomyOperationResult.Ok());
        }

        public UniTask<EconomyOperationResult> ConsumeItemAsync(string itemId, int count, string reason = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return UniTask.FromResult(Fail("ConsumeItem", "Invalid item operation."));
            }

            int current;
            _inventory.TryGetValue(itemId, out current);
            if (current < count)
            {
                return UniTask.FromResult(Fail("ConsumeItem", "Insufficient item count."));
            }

            int next = current - count;
            _inventory[itemId] = next;
            _signalBus?.Fire(new InventoryItemChangedSignal(itemId, next));
            return UniTask.FromResult(EconomyOperationResult.Ok());
        }

        private EconomyOperationResult Fail(string operation, string error)
        {
            _signalBus?.Fire(new EconomyOperationFailedSignal(operation, error));
            return EconomyOperationResult.Fail(error);
        }
    }
}
