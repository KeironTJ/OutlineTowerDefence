using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages round-based currency (fragments) and delegates other currencies to the player wallet.
/// </summary>
public class RoundCurrencyWallet : ICurrencyWallet
{
    private float fragmentsBalance;
    private readonly ICurrencyWallet playerWallet;
    private readonly Action<float> onFragmentsSpent;

    /// <summary>
    /// Fired when any currency balance changes.
    /// </summary>
    public event Action<CurrencyType, float> BalanceChanged;

    /// <summary>
    /// Creates a round wallet, wrapping the player wallet.
    /// </summary>
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

    /// <summary>
    /// Gets the current balance for the given currency type.
    /// </summary>
    public float Get(CurrencyType type)
    {
        return type == CurrencyType.Fragments ? fragmentsBalance : playerWallet?.Get(type) ?? 0f;
    }

    /// <summary>
    /// Sets the balance for the given currency type.
    /// </summary>
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

    /// <summary>
    /// Adds the specified amount to the currency balance.
    /// </summary>
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

    /// <summary>
    /// Attempts to spend the specified amount of currency.
    /// Returns true if successful, false if insufficient funds.
    /// </summary>
    public bool TrySpend(CurrencyType type, float amount)
    {
        if (amount <= 0f) return true;

        if (type == CurrencyType.Fragments)
        {
            if (fragmentsBalance >= amount)
            {
                fragmentsBalance -= amount;
                BalanceChanged?.Invoke(CurrencyType.Fragments, fragmentsBalance);
                onFragmentsSpent?.Invoke(amount);
                return true;
            }
            return false;
        }
        return playerWallet?.TrySpend(type, amount) ?? false;
    }

    /// <summary>
    /// Gets a read-only dictionary of all currency balances.
    /// </summary>
    public IReadOnlyDictionary<CurrencyType, float> GetAll()
    {
        var map = playerWallet?.GetAll() is Dictionary<CurrencyType, float> d
            ? new Dictionary<CurrencyType, float>(d)
            : new Dictionary<CurrencyType, float>();

        map[CurrencyType.Fragments] = fragmentsBalance;
        return map;
    }

    /// <summary>
    /// Clears the round-based fragments balance.
    /// </summary>
    public void ClearRound()
    {
        Set(CurrencyType.Fragments, 0f);
    }
}
