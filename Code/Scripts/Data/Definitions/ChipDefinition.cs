using UnityEngine;

public enum ChipRarity
{
    Level1 = 0,
    Level2 = 1,
    Level3 = 2,
    Level4 = 3,
    Level5 = 4
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
    [Tooltip("Base bonus value at rarity level 0 (Common)")]
    public float baseBonus = 1.0f;
    [Tooltip("Bonus multiplier per rarity level")]
    public float bonusPerRarity = 0.5f;
    [Tooltip("Format string for display (e.g., '+{0}%' or '{0}x')")]
    public string bonusFormat = "+{0}%";

    [Header("Stat Mapping")]
    [Tooltip("LEGACY: Single stat target (use statBonuses array for multiple bonuses)")]
    public StatId targetStat = StatId.Count;
    [Tooltip("LEGACY: Single contribution kind (use statBonuses array for multiple bonuses)")]
    public SkillContributionKind contributionKind = SkillContributionKind.FlatBonus;
    [Tooltip("Multiplier applied before pushing the value into the pipeline (e.g. convert percent to scalar by using 0.01).")]
    public float pipelineScale = 1f;
    [Tooltip("Optional scaling used for display helpers (e.g. convert scalar back to percent).")] 
    public float displayScale = 1f;
    [Tooltip("Minimum pipeline value after scaling (useful for clamping multipliers).")]
    public float pipelineMin = float.NegativeInfinity;
    [Tooltip("Maximum pipeline value after scaling (useful for clamping multipliers).")]
    public float pipelineMax = float.PositiveInfinity;
    
    [Header("Multiple Stat Bonuses (New System)")]
    [Tooltip("Multiple stat bonuses this chip provides at base rarity. Values scale with rarity.")]
    public StatBonus[] statBonuses = new StatBonus[0];
    
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

    public bool HasStatMapping => targetStat != StatId.Count || (statBonuses != null && statBonuses.Length > 0);
    
    /// <summary>
    /// Apply all stat bonuses from this chip to the collector.
    /// Supports both legacy single-stat mode and new multi-stat mode.
    /// Values are scaled by rarity level.
    /// </summary>
    public void ApplyStatBonuses(StatCollector collector, int rarityLevel)
    {
        if (collector == null) return;
        
        // Legacy single-stat mode
        if (targetStat != StatId.Count && contributionKind != SkillContributionKind.None)
        {
            float rawBonus = GetBonusAtRarity(rarityLevel);
            float pipelineValue = ToPipelineValue(rawBonus);
            
            switch (contributionKind)
            {
                case SkillContributionKind.Base:
                    collector.AddBase(targetStat, pipelineValue);
                    break;
                case SkillContributionKind.FlatBonus:
                    collector.AddFlatBonus(targetStat, pipelineValue);
                    break;
                case SkillContributionKind.Multiplier:
                    collector.AddMultiplier(targetStat, pipelineValue);
                    break;
                case SkillContributionKind.Percentage:
                    collector.AddPercentage(targetStat, pipelineValue);
                    break;
            }
        }
        
        // New multi-stat mode
        if (statBonuses != null && statBonuses.Length > 0)
        {
            foreach (var bonus in statBonuses)
            {
                if (bonus != null && bonus.IsValid)
                {
                    // Scale bonus value by rarity
                    float scaledValue = bonus.value * (1f + (bonusPerRarity * rarityLevel / baseBonus));
                    bonus.ApplyTo(collector, scaledValue);
                }
            }
        }
    }
    
    /// <summary>
    /// Legacy method for backward compatibility with ChipDefinitionStatExtensions
    /// </summary>
    public float ToPipelineValue(float rawValue)
    {
        float scaled = StatPipelineScaling.ApplyScaling(targetStat, rawValue, pipelineScale);
        return StatPipelineScaling.ApplyClamping(scaled, pipelineMin, pipelineMax);
    }
}
