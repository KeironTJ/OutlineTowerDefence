using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnPatternBase : ScriptableObject
{
    public abstract List<(float t, EnemyTypeDefinition enemy)> Build(
        WaveContext ctx,
        float waveDuration,
        float budget,
        EnemyTypeDefinition[] pool);
}