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
    [SerializeField] private float fragments = 1f;
    [SerializeField] private float cores = 0f;
    [SerializeField] private float prisms = 0f;
    [SerializeField] private float loops = 0f;

    [Header("Enemy Type")]
    [SerializeField] private EnemyType type; // Set in the prefab
    [SerializeField] private EnemySubtype subtype; // Set in the prefab

    [SerializeField] private GameObject deathEffectPrefab; // Assign EnemyDeathEffect in Inspector

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
        this.fragments *= rewardModifier;
        this.cores *= rewardModifier;
        this.prisms *= rewardModifier;
        this.loops *= rewardModifier;
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

    private void OnCollisionEnter2D(Collision2D col)
    {
        // find a Tower on the collided object or any of its parents
        var tower = col.collider.GetComponentInParent<Tower>();
        if (tower != null)
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

            // Payloads
            var enemyPayload = new EnemyDestroyedEvent(type, subtype);
            var currencyPayload = new CurrencyEarnedEvent(fragments, cores, prisms, loops);

            EventManager.TriggerEvent(EventNames.EnemyDestroyed, enemyPayload);
            EventManager.TriggerEvent(EventNames.CurrencyEarned, currencyPayload);
            Die();
        }
    }

    public void Die()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                //main.startColor = Color.white; // fallback

                // Emit for each currency type
                EmitCurrencyParticles(ps, fragments, GetColorForCurrency(CurrencyType.Fragments));
                EmitCurrencyParticles(ps, cores, GetColorForCurrency(CurrencyType.Cores));
                EmitCurrencyParticles(ps, prisms, GetColorForCurrency(CurrencyType.Prisms));
                EmitCurrencyParticles(ps, loops, GetColorForCurrency(CurrencyType.Loops));
            }
        }

        Destroy(gameObject);
    }

    private void EmitCurrencyParticles(ParticleSystem ps, float amount, Color color)
    {
        if (amount > 0)
        {
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.startSize = Random.Range(0.5f, 0.8f);
            emitParams.startColor = new Color(color.r, color.g, color.b, Random.Range(0.7f, 1f));

            // Use logarithmic scaling and cap the max particles
            int minParticles = 5;
            int maxParticles = 30;
            int particlesToEmit = Mathf.Clamp(Mathf.RoundToInt(Mathf.Log10(amount + 1) * 5), minParticles, maxParticles);

            ps.Emit(emitParams, particlesToEmit);
        }
    }

    private Color GetColorForCurrency(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Fragments: return Color.cyan;
            case CurrencyType.Cores: return Color.yellow;
            case CurrencyType.Prisms: return Color.magenta;
            case CurrencyType.Loops: return Color.green;
            default: return Color.white;
        }
    }

    // Getters
    public float GetFragments() => fragments;
    public float GetCores() => cores;
    public float GetPrisms() => prisms;
    public float GetLoops() => loops;

    public EnemyType GetEnemyType() => type;
    public EnemySubtype GetEnemySubtype() => subtype;

}
