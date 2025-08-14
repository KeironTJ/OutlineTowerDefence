using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCurrencyWallet : ICurrencyWallet
{
    private readonly PlayerData data;
    private readonly Action saveCallback;

    public event Action<CurrencyType, float> BalanceChanged;

    public PlayerCurrencyWallet(PlayerData data, Action saveCallback = null)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.saveCallback = saveCallback;
    }

    public float Get(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Premium: return data.premiumCredits;
            case CurrencyType.Luxury:  return data.luxuryCredits;
            case CurrencyType.Special: return data.specialCredits;
            case CurrencyType.Basic:   return 0f; // round-only
            default: return 0f;
        }
    }

    public void Set(CurrencyType type, float amount)
    {
        amount = Math.Max(0f, amount);
        switch (type)
        {
            case CurrencyType.Premium:
                data.premiumCredits = amount;
                BalanceChanged?.Invoke(type, data.premiumCredits);
                break;
            case CurrencyType.Luxury:
                data.luxuryCredits = amount;
                BalanceChanged?.Invoke(type, data.luxuryCredits);
                break;
            case CurrencyType.Special:
                data.specialCredits = amount;
                BalanceChanged?.Invoke(type, data.specialCredits);
                break;
            case CurrencyType.Basic:
                return; // not stored here
        }
        saveCallback?.Invoke();
    }

    public void Add(CurrencyType type, float amount)
    {
        if (amount == 0f) return;

        switch (type)
        {
            case CurrencyType.Basic:
                if (amount > 0) data.totalBasicCreditsEarned += amount;
                saveCallback?.Invoke();
                return;

            case CurrencyType.Premium:
                data.premiumCredits += amount;
                if (amount > 0) data.totalPremiumCreditsEarned += amount;
                BalanceChanged?.Invoke(type, data.premiumCredits);
                break;

            case CurrencyType.Luxury:
                data.luxuryCredits += amount;
                if (amount > 0) data.totalLuxuryCreditsEarned += amount;
                BalanceChanged?.Invoke(type, data.luxuryCredits);
                break;

            case CurrencyType.Special:
                data.specialCredits += amount;
                if (amount > 0) data.totalSpecialCreditsEarned += amount;
                BalanceChanged?.Invoke(type, data.specialCredits);
                break;
        }
        saveCallback?.Invoke();
    }

    public bool TrySpend(CurrencyType type, float amount)
    {
        if (amount <= 0f) return true;

        switch (type)
        {
            case CurrencyType.Premium:
                if (data.premiumCredits >= amount)
                {
                    data.premiumCredits -= amount;
                    data.totalPremiumCreditsSpent += amount;
                    BalanceChanged?.Invoke(type, data.premiumCredits);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Luxury:
                if (data.luxuryCredits >= amount)
                {
                    data.luxuryCredits -= amount;
                    data.totalLuxuryCreditsSpent += amount;
                    BalanceChanged?.Invoke(type, data.luxuryCredits);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Special:
                if (data.specialCredits >= amount)
                {
                    data.specialCredits -= amount;
                    data.totalSpecialCreditsSpent += amount;
                    BalanceChanged?.Invoke(type, data.specialCredits);
                    saveCallback?.Invoke();
                    return true;
                }
                return false;

            case CurrencyType.Basic:
                return false;
        }
        return false;
    }

    public IReadOnlyDictionary<CurrencyType, float> GetAll()
    {
        return new Dictionary<CurrencyType, float>
        {
            { CurrencyType.Basic,   0f },
            { CurrencyType.Premium, data.premiumCredits },
            { CurrencyType.Luxury,  data.luxuryCredits  },
            { CurrencyType.Special, data.specialCredits }
        };
    }
}
