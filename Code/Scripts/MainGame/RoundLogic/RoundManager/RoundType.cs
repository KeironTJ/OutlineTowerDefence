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

    [Header("Spawn Rate Envelope (optional assist)")]
    public float minSpawnInterval = 0.15f;
    public float maxSpawnInterval = 0.6f; // early game
    public AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 1, 1, 0); // 0..1 normalized progress

    [Header("Boss")]
    public int bossEvery = 10;
    public float bossHealthMultiplier = 40f;
    public float bossRewardMultiplier = 25f;

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
}