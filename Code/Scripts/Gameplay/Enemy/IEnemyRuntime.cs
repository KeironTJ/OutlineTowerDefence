using UnityEngine;

public interface IEnemyRuntime
{
    void InitStats(float health, float speed, float damage, float damageInterval);
    void SetRewards(int fragments, int cores, int prisms, int loops);
    void SetTarget(Tower tower);
}