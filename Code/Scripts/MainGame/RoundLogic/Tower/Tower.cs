using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]   // Ensure a LineRenderer is always present
public class Tower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform turretRotationPoint;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;

    [Header("Attributes")]
    [SerializeField] private float baseRotationSpeed;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isHealing = false;
    [SerializeField] private bool towerAlive = true;

    [Header("Attack Skill Ids")]

    [SerializeField] private string attackDamageSkillId = "Attack Damage";
    [SerializeField] private string attackSpeedSkillId = "Attack Speed";
    [SerializeField] private string bulletSpeedSkillId = "Bullet Speed";
    [SerializeField] private string targetingRangeSkillId = "Targeting Range";
    [SerializeField] private string criticalChanceSkillId = "Critical Chance";
    [SerializeField] private string criticalMultiplierSkillId = "Critical Multiplier";

    [Header("Support Skill Ids")]
    [SerializeField] private string healthSkillId = "Health";
    [SerializeField] private string healSpeedSkillId = "Heal Speed";
    [SerializeField] private string armorSkillId = "Armor";
    

    //[Header("Special Skill Ids")]
    



    // Events
    public event System.Action<float, float> HealthChanged; // (current,max)
    public event System.Action TowerDestroyed;

    // Shooting
    private Transform target;
    private float timeUntilFire;

    // Coroutines
    private Coroutine healingCoroutine;

    // Managers / Services
    private RoundManager roundManager;
    private EnemySpawner enemySpawner;
    private UIManager uiManager;
    private SkillService skillService;

    // Cached visuals
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;
    private const int rangeSegments = 50;
    private float lastRange = -1f;

    private float lastKnownMaxHealth = 0f;

    public float GetCurrentHealth() => currentHealth;
    public float MaxHealth => GetMaxHealth();  
    public void ForceHealthEvent() => InvokeHealthChanged();

    private void Awake()
    {
        EnsureRangeRendererConfigured();
    }

    private void OnEnable()
    {
        if (SkillService.Instance != null)
            SkillService.Instance.SkillUpgraded += OnSkillUpgraded;
        EnsureRangeRendererConfigured();
    }

    private void OnDisable()
    {
        if (SkillService.Instance != null)
            SkillService.Instance.SkillUpgraded -= OnSkillUpgraded;
    }

    public void Initialize(RoundManager roundManager, EnemySpawner enemySpawner, UIManager uiManager)
    {
        this.roundManager = roundManager;
        this.enemySpawner = enemySpawner;
        this.uiManager = uiManager;
        skillService = SkillService.Instance;

        lastKnownMaxHealth = GetMaxHealth();
        currentHealth = lastKnownMaxHealth;
        InvokeHealthChanged();
    }

    private void Update()
    {
        if (!towerAlive) return;

        DrawTargetingRange();
        ActivateShooting();
        ManageHealth();
    }

    // --- Skill Helpers ---
    private float GetSkillValue(string id)
    {
        return (skillService != null) ? skillService.GetValue(id) : 0f;
    }

    private float GetMaxHealth()
    {
        return Mathf.Max(1f, GetSkillValue(healthSkillId));
    }

    // --- Health Management ---
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

    public void OnMaxHealthChanged()
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

    private void OnSkillUpgraded(string skillId)
    {
        if (skillId == healthSkillId)
            OnMaxHealthChanged();
    }

    private void InvokeHealthChanged()
    {
        float max = GetMaxHealth();
        HealthChanged?.Invoke(currentHealth, max);
    }

    // --- Shooting / Targeting ---
    public void ActivateShooting()
    {
        HandleTargeting();
        if (target == null) return;
        if (CanShoot()) Shoot();
    }

    private bool CanShoot()
    {
        float attackSpeed = Mathf.Max(0.0001f, GetSkillValue(attackSpeedSkillId));
        float fireInterval = 1f / attackSpeed;

        timeUntilFire += Time.deltaTime;
        if (timeUntilFire >= fireInterval)
        {
            timeUntilFire -= fireInterval;
            return true;
        }
        return false;
    }

    private float GetCritChance01()
    {
        if (skillService == null) return 0f;
        bool unlocked = skillService.IsUnlocked(criticalChanceSkillId, persistentOnly: false);
        if (!unlocked) return 0f;

        float raw = GetSkillValue(criticalChanceSkillId); // authored as percent, 1 => 1%
        return Mathf.Clamp01(raw * 0.01f);
    }

    private float GetCritMultiplier()
    {
        if (skillService == null) return 1f;
        bool unlocked = skillService.IsUnlocked(criticalMultiplierSkillId, persistentOnly: false);
        if (!unlocked) return 1f;

        return Mathf.Max(1f, GetSkillValue(criticalMultiplierSkillId));
    }

    private void Shoot()
    {
        if (!bulletPrefab || !firingPoint || !target) return;

        GameObject bulletObj = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        if (bulletScript)
        {
            bulletScript.SetTarget(target, this);
            bulletScript.SetSpeed(GetSkillValue(bulletSpeedSkillId));

            float baseDamage = GetSkillValue(attackDamageSkillId);
            float critChance = GetCritChance01();
            float critMult   = GetCritMultiplier();

            // Pass damage and crit params to bullet; crit is resolved on hit
            bulletScript.ConfigureDamage(baseDamage, critChance, critMult, rollNow: false);
        }

        EventManager.TriggerEvent(EventNames.BulletFired, bulletScript);
    }

    private void HandleTargeting()
    {
        FindTarget();
        if (target == null || !CheckTargetIsInRange())
        {
            target = null;
            return;
        }
        RotateTowardsTarget();
    }

    private void FindTarget()
    {
        float range = GetSkillValue(targetingRangeSkillId);
        if (range <= 0f) { target = null; return; }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, range, Vector2.zero, 0f, enemyMask);
        float closest = float.PositiveInfinity;
        Transform best = null;

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(hit.transform.position, transform.position);
            if (dist < closest)
            {
                closest = dist;
                best = hit.transform;
            }
        }

        if (best != null && closest <= range)
            target = best;
        else
            target = null;
    }

    private bool CheckTargetIsInRange()
    {
        return target && Vector2.Distance(target.position, transform.position) <= GetSkillValue(targetingRangeSkillId);
    }

    private void RotateTowardsTarget()
    {
        if (!target) return;
        float angle = Mathf.Atan2(target.position.y - transform.position.y,
                                  target.position.x - transform.position.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        turretRotationPoint.rotation = Quaternion.RotateTowards(
            turretRotationPoint.rotation, targetRot, baseRotationSpeed * Time.deltaTime);
    }

    // --- RANGE RENDERER SETUP ---
    private void EnsureRangeRendererConfigured()
    {
        if (rangeRenderer == null)
        {
            if (!TryGetComponent(out rangeRenderer))
                rangeRenderer = gameObject.AddComponent<LineRenderer>(); // fallback (should not happen due to RequireComponent)
        }

        // If somehow still missing, bail safely
        if (rangeRenderer == null) return;

        rangeRenderer.enabled = true;
        rangeRenderer.loop = true;
        rangeRenderer.useWorldSpace = false;
        rangeRenderer.startWidth = 0.05f;
        rangeRenderer.endWidth = 0.05f;
        rangeRenderer.positionCount = rangeSegments;
        rangeRenderer.startColor = rangeRenderer.endColor = Color.cyan;

        if (rangeMaterial == null)
            rangeMaterial = new Material(Shader.Find("Sprites/Default"));
        rangeRenderer.material = rangeMaterial;
    }

    private void DrawTargetingRange()
    {
        // Ensure configured (handles cases where component was removed at runtime)
        EnsureRangeRendererConfigured();
        if (rangeRenderer == null) return;

        float range = GetSkillValue(targetingRangeSkillId);
        if (Mathf.Approximately(range, lastRange)) return;
        lastRange = range;

        if (range <= 0f)
        {
            rangeRenderer.enabled = false;
            return;
        }

        rangeRenderer.enabled = true;
        float step = 360f / rangeSegments;
        for (int i = 0; i < rangeSegments; i++)
        {
            float ang = Mathf.Deg2Rad * (i * step);
            float x = Mathf.Sin(ang) * range;
            float y = Mathf.Cos(ang) * range;
            rangeRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    // --- Healing ---
    private IEnumerator HealCurrentHealth()
    {
        isHealing = true;
        healingCoroutine = null;

        while (towerAlive && currentHealth < GetMaxHealth())
        {
            float healSpeed = GetSkillValue(healSpeedSkillId);
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
    public void TakeDamage(float dmg)
    {
        if (dmg <= 0f || !towerAlive) return;

        float armorPct = 0f;
        bool armorUnlocked = skillService != null && skillService.IsUnlocked(armorSkillId, persistentOnly: false);
        if (armorUnlocked)
        {
            float armorRaw = GetSkillValue(armorSkillId);     // authored as percent, e.g. 1 => 1%
            armorPct = Mathf.Clamp01(armorRaw * 0.01f);       // Option B: always percent-based
        }

        float reducedDamage = dmg * (1f - armorPct);

        Debug.Log($"Tower takes {reducedDamage} damage (raw {dmg}, armor {(armorPct * 100f):0.#}% | unlocked={armorUnlocked})");

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
}
