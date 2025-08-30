using System;
using UnityEngine;

[Serializable]
public struct EnemyDestroyedEvent
{
    public EnemyType type;
    public EnemySubtype subtype;

    public EnemyDestroyedEvent(EnemyType type, EnemySubtype subtype)
    {
        this.type = type;
        this.subtype = subtype;
    }
}