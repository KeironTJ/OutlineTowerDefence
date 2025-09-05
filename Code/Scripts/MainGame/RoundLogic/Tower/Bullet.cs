using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb; 

    private Vector2 moveDirection;
    private Tower tower;
    private float bulletSpeed;
    private float attackDamage;

    public void SetTarget(Transform target, Tower _tower) 
    {
        tower = _tower;
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

    public void SetDamage(float damage)
    {
        attackDamage = damage;
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
        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}