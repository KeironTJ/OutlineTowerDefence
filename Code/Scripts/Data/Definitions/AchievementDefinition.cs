using UnityEngine;

public enum AchievementType 
{ 
    KillEnemies, 
    ShootProjectiles, 
    CompleteWaves, 
    CompleteRounds,
    ReachDifficulty,
    EarnCurrency,
    SpendCurrency,
    UnlockTurret,
    UnlockProjectile,
    UpgradeProjectile
}

public enum AchievementCategory 
{ 
    Combat, 
    Progression, 
    Economy, 
    Mastery 
}

[CreateAssetMenu(menuName = "Rewards/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public AchievementCategory category = AchievementCategory.Combat;
    public AchievementType type;

    [Header("Tier System (Stackable Milestones)")]
    [Tooltip("Multiple tiers for the same achievement (e.g., 10, 100, 1000 kills)")]
    public AchievementTier[] tiers;

    [Header("Filters (Type-Specific)")]
    [Tooltip("For KillEnemies: leave empty for any enemy, or specify enemy definition ID")]
    public string targetDefinitionId;
    public bool useTierFilter = false;
    public EnemyTier targetEnemyTier = EnemyTier.Basic;
    public bool useFamilyFilter = false;
    public string targetFamily;
    public bool useTraitFilter = false;
    public EnemyTrait targetTraits = EnemyTrait.None;

    [Header("Filters for Currency")]
    public CurrencyType currencyType = CurrencyType.Cores;

    [Header("Filters for Projectiles")]
    public string targetProjectileId;
    public ProjectileTrait targetProjectileTrait = ProjectileTrait.None;

    [Header("UI")]
    public Sprite icon;
    public bool isHidden = false; // Hidden until unlocked

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = id;

        // Ensure tiers are sorted by target amount
        //if (tiers != null && tiers.Length > 1)
        //{
        //    System.Array.Sort(tiers, (a, b) => a.targetAmount.CompareTo(b.targetAmount));
        //}
    }
#endif

    public bool Matches(EnemyDestroyedDefinitionEvent e)
    {
        if (type != AchievementType.KillEnemies) return false;
        
        if (!string.IsNullOrEmpty(targetDefinitionId) && e.definitionId != targetDefinitionId)
            return false;
        if (useTierFilter && e.tier != targetEnemyTier)
            return false;
        if (useFamilyFilter && e.family != targetFamily)
            return false;
        if (useTraitFilter && (e.traits & targetTraits) != targetTraits)
            return false;

        return true;
    }
}

[System.Serializable]
public class AchievementTier
{
    public int tierLevel = 1;
    public float targetAmount = 100f;
    
    [Header("Rewards")]
    public AchievementReward[] rewards;

    [Header("UI")]
    public string tierName; // e.g., "Bronze", "Silver", "Gold"
    [TextArea] public string tierDescription;
}

[System.Serializable]
public class AchievementReward
{
    public AchievementRewardType rewardType;
    public string rewardId; // For unlocks (turretId, projectileId, etc.)
    public int amount; // For currency rewards
    public CurrencyType currencyType; // For currency rewards
}

public enum AchievementRewardType
{
    Currency,
    UnlockTurret,
    UnlockProjectile,
    UnlockTowerBase,
    StatBonus // For future use
}
