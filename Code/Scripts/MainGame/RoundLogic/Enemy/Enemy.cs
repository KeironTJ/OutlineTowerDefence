using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IEnemyRuntime
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Runtime Stats")]
    [SerializeField] private float health;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float damageInterval = 1f;

    [Header("Runtime Rewards")]
    [SerializeField] private int rewardFragments;
    [SerializeField] private int rewardCores;
    [SerializeField] private int rewardPrisms;
    [SerializeField] private int rewardLoops;

    [Header("Definition Link (Meta Cache)")]
    [SerializeField] private string definitionId;
    [SerializeField] private EnemyTier cachedTier;
    [SerializeField] private string cachedFamily;
    [SerializeField] private EnemyTrait cachedTraits;

    [Header("VFX")]
    [SerializeField] private GameObject deathEffectPrefab;

    private Tower tower;
    private Transform target;
    private Coroutine damageCoroutine;
    private bool isDestroyed;

    public string DefinitionId => definitionId;
    public void SetDefinitionId(string id) => definitionId = id;
    public void CacheDefinitionMeta(EnemyTier tier, string family, EnemyTrait traits)
    {
        cachedTier = tier;
        cachedFamily = family;
        cachedTraits = traits;
    }

    // IEnemyRuntime
    public void InitStats(float health, float speed, float damage)
    {
        this.health = health;
        moveSpeed = speed;
        attackDamage = damage;
    }

    public void SetRewards(int fragments, int cores, int prisms, int loops)
    {
        rewardFragments = fragments;
        rewardCores = cores;
        rewardPrisms = prisms;
        rewardLoops = loops;
    }

    public void SetTarget(Tower tower)
    {
        this.tower = tower;
        target = tower ? tower.transform : null;
    }

    private void FixedUpdate()
    {
        if (!tower || !target) return;
        Vector2 dir = (target.position - transform.position).normalized;
#if UNITY_2022_2_OR_NEWER
        if (rb) rb.linearVelocity = dir * moveSpeed;
#else
        if (rb) rb.velocity = dir * moveSpeed;
#endif
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.GetComponentInParent<Tower>() != null)
            StartDamage();
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.collider.GetComponentInParent<Tower>() != null)
            StopDamage();
    }

    private void StartDamage()
    {
        if (damageCoroutine == null)
            damageCoroutine = StartCoroutine(ApplyDamageOverTime(attackDamage, damageInterval));
    }

    private IEnumerator ApplyDamageOverTime(float dmg, float interval)
    {
        while (true)
        {
            if (tower) tower.TakeDamage(dmg);
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
        if (isDestroyed) return;
        health -= dmg;
        if (health <= 0f)
        {
            isDestroyed = true;
            StopDamage();
            int wave = WaveManager.Instance ? WaveManager.Instance.SafeCurrentWave() : 0;

            EventManager.TriggerEvent(EventNames.RawEnemyRewardEvent, new RawEnemyRewardEvent(
                definitionId, wave,
                rewardFragments, rewardCores, rewardPrisms, rewardLoops,
                cachedTier == EnemyTier.Boss));

            if (!string.IsNullOrEmpty(definitionId))
            {
                EventManager.TriggerEvent(EventNames.EnemyDestroyedDefinition,
                    new EnemyDestroyedDefinitionEvent(definitionId, cachedTier, cachedFamily, cachedTraits, wave));
            }

            Die();
        }
    }

    private void Die()
    {
        if (deathEffectPrefab)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            if (fx.TryGetComponent<ParticleSystem>(out var ps))
            {
                EmitCurrencyParticles(ps, rewardFragments, GetCurrencyColor(CurrencyType.Fragments));
                EmitCurrencyParticles(ps, rewardCores,     GetCurrencyColor(CurrencyType.Cores));
                EmitCurrencyParticles(ps, rewardPrisms,    GetCurrencyColor(CurrencyType.Prisms));
                EmitCurrencyParticles(ps, rewardLoops,     GetCurrencyColor(CurrencyType.Loops));
            }
        }
        Destroy(gameObject);
    }

    private void EmitCurrencyParticles(ParticleSystem ps, int amount, Color color)
    {
        if (amount <= 0) return;
        var emitParams = new ParticleSystem.EmitParams
        {
            startSize = Random.Range(0.5f, 0.8f),
            startColor = new Color(color.r, color.g, color.b, Random.Range(0.7f, 1f))
        };
        int particles = Mathf.Clamp(Mathf.RoundToInt(Mathf.Log10(amount + 1) * 5), 5, 30);
        ps.Emit(emitParams, particles);
    }

    private Color GetCurrencyColor(CurrencyType t) =>
        t switch
        {
            CurrencyType.Fragments => Color.cyan,
            CurrencyType.Cores     => Color.yellow,
            CurrencyType.Prisms    => Color.magenta,
            CurrencyType.Loops     => Color.green,
            _ => Color.white
        };

    // Simple accessors if still needed elsewhere
    public int Fragments => rewardFragments;
    public int Cores => rewardCores;
    public int Prisms => rewardPrisms;
    public int Loops => rewardLoops;
}
