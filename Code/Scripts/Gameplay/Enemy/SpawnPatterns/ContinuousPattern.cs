using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Spawn Pattern/Continuous")]
public class ContinuousPattern : SpawnPatternBase
{
    [Range(0.5f, 2f)] public float spacingAdjust = 1f;

    public override List<(float t, EnemyTypeDefinition enemy)> Build(
        WaveContext ctx,
        float waveDuration,
        float budget,
        EnemyTypeDefinition[] pool)
    {
        var list = new List<(float, EnemyTypeDefinition)>(64);
        if (pool == null || pool.Length == 0 || budget <= 0) return list;

        float spent = 0f;
        float cursor = 0f;

        // Rough target count = budget / averageCost
        float avgCost = 0f;
        foreach (var e in pool) avgCost += e.budgetCost;
        avgCost = Mathf.Max(1f, avgCost / pool.Length);
        int estimatedCount = Mathf.CeilToInt(budget / avgCost);
        float baseInterval = (waveDuration / Mathf.Max(1, estimatedCount)) * spacingAdjust;

        while (spent < budget && cursor < waveDuration)
        {
            var pick = pool[ctx.rng.Next(pool.Length)];
            // Elite replace
            bool makeElite = ctx.rng.NextDouble() < ctx.eliteChance;
            if (makeElite && pick.scalingProfile != EnemyTypeDefinition.ScalingProfile.Elite)
            {
                // Temporary scaling profile switch (optional)
            }

            if (spent + pick.budgetCost > budget && (budget - spent) < 1f)
                break;

            list.Add((cursor, pick));
            spent += pick.budgetCost;

            // Jitter
            float jitter = Mathf.Lerp(0.8f, 1.2f, (float)ctx.rng.NextDouble());
            cursor += baseInterval * jitter;
        }

        return list;
    }
}