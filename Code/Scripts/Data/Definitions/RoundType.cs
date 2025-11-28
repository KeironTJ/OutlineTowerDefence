using UnityEngine;

[CreateAssetMenu(menuName = "TD/Round Type")]
public class RoundType : ScriptableObject
{
    [Header("Wave Timeline")]
    public float baseWaveDuration = 30f;
    public bool variableWaveDuration = false;
    public AnimationCurve waveDurationCurve = AnimationCurve.Linear(1, 30, 100, 45); // wave -> seconds
    public float breakDuration = 5f;

    [Header("Budget (Enemy Points)")]
    public AnimationCurve budgetCurve = AnimationCurve.Linear(1, 12, 100, 600); // wave -> base budget
    public float budgetGrowthFactor = 1.10f;   // mild exponential overlay
    public float minBudget = 6f;

    [Header("Scaling Curves (Multipliers at wave X)")]
    public AnimationCurve healthCurve = AnimationCurve.Linear(1, 1, 100, 18);
    public AnimationCurve speedCurve  = AnimationCurve.Linear(1, 1, 100, 2.0f);
    public AnimationCurve damageCurve = AnimationCurve.Linear(1, 1, 100, 7);
    public AnimationCurve rewardCurve = AnimationCurve.Linear(1, 1, 100, 6);

    [Header("Scaling Curve Interpretation")]
    [Tooltip("Base for health scaling: multiplier = base^(wave * curveValue).")]
    public float healthCurveBase = 2f;
    [Tooltip("Base for speed scaling: multiplier = base^(wave * curveValue).")]
    public float speedCurveBase = 1.2f;
    [Tooltip("Base for damage scaling: multiplier = base^(wave * curveValue).")]
    public float damageCurveBase = 1.3f;
    [Tooltip("Base for reward scaling: multiplier = base^(wave * curveValue).")]
    public float rewardCurveBase = 1.1f;

    [Header("Spawn Rate Envelope (optional assist)")]
    public float minSpawnInterval = 0.15f;
    public float maxSpawnInterval = 0.6f; // early game
    public AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 1, 1, 0); // 0..1 normalized progress

    [Header("Boss")]
    public int bossEvery = 10;
    public float bossHealthMultiplier = 1.1f;
    public float bossRewardMultiplier = 1.1f;

    [Header("Elites")]
    public AnimationCurve eliteChanceCurve = AnimationCurve.Linear(1, 0f, 100, 0.3f);

    public float GetWaveDuration(int wave)
        => variableWaveDuration ? waveDurationCurve.Evaluate(wave) : baseWaveDuration;

    public float GetBudget(int wave)
    {
        float baseB = budgetCurve.Evaluate(wave);
        float scaled = baseB * Mathf.Pow(budgetGrowthFactor, wave * 0.15f);
        return Mathf.Max(minBudget, scaled);
    }

    public float GetHealthMultiplier(int wave) => EvaluateScalingCurve(healthCurve, healthCurveBase, wave, 1f);
    public float GetSpeedMultiplier(int wave) => EvaluateScalingCurve(speedCurve, speedCurveBase, wave, 1f);
    public float GetDamageMultiplier(int wave) => EvaluateScalingCurve(damageCurve, damageCurveBase, wave, 1f);
    public float GetRewardMultiplier(int wave) => EvaluateScalingCurve(rewardCurve, rewardCurveBase, wave, 1f);

    /// <summary>
    /// Converts a wave-indexed AnimationCurve into a multiplier by treating the sample as a coefficient in the exponent.
    /// Designers author small key values; the runtime does base^(wave * value) to produce exponential growth.
    /// Example: base = 2, wave = 5, curve value = 1.3 => 2^(5 * 1.3).
    /// </summary>
    private float EvaluateScalingCurve(AnimationCurve curve, float exponentBase, int wave, float fallback)
    {
        if (curve == null) return fallback;

        float baseValue = Mathf.Max(0.0001f, exponentBase);
        float clampedWave = Mathf.Max(0f, wave);
        float sample = Mathf.Max(0f, curve.Evaluate(wave));

        if (clampedWave == 0f || sample == 0f)
            return 1f;

        float exponent = clampedWave * sample;
        return Mathf.Pow(baseValue, exponent);
    }
}