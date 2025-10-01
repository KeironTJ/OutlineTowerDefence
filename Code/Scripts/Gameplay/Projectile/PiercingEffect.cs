using UnityEngine;
using System.Collections;

// Damage over time effect for Piercing trait
public class PiercingEffect : MonoBehaviour
{
    private float damagePerTick;
    private float duration;
    private float tickRate;
    private Enemy targetEnemy;
    private float elapsed = 0f;
    private float nextTickTime = 0f;

    public void Initialize(float damagePerTick, float duration, float tickRate, Enemy enemy)
    {
        this.damagePerTick = damagePerTick;
        this.duration = duration;
        this.tickRate = tickRate;
        this.targetEnemy = enemy;
        nextTickTime = 1f / tickRate;
    }

    private void Update()
    {
        if (targetEnemy == null)
        {
            Destroy(this);
            return;
        }

        elapsed += Time.deltaTime;
        nextTickTime -= Time.deltaTime;

        if (nextTickTime <= 0f)
        {
            targetEnemy.TakeDamage(damagePerTick);
            nextTickTime = 1f / tickRate;
        }

        if (elapsed >= duration)
        {
            Destroy(this);
        }
    }
}
