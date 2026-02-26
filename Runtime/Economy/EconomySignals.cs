namespace Vareiko.Foundation.Economy
{
    public readonly struct CurrencyBalanceChangedSignal
    {
        public readonly string CurrencyId;
        public readonly long Balance;

        public CurrencyBalanceChangedSignal(string currencyId, long balance)
        {
            CurrencyId = currencyId;
            Balance = balance;
        }
    }

    public readonly struct InventoryItemChangedSignal
    {
        public readonly string ItemId;
        public readonly int Count;

        public InventoryItemChangedSignal(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    public readonly struct EconomyOperationFailedSignal
    {
        public readonly string Operation;
        public readonly string Error;

        public EconomyOperationFailedSignal(string operation, string error)
        {
            Operation = operation;
            Error = error;
        }
    }
}
