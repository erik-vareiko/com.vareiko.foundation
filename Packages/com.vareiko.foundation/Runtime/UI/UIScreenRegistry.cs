using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.UI
{
    public sealed class UIScreenRegistry : MonoBehaviour
    {
        [SerializeField] private List<UIScreen> _screens = new List<UIScreen>();
        [SerializeField] private bool _scanChildrenOnAwake = true;

        private readonly Dictionary<string, UIScreen> _map = new Dictionary<string, UIScreen>(StringComparer.Ordinal);

        public int Count => _map.Count;

        private void Awake()
        {
            BuildMap();
        }

        public bool TryGet(string screenId, out UIScreen screen)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                screen = null;
                return false;
            }

            return _map.TryGetValue(screenId, out screen);
        }

        public void BuildMap()
        {
            _map.Clear();

            for (int i = 0; i < _screens.Count; i++)
            {
                Register(_screens[i]);
            }

            if (_scanChildrenOnAwake)
            {
                UIScreen[] screens = GetComponentsInChildren<UIScreen>(true);
                for (int i = 0; i < screens.Length; i++)
                {
                    Register(screens[i]);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, UIScreen>> Enumerate()
        {
            return _map;
        }

        private void Register(UIScreen screen)
        {
            if (screen == null || string.IsNullOrWhiteSpace(screen.Id))
            {
                return;
            }

            _map[screen.Id] = screen;
        }
    }
}
