using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Pooling
{
    /// <summary>
    /// Pools instances of a prefab component. Inactive instances are deactivated and parented
    /// under <c>parent</c> (when provided); overflow beyond <c>maxSize</c> is destroyed.
    /// </summary>
    public sealed class ComponentPool<T> : IObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>();

        public ComponentPool(T prefab, Transform parent = null, int maxSize = 64, int prewarmCount = 0)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (maxSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Pool max size must be positive.");
            }

            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;

            for (int i = 0; i < prewarmCount && i < maxSize; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _inactive.Push(instance);
            }
        }

        public int CountInactive => _inactive.Count;
        public int CountActive => _active.Count;

        public T Get()
        {
            T item = null;
            while (_inactive.Count > 0 && item == null)
            {
                // Inactive instances can be destroyed externally (scene unload); skip the corpses.
                item = _inactive.Pop();
            }

            if (item == null)
            {
                item = CreateInstance();
            }

            _active.Add(item);
            item.gameObject.SetActive(true);
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
                throw new InvalidOperationException($"Released an instance that is not active in this pool: {item.name}.");
            }

            if (_inactive.Count < _maxSize)
            {
                item.gameObject.SetActive(false);
                if (_parent != null)
                {
                    item.transform.SetParent(_parent, false);
                }

                _inactive.Push(item);
            }
            else
            {
                DestroyInstance(item);
            }
        }

        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                T item = _inactive.Pop();
                if (item != null)
                {
                    DestroyInstance(item);
                }
            }
        }

        public void Dispose()
        {
            Clear();
            _active.Clear();
        }

        private T CreateInstance()
        {
            return _parent != null
                ? UnityEngine.Object.Instantiate(_prefab, _parent)
                : UnityEngine.Object.Instantiate(_prefab);
        }

        private static void DestroyInstance(T item)
        {
            // Destroy is play-mode only; EditMode (tests, tooling) needs DestroyImmediate.
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}
