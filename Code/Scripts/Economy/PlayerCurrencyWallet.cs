using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages persistent player currency balances and transactions.
/// </summary>
public class PlayerCurrencyWallet : ICurrencyWallet
{
    private readonly PlayerData data;
    private readonly Action saveCallback;

    /// <summary>
    /// Fired when a currency balance changes.
    /// </summary>
    public event Action<CurrencyType, float> BalanceChanged;

    /// <summary>
    /// Creates a wallet for the given player data.
    /// </summary>
    public PlayerCurrencyWallet(PlayerData data, Action saveCallback = null)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.saveCallback = saveCallback;
    }

    /// <summary>
    /// Gets the current balance for the given currency type.
    /// Fragments are not stored here (round-only).
    /// </summary>
    public float Get(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Cores: return data.cores;
            case CurrencyType.Loops: return data.loops;
            case CurrencyType.Prisms: return data.prisms;
            case CurrencyType.Fragments: return 0f; // round-only
            default: return 0f;
        }
    }

    /// <summary>
    /// Sets the balance for the given currency type.
    /// Fragments are not stored here.
    /// </summary>
    public void Set(CurrencyType type, float amount)
    {
        amount = Math.Max(0f, amount);
        switch (type)
        {
            case CurrencyType.Cores:
                data.cores = amount;
                BalanceChanged?.Invoke(type, data.cores);
                break;
            case CurrencyType.Loops:
                data.loops = amount;
                BalanceChanged?.Invoke(type, data.loops);
                break;
            case CurrencyType.Prisms:
                data.prisms = amount;
                BalanceChanged?.Invoke(type, data.prisms);
                break;
            case CurrencyType.Fragments:
                return; // not stored here
        }
        saveCallback?.Invoke();
    }

    /// <summary>
    /// Adds the specified amount to the currency balance.
    /// Fragments are tracked as earned only.
    /// </summary>
    public void Add(CurrencyType type, float amount)
    {
        if (amount == 0f) return;

        switch (type)
        {
            case CurrencyType.Fragments:
                if (amount > 0) data.totalFragmentsEarned += amount;
                saveCallback?.Invoke();
                return;

            case CurrencyType.Cores:
                data.cores += amount;
                if (amount > 0) data.totalCoresEarned += amount;
                BalanceChanged?.Invoke(type, data.cores);
                break;

            case CurrencyType.Prisms:
                data.prisms += amount;
                if (amount > 0) data.totalPrismsEarned += amount;
                BalanceChanged?.Invoke(type, data.prisms);
                break;

            case CurrencyType.Loops:
                data.loops += amount;
                if (amount > 0) data.totalLoopsEarned += amount;
                BalanceChanged?.Invoke(type, data.loops);
                break;
        }
        saveCallback?.Invoke();
    }

    /// <summary>
    /// Attempts to spend the specified amount of currency.
    /// Returns true if successful, false if insufficient funds.
    /// Fragments cannot be spent.
    /// </summary>
    public bool TrySpend(CurrencyType type, float amount)
    {
        if (amount <= 0f) return true;

        switch (type)
        {
            case CurrencyType.Cores:
                if (data.cores >= amount)
                {
                    data.cores -= amount;
                    data.totalCoresSpent += amount;
                    BalanceChanged?.Invoke(type, data.cores);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Prisms:
                if (data.prisms >= amount)
                {
                    data.prisms -= amount;
                    data.totalPrismsSpent += amount;
                    BalanceChanged?.Invoke(type, data.prisms);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Loops:
                if (data.loops >= amount)
                {
                    data.loops -= amount;
                    data.totalLoopsSpent += amount;
                    BalanceChanged?.Invoke(type, data.loops);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Fragments:
                return false;
        }
        return false;
    }

    /// <summary>
    /// Gets a read-only dictionary of all currency balances.
    /// Fragments are always zero here.
    /// </summary>
    public IReadOnlyDictionary<CurrencyType, float> GetAll()
    {
        return new Dictionary<CurrencyType, float>
        {
            { CurrencyType.Fragments, 0f },
            { CurrencyType.Prisms, data.prisms },
            { CurrencyType.Loops, data.loops },
            { CurrencyType.Cores, data.cores }
        };
    }
}
