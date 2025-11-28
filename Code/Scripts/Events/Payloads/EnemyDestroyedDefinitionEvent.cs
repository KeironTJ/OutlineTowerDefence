using UnityEngine;
using System;

public struct EnemyDestroyedDefinitionEvent
{
    public string definitionId;
    public EnemyTier tier;
    public string family;
    public EnemyTrait traits;
    public int wave;
    public EnemyDestroyedDefinitionEvent(string id, EnemyTier tier, string family, EnemyTrait traits, int wave)
    { definitionId=id; this.tier=tier; this.family=family; this.traits=traits; this.wave=wave; }
}
