using UnityEngine;

/// <summary>
/// Represents a single stat bonus that can be applied to the stat system.
/// Used by ScriptableObject definitions to define multiple stat bonuses.
/// </summary>
[System.Serializable]
public class StatBonus
{
    [Tooltip("The stat to modify")]
    public StatId targetStat = StatId.Count;
    
    [Tooltip("How this bonus is applied (Base, FlatBonus, Multiplier, Percentage)")]
    public SkillContributionKind contributionKind = SkillContributionKind.FlatBonus;
    
    [Tooltip("The raw bonus value (before scaling)")]
    public float value = 0f;
    
    [Tooltip("Multiplier applied before pushing to pipeline (e.g., 0.01 to convert percent to scalar)")]
    public float pipelineScale = 1f;
    
    [Tooltip("Minimum pipeline value after scaling")]
    public float pipelineMin = float.NegativeInfinity;
    
    [Tooltip("Maximum pipeline value after scaling")]
    public float pipelineMax = float.PositiveInfinity;

    /// <summary>
    /// Checks if this bonus is valid and should be applied
    /// </summary>
    public bool IsValid => targetStat != StatId.Count && contributionKind != SkillContributionKind.None;

    /// <summary>
    /// Converts the raw value to a pipeline-ready value with scaling and clamping
    /// </summary>
    public float ToPipelineValue(float rawValue)
    {
        if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
            rawValue = 0f;

        float scaled = rawValue * pipelineScale;
        
        if (!float.IsNegativeInfinity(pipelineMin))
            scaled = Mathf.Max(pipelineMin, scaled);
        
        if (!float.IsPositiveInfinity(pipelineMax))
            scaled = Mathf.Min(pipelineMax, scaled);
        
        return scaled;
    }

    /// <summary>
    /// Applies this bonus to the stat collector
    /// </summary>
    public void ApplyTo(StatCollector collector, float rawValue)
    {
        if (!IsValid || collector == null) return;

        float pipelineValue = ToPipelineValue(rawValue);

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

    /// <summary>
    /// Applies this bonus to the stat collector using its configured value
    /// </summary>
    public void ApplyTo(StatCollector collector)
    {
        ApplyTo(collector, value);
    }
}
