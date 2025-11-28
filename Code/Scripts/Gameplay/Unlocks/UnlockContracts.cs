using System;
using System.Collections.Generic;
using UnityEngine;

public enum UnlockableContentType
{
    TowerBase,
    Turret,
    Projectile
}

public enum RequirementMatchMode
{
    All,
    Any
}

public enum UnlockRequirementType
{
    HighestWave,
    MaxDifficulty,
    AchievementCompleted,
    DefinitionUnlocked,
    SpendCurrency
}

[Serializable]
public struct UnlockCurrencyCost
{
    public int cores;
    public int prisms;
    public int loops;

    public bool IsFree => cores <= 0 && prisms <= 0 && loops <= 0;

    public bool CanAfford(PlayerManager pm)
    {
        if (IsFree) return true;
        if (pm?.Wallet == null) return false;

        return pm.Wallet.Get(CurrencyType.Cores) >= cores &&
               pm.Wallet.Get(CurrencyType.Prisms) >= prisms &&
               pm.Wallet.Get(CurrencyType.Loops) >= loops;
    }

    public bool TrySpend(PlayerManager pm)
    {
        if (IsFree) return true;
        if (pm == null) return false;
        if (!CanAfford(pm)) return false;
        return pm.TrySpendCurrency(cores, prisms, loops);
    }

    public string ToLabel()
    {
        var parts = new List<string>(3);
        if (cores > 0) parts.Add($"{cores} C");
        if (prisms > 0) parts.Add($"{prisms} P");
        if (loops > 0) parts.Add($"{loops} L");
        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
    }

    public static UnlockCurrencyCost operator +(UnlockCurrencyCost a, UnlockCurrencyCost b)
    {
        return new UnlockCurrencyCost
        {
            cores = a.cores + b.cores,
            prisms = a.prisms + b.prisms,
            loops = a.loops + b.loops
        };
    }
}

[Serializable]
public class UnlockRequirement
{
    public UnlockRequirementType requirementType = UnlockRequirementType.HighestWave;
    [Tooltip("How to evaluate referenced ids: All = every id must pass, Any = at least one must pass.")]
    public RequirementMatchMode matchMode = RequirementMatchMode.All;
    [Tooltip("Used when requirement type references other definitions.")]
    public UnlockableContentType prerequisiteType = UnlockableContentType.Turret;
    [Tooltip("Ids referenced by this requirement (achievements, prerequisite definitions, etc).")]
    public string[] referencedIds = Array.Empty<string>();
    [Tooltip("Primary numeric threshold (wave number, difficulty, etc).")]
    public int thresholdValue = 1;
    [Tooltip("Specific difficulty level for HighestWave requirements (<=0 means any difficulty).")]
    public int difficultyLevel = 0;
    [Tooltip("Minimum achievement tier index required (-1 = final tier).")]
    public int minimumAchievementTier = -1;
    [Tooltip("Currency cost information for SpendCurrency requirements.")]
    public UnlockCurrencyCost currencyCost;
    [Tooltip("Optional override message shown when this requirement blocks unlocking.")]
    public string failureHint = string.Empty;
}

[Serializable]
public class UnlockRequirementGroup
{
    [Tooltip("Label shown on the unlock button when this group is available.")]
    public string label = "Unlock";
    [TextArea]
    [Tooltip("Optional description for UI.")]
    public string description;
    [Tooltip("All requirements listed here must pass. Leave empty for an always-available path.")]
    public UnlockRequirement[] requirements = Array.Empty<UnlockRequirement>();
}

[Serializable]
public class UnlockProfile
{
    [Tooltip("Automatically grant this definition without additional requirements.")]
    public bool grantByDefault = true;
    [Tooltip("Fallback message when locked and no specific reason exists.")]
    public string lockedHint = "Locked";
    [Tooltip("Unlock paths. When multiple groups exist, any passing group will unlock the definition.")]
    public UnlockRequirementGroup[] requirementGroups = Array.Empty<UnlockRequirementGroup>();
}

public interface IUnlockableDefinition
{
    string DefinitionId { get; }
    UnlockableContentType ContentType { get; }
    UnlockProfile UnlockProfile { get; }
}

public readonly struct UnlockPathInfo
{
    public readonly UnlockRequirementGroup group;
    public readonly UnlockCurrencyCost totalCost;
    public readonly string label;
    public readonly string description;

    public UnlockPathInfo(UnlockRequirementGroup group, UnlockCurrencyCost totalCost, string label, string description)
    {
        this.group = group;
        this.totalCost = totalCost;
        this.label = string.IsNullOrEmpty(label) ? "Unlock" : label;
        this.description = description;
    }

    public bool HasCost => !totalCost.IsFree;
}
