using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    private Vector2 moveDirection;
    private float bulletSpeed;

    // Damage / Crit
    private float baseDamage;
    private float critChance01;    // 0..1 (e.g., 1% => 0.01)
    private float critMultiplier = 1f; // ≥1
    private bool critRolled;
    private bool isCrit;

    public void SetTarget(Transform target)
    {
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

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDirection * bulletSpeed;

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

        if (!critRolled)
        {
            isCrit = Random.value < critChance01;
            critRolled = true;
        }

        float finalDamage = baseDamage * (isCrit ? critMultiplier : 1f);
        enemy.TakeDamage(finalDamage);

        //Debug.Log($"Bullet hit: {baseDamage} base, {(isCrit ? "CRIT x" + critMultiplier : "no crit")} => {finalDamage} damage.");

        // TODO: trigger crit VFX/SFX if (isCrit)

        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}