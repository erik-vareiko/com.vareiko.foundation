using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Common
{
    public readonly struct RetryPolicy
    {
        public readonly bool Enabled;
        public readonly int MaxAttempts;
        public readonly int InitialDelayMs;

        public RetryPolicy(bool enabled, int maxAttempts, int initialDelayMs)
        {
            Enabled = enabled;
            MaxAttempts = maxAttempts < 1 ? 1 : maxAttempts;
            InitialDelayMs = initialDelayMs < 0 ? 0 : initialDelayMs;
        }

        public int GetDelayMs(int attempt)
        {
            if (attempt <= 1 || InitialDelayMs <= 0)
            {
                return 0;
            }

            return InitialDelayMs * (attempt - 1);
        }

        public async UniTask<T> ExecuteAsync<T>(
            Func<CancellationToken, UniTask<T>> operation,
            Func<T, bool> successPredicate,
            Action<int, int, string> onRetry = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (successPredicate == null)
            {
                throw new ArgumentNullException(nameof(successPredicate));
            }

            int maxAttempts = Enabled ? MaxAttempts : 1;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                T result = await operation(cancellationToken);
                if (successPredicate(result) || attempt >= maxAttempts)
                {
                    return result;
                }

                int delay = GetDelayMs(attempt + 1);
                onRetry?.Invoke(attempt, maxAttempts, string.Empty);
                if (delay > 0)
                {
                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                }
            }

            return await operation(cancellationToken);
        }
    }
}
