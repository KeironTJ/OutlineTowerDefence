using UnityEngine;

[CreateAssetMenu(menuName = "Outline/ProjectileDefinition")]
public class ProjectileDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string projectileName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("Classification")]
    public ProjectileType projectileType = ProjectileType.Standard;
    public ProjectileTrait traits = ProjectileTrait.None;
    
    [Header("Prefab")]
    [Tooltip("Prefab must contain a Bullet component.")]
    public GameObject projectilePrefab;
    
    [Header("Visual")]
    public Sprite icon;
    
    [Header("Base Stats Modifiers")]
    [Tooltip("Damage multiplier applied to turret's base damage")]
    public float damageMultiplier = 1.0f;
    [Tooltip("Speed multiplier for projectile travel speed")]
    public float speedMultiplier = 1.0f;
    
    [Header("Trait-Specific Parameters")]
    [Tooltip("For Penetrate: max enemies it can pass through (0 = infinite)")]
    public int maxPenetrations = 0;
    
    [Tooltip("For Piercing: damage per tick as % of base damage")]
    public float piercingDamagePercent = 10f;
    [Tooltip("For Piercing: duration in seconds")]
    public float piercingDuration = 3f;
    [Tooltip("For Piercing: ticks per second")]
    public float piercingTickRate = 1f;
    
    [Tooltip("For Explosive: explosion radius")]
    public float explosionRadius = 1.5f;
    [Tooltip("For Explosive: damage multiplier for AoE")]
    public float explosionDamageMultiplier = 0.5f;
    
    [Tooltip("For Slow: movement speed reduction (0.5 = 50% slower)")]
    public float slowMultiplier = 0.5f;
    [Tooltip("For Slow: duration in seconds")]
    public float slowDuration = 2f;
    
    [Tooltip("For IncoreCores/IncFragment: reward multiplier")]
    public float rewardMultiplier = 1.5f;
    
    [Tooltip("For Homing: turn rate in degrees per second")]
    public float homingTurnRate = 180f;
    
    [Tooltip("For Chain: max number of chain targets")]
    public int maxChainTargets = 3;
    [Tooltip("For Chain: chain range radius")]
    public float chainRange = 2f;
    [Tooltip("For Chain: damage multiplier per chain (0.8 = 20% reduction per jump)")]
    public float chainDamageMultiplier = 0.8f;
    
    [Header("Requirements")]
    [Tooltip("Minimum wave required to unlock")]
    public int unlockWave = 1;
    
    [Header("Upgrades")]
    [Tooltip("Maximum upgrade level for this projectile")]
    public int maxUpgradeLevel = 5;
    [Tooltip("Base cost in Prisms to upgrade (cost increases per level)")]
    public int baseUpgradeCost = 10;
    [Tooltip("Damage bonus per upgrade level (%)")]
    public float damagePerLevel = 5f;
    [Tooltip("Speed bonus per upgrade level (%)")]
    public float speedPerLevel = 3f;
    
    [Header("Stat Bonuses")]
    [Tooltip("Multiple stat bonuses this projectile provides")]
    public StatBonus[] statBonuses = new StatBonus[0];
    
    [Header("Benefits & Tradeoffs")]
    [TextArea(2, 3)]
    public string benefits = "Standard projectile";
    [TextArea(2, 3)]
    public string tradeoffs = "None";
    [Tooltip("Overall rating: -2 (major drawback) to +2 (major benefit)")]
    [Range(-2f, 2f)]
    public float overallRating = 0f;
    
    // Helper methods
    public bool HasTrait(ProjectileTrait trait) => (traits & trait) != 0;
    public bool IsType(ProjectileType type) => projectileType == type;
    
    // Calculate upgrade cost for a specific level
    public int GetUpgradeCost(int fromLevel)
    {
        if (fromLevel >= maxUpgradeLevel) return 0;
        // Cost increases: baseUpgradeCost * (level + 1)
        return baseUpgradeCost * (fromLevel + 1);
    }
    
    // Calculate stat multiplier for a given upgrade level
    public float GetDamageMultiplierAtLevel(int level)
    {
        return damageMultiplier * (1f + (damagePerLevel * level / 100f));
    }
    
    public float GetSpeedMultiplierAtLevel(int level)
    {
        return speedMultiplier * (1f + (speedPerLevel * level / 100f));
    }
    
    /// <summary>
    /// Apply all stat bonuses from this projectile to the collector
    /// </summary>
    public void ApplyStatBonuses(StatCollector collector)
    {
        if (collector == null || statBonuses == null) return;
        
        foreach (var bonus in statBonuses)
        {
            if (bonus != null && bonus.IsValid)
            {
                bonus.ApplyTo(collector);
            }
        }
    }
}
