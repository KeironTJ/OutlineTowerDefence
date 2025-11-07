using UnityEngine;

/// <summary>
/// Shared helpers for converting authored stat values into pipeline-ready numbers.
/// Ensures percent-based stats authored as whole numbers are normalised automatically.
/// </summary>
public static class StatPipelineScaling
{
    private const float PercentScale = 0.01f;
    private const float ApproximateEpsilon = 0.0001f;

    public static float ApplyScaling(StatId statId, float rawValue, float configuredScale)
    {
        if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
            rawValue = 0f;

        float scaled = rawValue * configuredScale;

        if (RequiresPercentNormalisation(statId) && ApproximatelyOne(configuredScale))
        {
            scaled = rawValue * PercentScale;
        }

        return scaled;
    }

    public static float ApplyClamping(float value, float min, float max)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            value = 0f;

        if (!float.IsNegativeInfinity(min))
            value = Mathf.Max(min, value);

        if (!float.IsPositiveInfinity(max))
            value = Mathf.Min(max, value);

        return value;
    }

    private static bool RequiresPercentNormalisation(StatId statId)
    {
        return statId == StatId.CritChance;
    }

    private static bool ApproximatelyOne(float value)
    {
        return Mathf.Abs(value - 1f) <= ApproximateEpsilon;
    }
}
