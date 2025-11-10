using UnityEngine;

/// <summary>
/// Types of research that can be conducted
/// </summary>
public enum ResearchType
{
    TowerBase,
    Turret,
    Projectile,
    BaseStat
}

/// <summary>
/// Defines exponential growth curves for time and cost
/// </summary>
public enum ResearchCurveType
{
    Linear,
    Exponential,
    Quadratic,
    Custom
}

/// <summary>
/// ScriptableObject defining a researchable item
/// </summary>
[CreateAssetMenu(menuName = "Game/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ResearchType researchType;
    
    [Header("Progression")]
    public int maxLevel = 10;
    
    [Header("Time Configuration")]
    [Tooltip("Base time in seconds for level 1")]
    public float baseTimeSeconds = 60f;
    [Tooltip("Time growth factor (e.g., 1.5 for exponential)")]
    public float timeGrowthFactor = 1.5f;
    public ResearchCurveType timeCurve = ResearchCurveType.Exponential;
    [Tooltip("Optional custom curve for time scaling")]
    public AnimationCurve customTimeCurve;
    
    [Header("Cost Configuration - Cores")]
    [Tooltip("Base cores cost for level 1")]
    public float baseCoreCost = 100f;
    [Tooltip("Cores cost growth factor")]
    public float coreCostGrowthFactor = 1.5f;
    public ResearchCurveType coreCostCurve = ResearchCurveType.Exponential;
    [Tooltip("Optional custom curve for core cost scaling")]
    public AnimationCurve customCoreCostCurve;
    
    [Header("Speed-Up Configuration")]
    [Tooltip("Loops required per hour of speedup")]
    public float loopsPerHourSpeedup = 10f;
    
    [Header("Instant Complete Configuration")]
    [Tooltip("Prisms cost multiplier for instant completion (multiplied by remaining time in hours)")]
    public float prismsPerHourInstant = 50f;
    
    [Header("Prerequisites")]
    [Tooltip("Required research IDs that must be completed before this is available")]
    public string[] prerequisiteResearchIds;
    
    [Header("Effects")]
    [Tooltip("The item ID to unlock (tower base, turret, projectile ID)")]
    public string unlockTargetId;
    [Tooltip("Stat bonuses granted by this research (for BaseStat type)")]
    public StatBonus[] statBonuses;
    
    /// <summary>
    /// Calculate the time required for a specific level
    /// </summary>
    public float GetTimeForLevel(int level)
    {
        if (level <= 0) return 0f;
        
        switch (timeCurve)
        {
            case ResearchCurveType.Linear:
                return baseTimeSeconds + (level - 1) * timeGrowthFactor;
            
            case ResearchCurveType.Exponential:
                return baseTimeSeconds * Mathf.Pow(timeGrowthFactor, level - 1);
            
            case ResearchCurveType.Quadratic:
                return baseTimeSeconds * Mathf.Pow(level, 2) * timeGrowthFactor;
            
            case ResearchCurveType.Custom:
                if (customTimeCurve != null && customTimeCurve.length > 0)
                {
                    float t = Mathf.Clamp01(level / (float)maxLevel);
                    return baseTimeSeconds * customTimeCurve.Evaluate(t);
                }
                return baseTimeSeconds * Mathf.Pow(timeGrowthFactor, level - 1);
            
            default:
                return baseTimeSeconds;
        }
    }
    
    /// <summary>
    /// Calculate the cores cost for a specific level
    /// </summary>
    public float GetCoreCostForLevel(int level)
    {
        if (level <= 0) return 0f;
        
        switch (coreCostCurve)
        {
            case ResearchCurveType.Linear:
                return baseCoreCost + (level - 1) * coreCostGrowthFactor;
            
            case ResearchCurveType.Exponential:
                return baseCoreCost * Mathf.Pow(coreCostGrowthFactor, level - 1);
            
            case ResearchCurveType.Quadratic:
                return baseCoreCost * Mathf.Pow(level, 2) * coreCostGrowthFactor;
            
            case ResearchCurveType.Custom:
                if (customCoreCostCurve != null && customCoreCostCurve.length > 0)
                {
                    float t = Mathf.Clamp01(level / (float)maxLevel);
                    return baseCoreCost * customCoreCostCurve.Evaluate(t);
                }
                return baseCoreCost * Mathf.Pow(coreCostGrowthFactor, level - 1);
            
            default:
                return baseCoreCost;
        }
    }
    
    /// <summary>
    /// Calculate loops needed to speed up by specified seconds
    /// </summary>
    public float GetLoopsCostForSpeedup(float secondsToSpeedup)
    {
        float hours = secondsToSpeedup / 3600f;
        return Mathf.Ceil(hours * loopsPerHourSpeedup);
    }
    
    /// <summary>
    /// Calculate prisms needed for instant completion
    /// </summary>
    public float GetPrismsCostForInstant(float remainingSeconds)
    {
        float hours = remainingSeconds / 3600f;
        return Mathf.Ceil(hours * prismsPerHourInstant);
    }
}
