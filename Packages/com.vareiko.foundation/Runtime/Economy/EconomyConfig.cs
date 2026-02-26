using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Economy
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Serializable]
        public struct CurrencySeed
        {
            public string CurrencyId;
            public long Amount;
        }

        [Serializable]
        public struct ItemSeed
        {
            public string ItemId;
            public int Count;
        }

        [SerializeField] private List<CurrencySeed> _currencySeeds = new List<CurrencySeed>();
        [SerializeField] private List<ItemSeed> _itemSeeds = new List<ItemSeed>();

        public IReadOnlyList<CurrencySeed> CurrencySeeds => _currencySeeds;
        public IReadOnlyList<ItemSeed> ItemSeeds => _itemSeeds;
    }
}
