using System;
using System.Collections.Generic;
using UnityEngine;

public class RoundCurrencyWallet : ICurrencyWallet
{
    private float basicBalance;
    private readonly ICurrencyWallet playerWallet;
    private readonly Action<float> onBasicSpent; // NEW

    public event Action<CurrencyType, float> BalanceChanged;

    // NEW: accept a spend callback
    public RoundCurrencyWallet(ICurrencyWallet playerWallet, Action<float> onBasicSpent = null)
    {
        this.playerWallet = playerWallet;
        this.onBasicSpent = onBasicSpent;

        if (playerWallet != null)
        {
            playerWallet.BalanceChanged += (t, v) =>
            {
                if (t != CurrencyType.Basic) BalanceChanged?.Invoke(t, v);
            };
        }
    }

    public float Get(CurrencyType type)
    {
        return type == CurrencyType.Basic ? basicBalance : playerWallet?.Get(type) ?? 0f;
    }

    public void Set(CurrencyType type, float amount)
    {
        if (type == CurrencyType.Basic)
        {
            basicBalance = Math.Max(0f, amount);
            BalanceChanged?.Invoke(CurrencyType.Basic, basicBalance);
        }
        else
        {
            playerWallet?.Set(type, amount);
        }
    }

    public void Add(CurrencyType type, float amount)
    {
        if (amount == 0f) return;

        if (type == CurrencyType.Basic)
        {
            basicBalance += amount;
            BalanceChanged?.Invoke(CurrencyType.Basic, basicBalance);
            playerWallet?.Add(CurrencyType.Basic, Math.Max(0f, amount)); // KPI forward
        }
        else
        {
            playerWallet?.Add(type, amount);
        }
    }

    public bool TrySpend(CurrencyType type, float amount)
    {
        if (amount <= 0f) return true;

        if (type == CurrencyType.Basic)
        {
            if (basicBalance >= amount)
            {
                basicBalance -= amount;
                BalanceChanged?.Invoke(CurrencyType.Basic, basicBalance);
                onBasicSpent?.Invoke(amount); // NEW: record lifetime spent
                return true;
            }
            return false;
        }
        return playerWallet?.TrySpend(type, amount) ?? false;
    }

    public IReadOnlyDictionary<CurrencyType, float> GetAll()
    {
        var map = playerWallet?.GetAll() is Dictionary<CurrencyType, float> d
            ? new Dictionary<CurrencyType, float>(d)
            : new Dictionary<CurrencyType, float>();

        map[CurrencyType.Basic] = basicBalance;
        return map;
    }

    public void ClearRound()
    {
        Set(CurrencyType.Basic, 0f);
    }
}
