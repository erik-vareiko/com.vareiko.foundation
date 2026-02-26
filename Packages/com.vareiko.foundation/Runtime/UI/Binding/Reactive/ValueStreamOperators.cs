using System;

namespace Vareiko.Foundation.UI
{
    public static class ValueStreamOperators
    {
        public static IComputedValueStream<TResult> Map<TSource, TResult>(
            this IReadOnlyValueStream<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            ComputedValueStream<TResult> computed = null;
            IDisposable subscription = source.Subscribe(value =>
            {
                computed?.SetValue(selector(value));
            }, invokeImmediately: true);

            computed = new ComputedValueStream<TResult>(subscription);
            if (source.HasValue)
            {
                computed.SetValue(selector(source.Value));
            }

            return computed;
        }

        public static IComputedValueStream<TResult> Combine<TLeft, TRight, TResult>(
            this IReadOnlyValueStream<TLeft> left,
            IReadOnlyValueStream<TRight> right,
            Func<TLeft, TRight, TResult> selector)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            TLeft leftValue = default;
            TRight rightValue = default;
            bool hasLeft = false;
            bool hasRight = false;

            if (left.HasValue)
            {
                leftValue = left.Value;
                hasLeft = true;
            }

            if (right.HasValue)
            {
                rightValue = right.Value;
                hasRight = true;
            }

            ComputedValueStream<TResult> computed = null;

            IDisposable leftSubscription = left.Subscribe(value =>
            {
                leftValue = value;
                hasLeft = true;
                if (hasRight)
                {
                    computed?.SetValue(selector(leftValue, rightValue));
                }
            }, invokeImmediately: true);

            IDisposable rightSubscription = right.Subscribe(value =>
            {
                rightValue = value;
                hasRight = true;
                if (hasLeft)
                {
                    computed?.SetValue(selector(leftValue, rightValue));
                }
            }, invokeImmediately: true);

            computed = new ComputedValueStream<TResult>(new CompositeDisposable(leftSubscription, rightSubscription));
            if (hasLeft && hasRight)
            {
                computed.SetValue(selector(leftValue, rightValue));
            }

            return computed;
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private IDisposable _first;
            private IDisposable _second;

            public CompositeDisposable(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _first?.Dispose();
                _second?.Dispose();
                _first = null;
                _second = null;
            }
        }
    }
}
