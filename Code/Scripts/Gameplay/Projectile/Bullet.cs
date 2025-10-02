using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    private Collider2D col; // cache own collider

    private Vector2 moveDirection;
    private float bulletSpeed;

    // origin & max travel range (set by the spawning turret)
    private Vector3 originPosition;
    private float maxRange = -1f; // <=0 means unlimited

    // Damage / Crit
    private float baseDamage;
    private float critChance01;    // 0..1 (e.g., 1% => 0.01)
    private float critMultiplier = 1f; // ≥1
    private bool critRolled;
    private bool isCrit;
    
    // Projectile Definition & Traits
    private ProjectileDefinition projectileDefinition;
    private int penetrationCount = 0;
    private List<Enemy> hitEnemies = new List<Enemy>();
    
    // For homing projectiles
    private Transform homingTarget;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void SetTarget(Transform target)
    {
        homingTarget = target;
        
        // Calculate direction once at spawn
        moveDirection = (target.position - transform.position).normalized;

        // Set initial velocity
        rb.linearVelocity = moveDirection * bulletSpeed;

        // Rotate bullet tip to face direction
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }

    public void SetSpeed(float speed)
    {
        bulletSpeed = speed;
        // If direction is already set, update velocity
        if (moveDirection != Vector2.zero)
            rb.linearVelocity = moveDirection * bulletSpeed;
    }

    // Back-compat: if called, treat as base damage without crit
    public void SetDamage(float damage)
    {
        baseDamage = damage;
        critChance01 = 0f;
        critMultiplier = 1f;
        critRolled = true; // no crit
        isCrit = false;
    }

    // Preferred: configure full damage parameters
    public void ConfigureDamage(float baseDamage, float critChance01, float critMultiplier, bool rollNow = false)
    {
        this.baseDamage = baseDamage;
        this.critChance01 = Mathf.Clamp01(critChance01);
        this.critMultiplier = Mathf.Max(1f, critMultiplier);

        if (rollNow)
        {
            isCrit = Random.value < this.critChance01;
            critRolled = true;
        }
        else
        {
            critRolled = false; // roll on hit
        }
    }
    
    // Set the projectile definition for trait-based behavior
    public void SetProjectileDefinition(ProjectileDefinition definition, int upgradeLevel = 0)
    {
        projectileDefinition = definition;
        if (definition != null)
        {
            // Apply damage and speed multipliers from definition with upgrade level
            baseDamage *= definition.GetDamageMultiplierAtLevel(upgradeLevel);
            bulletSpeed *= definition.GetSpeedMultiplierAtLevel(upgradeLevel);
        }
    }

    public void SetOriginAndMaxRange(Vector3 origin, float maxRange)
    {
        originPosition = origin;
        this.maxRange = maxRange;
    }

    private void FixedUpdate()
    {
        // Homing behavior
        if (projectileDefinition != null && projectileDefinition.HasTrait(ProjectileTrait.Homing) && homingTarget != null)
        {
            Vector2 targetDir = (homingTarget.position - transform.position).normalized;
            moveDirection = Vector2.MoveTowards(moveDirection, targetDir, 
                projectileDefinition.homingTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime);
            moveDirection.Normalize();
            
            // Update rotation to match direction
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
        
        rb.linearVelocity = moveDirection * bulletSpeed;

        // destroy when bullet exceeds the firing turret's range
        if (maxRange > 0f)
        {
            float sqr = (transform.position - originPosition).sqrMagnitude;
            if (sqr >= maxRange * maxRange)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Margin so the bullet is destroyed slightly outside the screen
        float margin = 0.5f; 
        Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);
        if (screenPos.x < -margin || screenPos.x > 1 + margin || screenPos.y < -margin || screenPos.y > 1 + margin)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy == null) return;
        
        // Check if we already hit this enemy (for penetration)
        if (hitEnemies.Contains(enemy)) return;

        if (!critRolled)
        {
            isCrit = Random.value < critChance01;
            critRolled = true;
        }

        float finalDamage = baseDamage * (isCrit ? critMultiplier : 1f);
        enemy.TakeDamage(finalDamage);
        hitEnemies.Add(enemy);

        // Ensure bullet continues with intended velocity to reduce deflection
        rb.linearVelocity = moveDirection * bulletSpeed;

        // Apply trait effects
        if (projectileDefinition != null)
        {
            ApplyTraitEffects(enemy, finalDamage);
        }

        // Check for penetration trait
        bool shouldDestroy = true;
        if (projectileDefinition != null && projectileDefinition.HasTrait(ProjectileTrait.Penetrate))
        {
            penetrationCount++;
            int maxPenetrations = projectileDefinition.maxPenetrations;
            
            // 0 means infinite penetration
            if (maxPenetrations == 0 || penetrationCount < maxPenetrations)
            {
                shouldDestroy = false;

                // Make the bullet ignore collisions with this enemy so it actually passes through (prevents physics deflection)
                if (col != null && other.collider != null)
                {
                    Physics2D.IgnoreCollision(col, other.collider);
                }
            }
        }
        
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
    }
    
    private void ApplyTraitEffects(Enemy enemy, float damage)
    {
        if (projectileDefinition == null) return;
        
        // Piercing - damage over time
        if (projectileDefinition.HasTrait(ProjectileTrait.Piercing))
        {
            ApplyPiercingEffect(enemy, damage);
        }
        
        // Explosive - area damage
        if (projectileDefinition.HasTrait(ProjectileTrait.Explosive))
        {
            ApplyExplosiveEffect(damage);
        }
        
        // Slow - reduce enemy speed
        if (projectileDefinition.HasTrait(ProjectileTrait.Slow))
        {
            ApplySlowEffect(enemy);
        }
        
        // Chain - jump to nearby enemies
        if (projectileDefinition.HasTrait(ProjectileTrait.Chain))
        {
            ApplyChainEffect(enemy, damage);
        }
        
        // Note: IncoreCores and IncFragment would be handled by the enemy on death
        // We could add a flag to the enemy or use an event system
    }
    
    private void ApplyPiercingEffect(Enemy enemy, float baseDamage)
    {
        if (enemy == null) return;
        
        float dotDamage = baseDamage * (projectileDefinition.piercingDamagePercent / 100f);
        float duration = projectileDefinition.piercingDuration;
        float tickRate = projectileDefinition.piercingTickRate;
        
        // Start a coroutine on the enemy to apply DoT
        var dotEffect = enemy.gameObject.AddComponent<PiercingEffect>();
        dotEffect.Initialize(dotDamage, duration, tickRate, enemy);
    }
    
    private void ApplyExplosiveEffect(float damage)
    {
        float radius = projectileDefinition.explosionRadius;
        float aoeDamage = damage * projectileDefinition.explosionDamageMultiplier;
        
        // Find all enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null && !hitEnemies.Contains(enemy))
            {
                enemy.TakeDamage(aoeDamage);
            }
        }
        
        // TODO: Spawn explosion VFX
    }
    
    private void ApplySlowEffect(Enemy enemy)
    {
        if (enemy == null) return;
        
        float slowMult = projectileDefinition.slowMultiplier;
        float duration = projectileDefinition.slowDuration;
        
        var slowEffect = enemy.gameObject.AddComponent<SlowEffect>();
        slowEffect.Initialize(slowMult, duration, enemy);
    }
    
    private void ApplyChainEffect(Enemy sourceEnemy, float damage)
    {
        int maxTargets = projectileDefinition.maxChainTargets;
        float chainRange = projectileDefinition.chainRange;
        float damageMultiplier = projectileDefinition.chainDamageMultiplier;
        
        List<Enemy> chainedEnemies = new List<Enemy> { sourceEnemy };
        Enemy currentSource = sourceEnemy;
        float currentDamage = damage * damageMultiplier;
        
        for (int i = 0; i < maxTargets && currentSource != null; i++)
        {
            // Find nearest enemy not yet chained
            Collider2D[] hits = Physics2D.OverlapCircleAll(currentSource.transform.position, chainRange);
            Enemy nextTarget = null;
            float closestDist = float.MaxValue;
            
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null && !chainedEnemies.Contains(enemy))
                {
                    float dist = Vector2.Distance(currentSource.transform.position, enemy.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nextTarget = enemy;
                    }
                }
            }
            
            if (nextTarget != null)
            {
                nextTarget.TakeDamage(currentDamage);
                chainedEnemies.Add(nextTarget);
                currentSource = nextTarget;
                currentDamage *= damageMultiplier;
                
                // TODO: Create visual chain effect between enemies
            }
            else
            {
                break;
            }
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}