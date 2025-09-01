using System;
using System.Collections.Generic;
using UnityEngine;

public class RoundCurrencyWallet : ICurrencyWallet
{
    private float fragmentsBalance;
    private readonly ICurrencyWallet playerWallet;
    private readonly Action<float> onFragmentsSpent; // NEW

    public event Action<CurrencyType, float> BalanceChanged;

    // NEW: accept a spend callback
    public RoundCurrencyWallet(ICurrencyWallet playerWallet, Action<float> onFragmentsSpent = null)
    {
        this.playerWallet = playerWallet;
        this.onFragmentsSpent = onFragmentsSpent;

        if (playerWallet != null)
        {
            playerWallet.BalanceChanged += (t, v) =>
            {
                if (t != CurrencyType.Fragments) BalanceChanged?.Invoke(t, v);
            };
        }
    }

    public float Get(CurrencyType type)
    {
        return type == CurrencyType.Fragments ? fragmentsBalance : playerWallet?.Get(type) ?? 0f;
    }

    public void Set(CurrencyType type, float amount)
    {
        if (type == CurrencyType.Fragments)
        {
            fragmentsBalance = Math.Max(0f, amount);
            BalanceChanged?.Invoke(CurrencyType.Fragments, fragmentsBalance);
        }
        else
        {
            playerWallet?.Set(type, amount);
        }
    }

    public void Add(CurrencyType type, float amount)
    {
        if (amount == 0f) return;

        if (type == CurrencyType.Fragments)
        {
            fragmentsBalance += amount;
            BalanceChanged?.Invoke(CurrencyType.Fragments, fragmentsBalance);
            playerWallet?.Add(CurrencyType.Fragments, Math.Max(0f, amount)); // KPI forward
        }
        else
        {
            playerWallet?.Add(type, amount);
        }
    }

    public bool TrySpend(CurrencyType type, float amount)
    {
        if (amount <= 0f) return true;

        if (type == CurrencyType.Fragments)
        {
            if (fragmentsBalance >= amount)
            {
                fragmentsBalance -= amount;
                BalanceChanged?.Invoke(CurrencyType.Fragments, fragmentsBalance);
                onFragmentsSpent?.Invoke(amount); // NEW: record lifetime spent
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

        map[CurrencyType.Fragments] = fragmentsBalance;
        return map;
    }

    public void ClearRound()
    {
        Set(CurrencyType.Fragments, 0f);
    }
}
