using UnityEngine;

public enum SkillCategory { Attack, Defence, Support, Turret }
public enum ProgressionCurve { Linear, Exponential, Quadratic, Custom, PercentAdditive } // see SkillMath

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public string id;                 // unique key
    public string displayName;
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
    // Skill required to unlock this one (optional)
    
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
}