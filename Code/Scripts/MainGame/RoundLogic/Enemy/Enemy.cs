using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    BasicEnemy,
    AdvancedEnemy,
    BossEnemy
}

public enum EnemySubtype
{
    Simple,
    Fast,
    Tank
}

public class Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Enemy Stats")]
    [SerializeField] private float health;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackDamage;

    [SerializeField] private float damageInterval = 1f; // Interval between damage applications

    [Header("Enemy Rewards")]
    [SerializeField] private float basicCreditsWorth = 1f;
    [SerializeField] private float premiumCreditsWorth = 0f;
    [SerializeField] private float luxuryCreditsWorth = 0f;
    [SerializeField] private float specialCreditsWorth = 0f;

    [Header("Enemy Type")]
    [SerializeField] private EnemyType type; // Set in the prefab
    [SerializeField] private EnemySubtype subtype; // Set in the prefab

    public EnemyType Type => type; // Expose Type as a read-only property
    public EnemySubtype Subtype => subtype; // Expose Subtype as a read-only property

    private Tower tower;
    private Transform target;
    private Coroutine damageCoroutine;
    private bool isDestroyed = false;



    public void Initialize(Tower tower, float healthModifier, float moveSpeedModifier, float attackDamageModifier, float rewardModifier = 1.1f)
    {
        // Existing initialization logic
        this.tower = tower;
        this.target = tower.transform;
        this.health *= healthModifier;
        this.moveSpeed *= moveSpeedModifier;
        this.attackDamage *= attackDamageModifier;
        this.basicCreditsWorth *= rewardModifier;
        this.premiumCreditsWorth *= rewardModifier;
        this.luxuryCreditsWorth *= rewardModifier;
    }

    private void FixedUpdate()
    {
        if (tower == null || target == null)
        {
            return;
        }

        // Move towards the tower
        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartDamage();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StopDamage();
        }
    }

    private void StartDamage()
    {
        if (damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(ApplyDamageOverTime(attackDamage, damageInterval));
        }
    }

    private IEnumerator ApplyDamageOverTime(float attackDamage, float interval)
    {
        while (true)
        {
            tower.TakeDamage(attackDamage);
            yield return new WaitForSeconds(interval);
        }
    }

    public void StopDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    public void TakeDamage(float dmg)
    {
        health -= dmg;

        if (health <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            StopDamage(); // Stop applying damage to the tower
            EventManager.TriggerEvent(EventNames.EnemyDestroyed, this);
            EventManager.TriggerEvent(EventNames.CreditsEarned, this);
            Destroy(gameObject);

            //tower.AddCredits(basicCreditsWorth, premiumCreditsWorth, luxuryCreditsWorth);
        }
    }

    // Getters
    public float GetBasicCreditsWorth() => basicCreditsWorth;
    public float GetPremiumCreditsWorth() => premiumCreditsWorth;
    public float GetLuxuryCreditsWorth() => luxuryCreditsWorth;
    public float GetSpecialCreditsWorth() => specialCreditsWorth;

    public EnemyType GetEnemyType() => type;
    public EnemySubtype GetEnemySubtype() => subtype;

}
