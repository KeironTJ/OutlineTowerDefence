using UnityEngine;

public interface IEnemyRuntime
{
    void InitStats(float health, float speed, float damage, float damageInterval);
    void SetRewards(int fragments, int cores, int prisms, int loops);
    void SetTarget(Tower tower);
    void SetDefinitionId(string id);
    void CacheDefinitionMeta(EnemyTier tier, string family, EnemyTrait traits);
}