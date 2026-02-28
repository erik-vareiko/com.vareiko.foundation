using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Iap
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/IAP Config")]
    public sealed class IapConfig : ScriptableObject
    {
        [Serializable]
        public sealed class ProductDefinition
        {
            [SerializeField] private string _productId = "product.id";
            [SerializeField] private InAppPurchaseProductType _productType = InAppPurchaseProductType.Consumable;
            [SerializeField] private string _localizedTitle = "Product";
            [SerializeField] private string _localizedDescription = string.Empty;
            [SerializeField] private double _price = 0.99d;
            [SerializeField] private string _isoCurrencyCode = "USD";
            [SerializeField] private string _localizedPriceString = "$0.99";
            [SerializeField] private bool _enabled = true;

            public string ProductId => string.IsNullOrWhiteSpace(_productId) ? string.Empty : _productId.Trim();
            public InAppPurchaseProductType ProductType => _productType;
            public string LocalizedTitle => _localizedTitle ?? string.Empty;
            public string LocalizedDescription => _localizedDescription ?? string.Empty;
            public double Price => _price < 0d ? 0d : _price;
            public string IsoCurrencyCode => _isoCurrencyCode ?? string.Empty;
            public string LocalizedPriceString => _localizedPriceString ?? string.Empty;
            public bool Enabled => _enabled;
        }

        [SerializeField] private InAppPurchaseProviderType _provider = InAppPurchaseProviderType.None;
        [SerializeField] private bool _autoInitializeOnStart;
        [SerializeField] private bool _simulateAlreadyOwnedAsFailure = true;
        [SerializeField] private List<ProductDefinition> _products = new List<ProductDefinition>();

        public InAppPurchaseProviderType Provider => _provider;
        public bool AutoInitializeOnStart => _autoInitializeOnStart;
        public bool SimulateAlreadyOwnedAsFailure => _simulateAlreadyOwnedAsFailure;
        public IReadOnlyList<ProductDefinition> Products => _products;
    }
}
