using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Common;

namespace Vareiko.Foundation.Tests.Common
{
    public sealed class RetryPolicyTests
    {
        [Test]
        public void Constructor_ClampsInvalidValues()
        {
            RetryPolicy policy = new RetryPolicy(enabled: true, maxAttempts: 0, initialDelayMs: -10);

            Assert.That(policy.MaxAttempts, Is.EqualTo(1));
            Assert.That(policy.InitialDelayMs, Is.EqualTo(0));
            Assert.That(policy.GetDelayMs(1), Is.EqualTo(0));
            Assert.That(policy.GetDelayMs(2), Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteAsync_WhenDisabled_UsesSingleAttempt()
        {
            RetryPolicy policy = new RetryPolicy(enabled: false, maxAttempts: 5, initialDelayMs: 0);
            int calls = 0;

            int result = await policy.ExecuteAsync(
                _ =>
                {
                    calls++;
                    return UniTask.FromResult(calls);
                },
                value => value >= 3);

            Assert.That(result, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public async Task ExecuteAsync_RetriesUntilSuccess_AndInvokesRetryCallback()
        {
            RetryPolicy policy = new RetryPolicy(enabled: true, maxAttempts: 3, initialDelayMs: 0);
            int calls = 0;
            List<int> retryAttempts = new List<int>(2);

            int result = await policy.ExecuteAsync(
                _ =>
                {
                    calls++;
                    return UniTask.FromResult(calls);
                },
                value => value >= 2,
                (attempt, _, __) => retryAttempts.Add(attempt));

            Assert.That(result, Is.EqualTo(2));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(retryAttempts, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ExecuteAsync_NullArguments_Throws()
        {
            RetryPolicy policy = new RetryPolicy(enabled: true, maxAttempts: 2, initialDelayMs: 0);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await policy.ExecuteAsync<int>(null, value => value > 0));

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await policy.ExecuteAsync<int>(_ => UniTask.FromResult(1), null));
        }
    }
}
