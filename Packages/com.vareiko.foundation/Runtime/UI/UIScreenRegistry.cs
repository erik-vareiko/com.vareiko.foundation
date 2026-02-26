using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.UI
{
    [Obsolete("Use UIRegistry instead.")]
    public class UIScreenRegistry : UIRegistry
    {
        [SerializeField] private List<UIScreen> _screens = new List<UIScreen>();

        public bool TryGet(string screenId, out UIScreen screen)
        {
            return TryGetScreen(screenId, out screen);
        }

        public IEnumerable<KeyValuePair<string, UIScreen>> Enumerate()
        {
            foreach (KeyValuePair<string, UIElement> pair in EnumerateElements())
            {
                UIScreen screen = pair.Value as UIScreen;
                if (screen != null)
                {
                    yield return new KeyValuePair<string, UIScreen>(pair.Key, screen);
                }
            }
        }

        protected override void CollectExplicitElements(List<UIElement> collector)
        {
            base.CollectExplicitElements(collector);
            for (int i = 0; i < _screens.Count; i++)
            {
                UIScreen screen = _screens[i];
                if (screen != null)
                {
                    collector.Add(screen);
                }
            }
        }
    }
}
