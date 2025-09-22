using UnityEngine;

[CreateAssetMenu(menuName = "TD/Enemy Type")]
public class EnemyTypeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public EnemyTier tier = EnemyTier.Basic;
    public string family = "Grunt";
    public EnemyTrait traits = EnemyTrait.None;

    [Header("Lifecycle")]
    public int unlockWave = 1;
    public int replaceFamilyFromWave = 0;

    [Header("Spawn Weight Curve")]
    public AnimationCurve weightByWave = AnimationCurve.Constant(1, 100, 1f);

    [Header("Base Stats")]
    public GameObject prefab;
    public int budgetCost = 1;
    public float baseHealth = 10;
    public float baseSpeed = 2;
    public float baseDamage = 1;

    [Header("Base Rewards (pre-scaling)")]
    public int baseFragments = 1;
    public int baseCores = 0;
    public int basePrisms = 0;
    public int baseLoops = 0;

    [Header("Scaling Profile")]
    public ScalingProfile scalingProfile = ScalingProfile.Standard;
    public enum ScalingProfile { Standard, Tank, Fast, Glass, Elite }

    public float GetWeight(int wave) => weightByWave.Evaluate(wave);

    public void ApplyToRuntime(WaveContext ctx, IEnemyRuntime runtime)
    {
        if (runtime == null) return;

        // ---- Stats ----
        float h = baseHealth * ctx.healthMult;
        float s = baseSpeed * ctx.speedMult;
        float d = baseDamage * ctx.damageMult;

        switch (scalingProfile)
        {
            case ScalingProfile.Tank:  h *= 1.6f; s *= 0.85f; break;
            case ScalingProfile.Fast:  s *= 1.45f; h *= 0.85f; break;
            case ScalingProfile.Glass: h *= 0.55f; s *= 1.25f; d *= 1.25f; break;
            case ScalingProfile.Elite: h *= 2.2f; d *= 1.7f; break;
        }

        runtime.InitStats(h, s, d);

        // ---- Rewards ----
        // Multiply all rewards by rewardMult (round down but keep at least base if >0)
        float mult = ctx.rewardMult;
        int fr = ScaleReward(baseFragments, mult);
        int co = ScaleReward(baseCores, mult);
        int pr = ScaleReward(basePrisms, mult);
        int lo = ScaleReward(baseLoops, mult);

        runtime.SetRewards(fr, co, pr, lo);
    }

    private int ScaleReward(int baseValue, float mult)
    {
        if (baseValue <= 0) return 0;
        int scaled = Mathf.FloorToInt(baseValue * mult);
        return Mathf.Max(1, scaled);
    }
}

// If not defined elsewhere, keep this here. Remove if duplicate.
public struct WaveContext
{
    public int wave;
    public float healthMult, speedMult, damageMult, rewardMult;
    public System.Random rng;
    public float eliteChance;
}