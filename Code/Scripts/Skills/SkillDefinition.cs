using UnityEngine;

public enum SkillCategory { Attack, Defence, Support, Special }
public enum ProgressionCurve { Linear, Exponential, Quadratic, Custom }

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public string id;                 // unique key
    public string displayName;
    public SkillCategory category;
    public bool startsUnlocked = true;

    [Header("Progression")]
    public int maxLevel = 50;
    public int maxResearchLevel = 10;

    [Header("Base Values")]
    public float baseValue = 1f;
    public float baseFragmentsCost = 5f;
    public float baseCoresCost = 10f;

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