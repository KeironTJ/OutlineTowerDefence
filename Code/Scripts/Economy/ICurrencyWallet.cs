using System;
using System.Collections.Generic;

public interface ICurrencyWallet
{
    // Raised when any currency balance changes
    event Action<CurrencyType, float> BalanceChanged;

    float Get(CurrencyType type);
    void Set(CurrencyType type, float amount);
    void Add(CurrencyType type, float amount);       // earn
    bool TrySpend(CurrencyType type, float amount);  // spend
    IReadOnlyDictionary<CurrencyType, float> GetAll();
}