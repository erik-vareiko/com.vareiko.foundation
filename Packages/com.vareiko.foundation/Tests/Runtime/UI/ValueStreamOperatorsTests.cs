using NUnit.Framework;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class ValueStreamOperatorsTests
    {
        [Test]
        public void Map_TransformsSourceValues()
        {
            ValueStream<int> source = new ValueStream<int>();
            using (IComputedValueStream<string> mapped = source.Map(v => $"Coins: {v}"))
            {
                string last = string.Empty;
                int calls = 0;
                using (mapped.Subscribe(v =>
                       {
                           last = v;
                           calls++;
                       }))
                {
                    Assert.That(calls, Is.EqualTo(0));

                    source.SetValue(10);
                    Assert.That(last, Is.EqualTo("Coins: 10"));
                    Assert.That(calls, Is.EqualTo(1));

                    source.SetValue(42);
                    Assert.That(last, Is.EqualTo("Coins: 42"));
                    Assert.That(calls, Is.EqualTo(2));
                }
            }
        }

        [Test]
        public void Map_TakesCurrentSourceValue()
        {
            ValueStream<int> source = new ValueStream<int>();
            source.SetValue(7);

            using (IComputedValueStream<int> mapped = source.Map(v => v * 3))
            {
                Assert.That(mapped.HasValue, Is.True);
                Assert.That(mapped.Value, Is.EqualTo(21));
            }
        }

        [Test]
        public void Combine_EmitsOnlyWhenBothSourcesHaveValues()
        {
            ValueStream<int> left = new ValueStream<int>();
            ValueStream<int> right = new ValueStream<int>();

            using (IComputedValueStream<int> combined = left.Combine(right, (l, r) => l + r))
            {
                int last = -1;
                int calls = 0;

                using (combined.Subscribe(v =>
                       {
                           last = v;
                           calls++;
                       }))
                {
                    Assert.That(calls, Is.EqualTo(0));

                    left.SetValue(2);
                    Assert.That(calls, Is.EqualTo(0));

                    right.SetValue(5);
                    Assert.That(calls, Is.EqualTo(1));
                    Assert.That(last, Is.EqualTo(7));

                    left.SetValue(10);
                    Assert.That(calls, Is.EqualTo(2));
                    Assert.That(last, Is.EqualTo(15));
                }
            }
        }

        [Test]
        public void Dispose_StopsFurtherUpdates()
        {
            ValueStream<int> source = new ValueStream<int>();
            IComputedValueStream<int> mapped = source.Map(v => v + 1);

            int last = -1;
            int calls = 0;
            using (mapped.Subscribe(v =>
                   {
                       last = v;
                       calls++;
                   }))
            {
                source.SetValue(1);
                Assert.That(last, Is.EqualTo(2));
                Assert.That(calls, Is.EqualTo(1));

                mapped.Dispose();
                source.SetValue(2);
                Assert.That(last, Is.EqualTo(2));
                Assert.That(calls, Is.EqualTo(1));
            }
        }
    }
}
