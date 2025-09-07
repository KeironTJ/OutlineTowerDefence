using UnityEngine;

public static class SkillMath
{
    public static float EvaluateCurve(ProgressionCurve curve, float baseValue, float growth, int level, AnimationCurve custom = null)
    {
        level = Mathf.Max(level, 1);
        return curve switch
        {
            ProgressionCurve.Linear => baseValue * level,
            ProgressionCurve.Quadratic => baseValue * (level * level),
            ProgressionCurve.Exponential => baseValue * Mathf.Pow(growth, level - 1),
            ProgressionCurve.Custom => custom != null ? baseValue * custom.Evaluate(level) : baseValue * level,
            _ => baseValue * level
        };
    }
}