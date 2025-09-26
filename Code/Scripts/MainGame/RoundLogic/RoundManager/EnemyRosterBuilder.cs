using System.Collections.Generic;

public static class EnemyRosterBuilder
{
    public static void BuildTierPools(
        int wave,
        EnemyTypeDefinition[] all,
        List<EnemyTypeDefinition> basics,
        List<EnemyTypeDefinition> advanced,
        List<EnemyTypeDefinition> elites)
    {
        basics.Clear();
        advanced.Clear();
        elites.Clear();
        if (all == null) return;

        foreach (var def in all)
        {
            if (!def) continue;
            if (wave < def.unlockWave) continue;
            switch (def.tier)
            {
                case EnemyTier.Basic:   basics.Add(def); break;
                case EnemyTier.Advanced:advanced.Add(def); break;
                case EnemyTier.Elite:   elites.Add(def); break;
                case EnemyTier.Boss:    break;
            }
        }
    }

    public static EnemyTypeDefinition MaybePromote(
        int wave,
        EnemyTypeDefinition chosenBasic,
        List<EnemyTypeDefinition> advanced,
        List<EnemyTypeDefinition> elites,
        System.Random rng,
        float rampLengthWaves = 20f)
    {
        if (!chosenBasic) return chosenBasic;
        EnemyTypeDefinition result = chosenBasic;

        // Advanced
        var adv = GetFamilyReplacement(wave, chosenBasic.family, advanced);
        if (adv != null && adv.replaceFamilyFromWave > 0 && wave >= adv.replaceFamilyFromWave)
        {
            float p = Clamp01((wave - adv.replaceFamilyFromWave) / rampLengthWaves);
            if (rng.NextDouble() < p) result = adv;
        }

        // Elite
        var elite = GetFamilyReplacement(wave, chosenBasic.family, elites);
        if (elite != null && elite.replaceFamilyFromWave > 0 && wave >= elite.replaceFamilyFromWave)
        {
            float pE = Clamp01((wave - elite.replaceFamilyFromWave) / (rampLengthWaves * 1.25f));
            if (rng.NextDouble() < pE) result = elite;
        }

        return result;
    }

    private static EnemyTypeDefinition GetFamilyReplacement(
        int wave,
        string family,
        List<EnemyTypeDefinition> pool)
    {
        EnemyTypeDefinition candidate = null;
        foreach (var d in pool)
        {
            if (d.family != family) continue;
            if (wave < d.unlockWave) continue;
            candidate = d; // last wins
        }
        return candidate;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    public static EnemyTypeDefinition[] GetEligibleBosses(int wave, EnemyTypeDefinition[] all)
    {
        if (all == null) return System.Array.Empty<EnemyTypeDefinition>();
        var list = new List<EnemyTypeDefinition>();
        foreach (var def in all)
        {
            if (def == null) continue;
            if (def.tier != EnemyTier.Boss) continue;
            if (wave < def.unlockWave) continue;
            list.Add(def);
        }
        return list.ToArray();
    }
}