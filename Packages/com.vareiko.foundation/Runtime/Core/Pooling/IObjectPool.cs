using System;

namespace Vareiko.Foundation.Pooling
{
    /// <summary>
    /// Generic object pool primitive. <see cref="ObjectPool{T}"/> pools plain classes;
    /// <see cref="ComponentPool{T}"/> pools prefab instances with activate/deactivate lifecycle.
    /// </summary>
    public interface IObjectPool<T> : IDisposable where T : class
    {
        int CountInactive { get; }
        int CountActive { get; }

        T Get();

        /// <summary>Gets an item wrapped in a scope that releases it back on Dispose.</summary>
        PooledItem<T> GetScoped(out T item);

        void Release(T item);

        /// <summary>Destroys all inactive items. Active items are unaffected.</summary>
        void Clear();
    }

    public readonly struct PooledItem<T> : IDisposable where T : class
    {
        private readonly IObjectPool<T> _pool;
        private readonly T _item;

        public PooledItem(IObjectPool<T> pool, T item)
        {
            _pool = pool;
            _item = item;
        }

        public void Dispose()
        {
            _pool?.Release(_item);
        }
    }
}
