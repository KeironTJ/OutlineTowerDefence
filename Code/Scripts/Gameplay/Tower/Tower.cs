using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class Tower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Attributes")]
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isHealing = false;
    [SerializeField] private bool towerAlive = true;

    // Events
    public event System.Action<float, float> HealthChanged; // (current,max)
    public event System.Action TowerDestroyed;

    // Coroutines
    private Coroutine healingCoroutine;

    // Managers / Services
    private RoundManager roundManager;
    private EnemySpawner enemySpawner;
    private UIManager uiManager;
    private TowerStatPipeline statPipeline;
    private TowerStatBundle cachedStats = TowerStatBundle.Empty;
    private bool pipelineSubscribed;

    private float lastKnownMaxHealth = 0f;

    public float GetCurrentHealth() => currentHealth;
    public float MaxHealth => GetMaxHealth();
    public void ForceHealthEvent() => InvokeHealthChanged();


    private void OnEnable()
    {
        EnsurePipelineHook();
    }

    private void OnDisable()
    {
        if (statPipeline != null && pipelineSubscribed)
        {
            statPipeline.StatsRebuilt -= OnPipelineStatsRebuilt;
            pipelineSubscribed = false;
        }
    }

    public void Initialize(RoundManager roundManager, EnemySpawner enemySpawner, UIManager uiManager)
    {
        this.roundManager = roundManager;
        this.enemySpawner = enemySpawner;
        this.uiManager = uiManager;

        EnsurePipelineHook();

        lastKnownMaxHealth = GetMaxHealth();
        currentHealth = lastKnownMaxHealth;
        InvokeHealthChanged();
    }

    private void Update()
    {
        if (!towerAlive) return;
        ManageHealth();
    }

    private float GetMaxHealth()
    {
        return Mathf.Max(1f, cachedStats.MaxHealth);
    }

    private void OnMaxHealthChanged()
    {
        float newMax = GetMaxHealth();
        if (lastKnownMaxHealth <= 0f)
        {
            lastKnownMaxHealth = newMax;
            InvokeHealthChanged();
            return;
        }
        float pct = currentHealth / lastKnownMaxHealth;
        currentHealth = Mathf.Clamp(newMax * pct, 0f, newMax);
        lastKnownMaxHealth = newMax;
        InvokeHealthChanged();
    }

    private void InvokeHealthChanged()
    {
        float max = GetMaxHealth();
        HealthChanged?.Invoke(currentHealth, max);
    }

    // --- Healing ---
    private IEnumerator HealCurrentHealth()
    {
        isHealing = true;
        healingCoroutine = null;

        while (towerAlive && currentHealth < GetMaxHealth())
        {
            float healSpeed = Mathf.Max(0f, cachedStats.HealPerSecond);
            if (healSpeed <= 0f) break;
            currentHealth = Mathf.Min(currentHealth + healSpeed * Time.deltaTime, GetMaxHealth());
            InvokeHealthChanged();
            yield return null;
        }

        isHealing = false;
        healingCoroutine = null;
    }

    private void StartHealing()
    {
        if (!towerAlive || isHealing || healingCoroutine != null) return;
        healingCoroutine = StartCoroutine(HealCurrentHealth());
    }

    private void StopHealing()
    {
        if (healingCoroutine != null)
            StopCoroutine(healingCoroutine);
        healingCoroutine = null;
        isHealing = false;
    }

    // --- Damage / Death ---
    public void AddHealth(float delta)
    {
        float max = GetMaxHealth();
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, max);
        InvokeHealthChanged();
    }

    public void SetHealthAbsolute(float value)
    {
        float max = GetMaxHealth();
        currentHealth = Mathf.Clamp(value, 0f, max);
        InvokeHealthChanged();
    }

    public void TakeDamage(float dmg)
    {
        if (dmg <= 0f || !towerAlive) return;

        float armorPct = Mathf.Clamp01(cachedStats.ArmorPercent);

        float reducedDamage = dmg * (1f - armorPct);
        AddHealth(-reducedDamage);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            InvokeHealthChanged();
            OnTowerDestroyed();
        }
    }

    public void OnTowerDestroyed()
    {
        if (!towerAlive) return;
        towerAlive = false;
        StopHealing();
        InvokeHealthChanged();
        TowerDestroyed?.Invoke();
        enabled = false;
    }

    private void ManageHealth()
    {
        if (currentHealth <= 0f && towerAlive)
        {
            currentHealth = 0f;
            OnTowerDestroyed();
            return;
        }

        if (currentHealth < GetMaxHealth() && towerAlive)
            StartHealing();
    }

    private void EnsurePipelineHook()
    {
        var pipeline = TowerStatPipeline.Instance;
        if (pipeline == null)
            return;

        if (pipelineSubscribed && statPipeline == pipeline)
        {
            cachedStats = pipeline.CurrentBundle;
            if (lastKnownMaxHealth > 0f)
                OnMaxHealthChanged();
            return;
        }

        if (pipelineSubscribed && statPipeline != null)
            statPipeline.StatsRebuilt -= OnPipelineStatsRebuilt;

        statPipeline = pipeline;
        statPipeline.StatsRebuilt -= OnPipelineStatsRebuilt; // avoid duplicate handlers
        statPipeline.StatsRebuilt += OnPipelineStatsRebuilt;
        pipelineSubscribed = true;

        cachedStats = statPipeline.CurrentBundle;
        if (lastKnownMaxHealth > 0f)
            OnMaxHealthChanged();
    }

    private void OnPipelineStatsRebuilt(TowerStatBundle bundle)
    {
        cachedStats = bundle;
        OnMaxHealthChanged();
    }
}
