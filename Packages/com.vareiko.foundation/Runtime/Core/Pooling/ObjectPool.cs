using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Pooling
{
    public sealed class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>();

        public ObjectPool(
            Func<T> factory,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int maxSize = 64,
            int prewarmCount = 0)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (maxSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Pool max size must be positive.");
            }

            _factory = factory;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;

            for (int i = 0; i < prewarmCount && i < maxSize; i++)
            {
                _inactive.Push(_factory());
            }
        }

        public int CountInactive => _inactive.Count;
        public int CountActive => _active.Count;

        public T Get()
        {
            T item = _inactive.Count > 0 ? _inactive.Pop() : _factory();
            _active.Add(item);
            _onGet?.Invoke(item);
            return item;
        }

        public PooledItem<T> GetScoped(out T item)
        {
            item = Get();
            return new PooledItem<T>(this, item);
        }

        public void Release(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_active.Remove(item))
            {
                throw new InvalidOperationException($"Released an item that is not active in this pool: {item}.");
            }

            _onRelease?.Invoke(item);
            if (_inactive.Count < _maxSize)
            {
                _inactive.Push(item);
            }
            else
            {
                _onDestroy?.Invoke(item);
            }
        }

        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                _onDestroy?.Invoke(_inactive.Pop());
            }
        }

        public void Dispose()
        {
            Clear();
            _active.Clear();
        }
    }
}
