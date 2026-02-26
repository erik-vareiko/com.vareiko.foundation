using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.UI
{
    public sealed class UIItemCollectionBinder : MonoBehaviour
    {
        [SerializeField] private UIItemView _itemPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private int _initialCount;
        [SerializeField] private bool _hideTemplateItem = true;
        [SerializeField] private bool _destroyItemsWhenShrinking;

        private readonly List<UIItemView> _items = new List<UIItemView>();
        private Transform _resolvedContainer;
        private bool _initialized;
        private int _activeCount;

        public event Action<int, UIItemView> ItemCreated;
        public event Action<int, UIItemView> ItemActivated;
        public event Action<int, UIItemView> ItemDeactivated;

        public int ActiveCount => _activeCount;
        public int PooledCount => _items.Count;
        public IReadOnlyList<UIItemView> Items => _items;

        private void Awake()
        {
            EnsureInitialized();
            if (_initialCount > 0)
            {
                SetCount(_initialCount);
            }
        }

        public void SetCount(int count)
        {
            EnsureInitialized();
            if (_itemPrefab == null)
            {
                _activeCount = 0;
                return;
            }

            int safeCount = Math.Max(0, count);
            EnsurePoolSize(safeCount);
            ApplyVisibility(safeCount);
            ShrinkPoolIfNeeded(safeCount);
            _activeCount = safeCount;
        }

        public bool TryGetItem(int index, out UIItemView item)
        {
            EnsureInitialized();
            if (index < 0 || index >= _items.Count)
            {
                item = null;
                return false;
            }

            item = _items[index];
            return item != null;
        }

        public void Clear(bool destroyPooledItems = false)
        {
            EnsureInitialized();

            if (destroyPooledItems)
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    UIItemView item = _items[i];
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                    }
                }

                _items.Clear();
                _activeCount = 0;
                return;
            }

            ApplyVisibility(0);
            _activeCount = 0;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _resolvedContainer = _container != null ? _container : transform;
            if (_hideTemplateItem && _itemPrefab != null && _itemPrefab.gameObject.scene.IsValid())
            {
                _itemPrefab.Hide(true);
            }

            _initialized = true;
        }

        private void EnsurePoolSize(int requiredCount)
        {
            while (_items.Count < requiredCount)
            {
                UIItemView item = Instantiate(_itemPrefab, _resolvedContainer);
                item.name = $"{_itemPrefab.name}_{_items.Count:00}";
                item.Hide(true);
                int createdIndex = _items.Count;
                _items.Add(item);
                ItemCreated?.Invoke(createdIndex, item);
            }
        }

        private void ApplyVisibility(int visibleCount)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                UIItemView item = _items[i];
                if (item == null)
                {
                    continue;
                }

                bool shouldBeVisible = i < visibleCount;
                if (shouldBeVisible)
                {
                    item.Show(true);
                    ItemActivated?.Invoke(i, item);
                }
                else
                {
                    item.Hide(true);
                    ItemDeactivated?.Invoke(i, item);
                }
            }
        }

        private void ShrinkPoolIfNeeded(int targetCount)
        {
            if (!_destroyItemsWhenShrinking || targetCount >= _items.Count)
            {
                return;
            }

            for (int i = _items.Count - 1; i >= targetCount; i--)
            {
                UIItemView item = _items[i];
                if (item != null)
                {
                    Destroy(item.gameObject);
                }

                _items.RemoveAt(i);
            }
        }
    }
}
