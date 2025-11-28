using System;

[Serializable]
public class EnemyKillEntry
{
    public string definitionId;
    public EnemyTier tier;
    public string family;
    public EnemyTrait traits;
    public int count;
}
