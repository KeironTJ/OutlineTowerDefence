using UnityEngine;

public enum ChipRarity
{
    Level1 = 0,
    Level2 = 1,
    Level3 = 2,
    Level4 = 3,
    Level5 = 4
}

public enum ChipBonusType
{
    AttackDamageMultiplier,
    AttackSpeed,
    CoresPerKillMultiplier,
    Health,
    HealthRecoverySpeed,
    FragmentsBoost,
    CriticalChance,
    CriticalDamage,
    ProjectileSpeed,
    TurretRange,
    ExperienceBoost
}

[CreateAssetMenu(menuName = "Outline/ChipDefinition")]
public class ChipDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string chipName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("Visual")]
    public Sprite icon;
    
    [Header("Bonus Configuration")]
    public ChipBonusType bonusType;
    [Tooltip("Base bonus value at rarity level 0 (Common)")]
    public float baseBonus = 1.0f;
    [Tooltip("Bonus multiplier per rarity level")]
    public float bonusPerRarity = 0.5f;
    [Tooltip("Format string for display (e.g., '+{0}%' or '{0}x')")]
    public string bonusFormat = "+{0}%";
    
    [Header("Rarity Progression")]
    [Tooltip("Number of chips needed to reach each rarity level. Array size = max rarity level")]
    public int[] chipsNeededForRarity = new int[] { 0, 3, 5, 7, 10 }; // Level1 to Level5
    
    [Header("Restrictions")]
    [Tooltip("Can this chip be changed during a round?")]
    public bool canChangeInRound = true;
    [Tooltip("Minimum wave required to unlock this chip")]
    public int unlockWave = 1;
    
    // Helper methods
    public int GetMaxRarity()
    {
        return chipsNeededForRarity.Length - 1;
    }
    
    public int GetChipsNeededForRarity(int rarity)
    {
        if (rarity < 0 || rarity >= chipsNeededForRarity.Length)
            return int.MaxValue;
        return chipsNeededForRarity[rarity];
    }
    
    public float GetBonusAtRarity(int rarity)
    {
        rarity = Mathf.Clamp(rarity, 0, GetMaxRarity());
        return baseBonus + (bonusPerRarity * rarity);
    }
    
    public string GetFormattedBonus(int rarity)
    {
        float bonus = GetBonusAtRarity(rarity);
        return string.Format(bonusFormat, bonus);
    }
    
    public ChipRarity GetRarityEnum(int rarityLevel)
    {
        rarityLevel = Mathf.Clamp(rarityLevel, 0, 4);
        return (ChipRarity)rarityLevel;
    }
}
