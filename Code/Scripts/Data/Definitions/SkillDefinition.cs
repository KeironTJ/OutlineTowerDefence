using UnityEngine;

public enum SkillCategory { Attack, Defence, Support, Turret }
public enum ProgressionCurve { Linear, Exponential, Quadratic, Custom, PercentAdditive, Additive } // see SkillMath

public enum SkillContributionKind
{
    None,
    Base,
    FlatBonus,
    Multiplier,
    Percentage
}

public enum SkillProjectionMode
{
    Multiply,
    Add
}

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // unique key
    public string displayName;
    [TextArea(2, 4)]
    public string description; // limit to ~100 characters for UI fit
    public SkillCategory category;

    [Header("Upgrade Availability")]
    public bool startsUnlocked = true;
    public bool upgradableInRound = true;

    [Header("Progression")]
    public int maxLevel = 50;
    public int maxResearchLevel = 10;

    [Header("Base Values")]
    public float baseValue = 1f;
    public string valueFormat = "";   // List of format options
    public float baseFragmentsCost = 5f;
    public float baseCoresCost = 10f;

    [Header("Unlock Cost")]
    public string prerequisiteSkillId;
    public float coresToUnlock = 500f;

    [Header("Prerequisite (optional)")]
    [Tooltip("If set, this skill only becomes active when the specified turret id is unlocked (TurretDefinition.id).")]
    public string requiredTurretId;

    [Header("Curves")]
    public ProgressionCurve valueCurve = ProgressionCurve.Exponential;
    public float valueGrowth = 1.15f;          // used for exponential
    public ProgressionCurve fragmentsCostCurve = ProgressionCurve.Exponential;
    public float fragmentsCostGrowth = 1.25f;
    public ProgressionCurve coresCostCurve = ProgressionCurve.Exponential;
    public float coresCostGrowth = 1.35f;

    public AnimationCurve customValueCurve;
    public AnimationCurve customCostCurveFragments;
    public AnimationCurve customCostCurveCores;

    [Header("Stat Mapping")]
    public StatId primaryStat = StatId.Count;
    public SkillContributionKind statContributionKind = SkillContributionKind.Base;
    public SkillProjectionMode projectionMode = SkillProjectionMode.Multiply;
    [Tooltip("Multiplier applied when pipelining this skill into the stat system.")]
    public float pipelineScale = 1f;
    [Tooltip("Optional display scaling (e.g. convert fraction to percentage by setting to 100).")]
    public float displayScale = 1f;
    [Tooltip("Minimum allowed value when pushing into the stat pipeline. Uses scaled value.")]
    public float pipelineMin = float.NegativeInfinity;
    [Tooltip("Maximum allowed value when pushing into the stat pipeline. Uses scaled value.")]
    public float pipelineMax = float.PositiveInfinity;
    [Tooltip("Use GetValueSafe (default 1) instead of GetValue when the skill might be undefined but treated as multiplier.")]
    public bool useSafeValue = false;
    [Tooltip("If true, the contribution only applies once the skill is unlocked (persistent or round).")]
    public bool requiresUnlockForContribution = false;

    public bool HasStatMapping => primaryStat != StatId.Count;
}