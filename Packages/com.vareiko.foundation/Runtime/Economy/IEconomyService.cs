using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Economy
{
    public interface IEconomyService
    {
        UniTask<long> GetBalanceAsync(string currencyId, CancellationToken cancellationToken = default);
        UniTask<IReadOnlyDictionary<string, long>> GetBalancesAsync(CancellationToken cancellationToken = default);
        UniTask<EconomyOperationResult> AddCurrencyAsync(string currencyId, long amount, string reason = null, CancellationToken cancellationToken = default);
        UniTask<EconomyOperationResult> SpendCurrencyAsync(string currencyId, long amount, string reason = null, CancellationToken cancellationToken = default);
        UniTask<int> GetItemCountAsync(string itemId, CancellationToken cancellationToken = default);
        UniTask<EconomyOperationResult> GrantItemAsync(string itemId, int count, string reason = null, CancellationToken cancellationToken = default);
        UniTask<EconomyOperationResult> ConsumeItemAsync(string itemId, int count, string reason = null, CancellationToken cancellationToken = default);
    }
}
