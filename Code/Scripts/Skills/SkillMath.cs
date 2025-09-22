using UnityEngine;

public static class SkillMath
{
    public static float EvaluateCurve(ProgressionCurve curve, float baseValue, float growth, int level, AnimationCurve custom = null)
    {
        level = Mathf.Max(level, 1);
        return curve switch
        {
            ProgressionCurve.Linear          => baseValue * level,
            ProgressionCurve.Additive        => baseValue + growth * (level - 1),                     // growth = 0.5 => +0.5/level
            ProgressionCurve.Quadratic       => baseValue * (level * level),
            ProgressionCurve.Exponential     => baseValue * Mathf.Pow(growth, level - 1),              // growth = 1.05 => +5% compounded
            ProgressionCurve.Custom          => custom != null ? baseValue * custom.Evaluate(level) : baseValue * level,
            ProgressionCurve.PercentAdditive => baseValue * (1f + growth * (level - 1f)),               // growth = 0.05 => +5%/level non-compounding
            _ => baseValue * level
        };
    }

    // Optional helper if you want a direct API without enum:
    public static float EvaluatePercentAdditive(float baseValue, float percentPerLevel, int level)
        => baseValue * (1f + percentPerLevel * (Mathf.Max(level, 1) - 1f));
}

