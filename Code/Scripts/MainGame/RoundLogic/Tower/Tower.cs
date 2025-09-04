using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Events
    public event System.Action<float, float> HealthChanged; // (current,max)
    public event System.Action TowerDestroyed;

    // Shooting
    private Transform target;
    private float timeUntilFire;

    // Coroutines
    private Coroutine healingCoroutine;

    // Managers
    private RoundManager roundManager;
    private EnemySpawner enemySpawner;
    private SkillManager skillManager;
    private UIManager uiManager;

    // Cached visuals
    private LineRenderer rangeRenderer; 
    private Material rangeMaterial;    
    private const int rangeSegments = 50;  
    private float lastRange = -1f;    

    private float lastKnownMaxHealth = 0f;

    
    // GAME SEQUENCE

    private void Update()
    {
        if (!towerAlive) return;

        DrawTargetingRange();
        ActivateShooting();
        ManageHealth();
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.SkillUpgraded, OnSkillUpgraded);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.SkillUpgraded, OnSkillUpgraded);
    }

       

    public void Initialize(RoundManager roundManager, EnemySpawner enemySpawner, SkillManager skillManager, UIManager uiManager)
    {
        this.roundManager = roundManager;
        this.enemySpawner = enemySpawner;
        this.skillManager = skillManager;
        this.uiManager = uiManager;

        if (skillManager != null)
        {
            lastKnownMaxHealth = GetMaxHealth();
            currentHealth = lastKnownMaxHealth;
            InvokeHealthChanged();
        }
        else
        {
            Debug.LogError("SkillManager not found during initialization!");
        }
    }

    // Helper: single source of truth for max health
    private float GetMaxHealth()
    {
        // prefer skillManager (should be set in Initialize)
        if (skillManager != null)
            return skillManager.GetSkillValue(skillManager.GetSkill("Health"));
        return currentHealth;
    }

    // Make intent explicit: this adds a delta to current health and clamps
    public void AddHealth(float delta)
    {
        float max = GetMaxHealth();
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, max);
        InvokeHealthChanged();
    }

    // If you need an absolute setter, expose it explicitly
    public void SetHealthAbsolute(float value)
    {
        float max = GetMaxHealth();
        currentHealth = Mathf.Clamp(value, 0f, max);
        InvokeHealthChanged();
    }

    // Called when the maximum health value changes (e.g. upgrade)
    // Preserve the current percentage of health by default.
    public void OnMaxHealthChanged()
    {
        float newMax = GetMaxHealth();

        // If we never recorded a last known max, initialize and don't attempt to scale.
        if (lastKnownMaxHealth <= 0f)
        {
            lastKnownMaxHealth = newMax;
            // Optionally, keep currentHealth as-is or set to full:
            // currentHealth = newMax;
            InvokeHealthChanged();
            return;
        }

        // Preserve current percentage relative to previous max
        float pct = (lastKnownMaxHealth > 0f) ? currentHealth / lastKnownMaxHealth : 1f;
        currentHealth = Mathf.Clamp(newMax * pct, 0f, newMax);
        lastKnownMaxHealth = newMax;
        InvokeHealthChanged();
    }

    private void OnSkillUpgraded(object eventData)
    {
        // SkillManager currently sends the Skill instance as payload
        var skill = eventData as Skill;
        if (skill == null) return;

        if (skill.skillName == "Health")
        {
            // Recompute max and preserve percentage
            OnMaxHealthChanged();
        }
    }


    // Track last known max so upgrades can compute percentage


    // Shooting / Targeting

    public void ActivateShooting()
    {
        HandleTargeting(); // Encapsulated targeting logic

        if (target == null)
        {
            return; // Exit if no target is found
        }

        if (CanShoot())
        {
            Shoot();
        }
    }

    // Tower Actions


    private void Shoot()
    {
        if (bulletPrefab == null || firingPoint == null || target == null) return; 

        GameObject bulletObj = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        bulletScript.SetTarget(target, this);
        bulletScript.SetSpeed(skillManager.GetSkillValue(skillManager.GetSkill("Bullet Speed")));
        bulletScript.SetDamage(skillManager.GetSkillValue(skillManager.GetSkill("Attack Damage")));

        EventManager.TriggerEvent(EventNames.BulletFired, bulletScript);
    }

    private void FindTarget()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range")), Vector2.zero, 0f, enemyMask);

        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(hit.transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        if (closestTarget != null && closestDistance <= skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range")))
        {
            target = closestTarget;
        }
        else
        {
            target = null;
        }
    }

    private bool CheckTargetIsInRange()
    {
        return target != null && Vector2.Distance(target.position, transform.position) <= skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range"));
    }

    private void RotateTowardsTarget()
    {
        if (target == null) return;

        float angle = Mathf.Atan2(target.position.y - transform.position.y, target.position.x - transform.position.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
        turretRotationPoint.rotation = Quaternion.RotateTowards(turretRotationPoint.rotation, targetRotation, baseRotationSpeed * Time.deltaTime);
    }

    private void DrawTargetingRange()
    {
        // update only when range changes
        if (rangeRenderer == null)
        {
            rangeRenderer = GetComponent<LineRenderer>();
            if (rangeRenderer == null)
            {
                rangeRenderer = gameObject.AddComponent<LineRenderer>();
                rangeRenderer.startWidth = 0.05f;
                rangeRenderer.endWidth = 0.05f;
                rangeRenderer.positionCount = rangeSegments;
                rangeRenderer.loop = true;
                rangeRenderer.useWorldSpace = false;
                rangeRenderer.startColor = Color.cyan;
                rangeRenderer.endColor = Color.cyan;
                // Cache material once
                rangeMaterial = new Material(Shader.Find("Sprites/Default"));
                rangeRenderer.material = rangeMaterial;
            }
        }

        float range = skillManager != null ? skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range")) : 0f;
        if (Mathf.Approximately(range, lastRange)) return; // only recompute when changed
        lastRange = range;

        float angle = 0f;
        float step = 360f / rangeSegments;
        for (int i = 0; i < rangeSegments; i++)
        {
            float rad = Mathf.Deg2Rad * angle;
            float x = Mathf.Sin(rad) * range;
            float y = Mathf.Cos(rad) * range;
            rangeRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += step;
        }
    }

    // Health Management

    private void InvokeHealthChanged()
    {
        float maxHealth = (skillManager != null)
            ? skillManager.GetSkillValue(skillManager.GetSkill("Health"))
            : currentHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Healing coroutine
    private IEnumerator HealCurrentHealth()
    {
        isHealing = true;
        healingCoroutine = null; // will be set at start by caller

        while (towerAlive && currentHealth < GetMaxHealth())
        {
            float maxHealth = GetMaxHealth(); // recalc each frame
            float healSpeed = skillManager != null ? skillManager.GetSkillValue(skillManager.GetSkill("Heal Speed")) : 0f;
            float healAmount = healSpeed * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            InvokeHealthChanged();
            yield return null;
        }

        // finished
        isHealing = false;
        healingCoroutine = null;
    }

    void StartHealing()
    {
        if (!towerAlive) return;
        if (isHealing || healingCoroutine != null) return;
        healingCoroutine = StartCoroutine(HealCurrentHealth());
    }

    private void StopHealing()
    {
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }
        isHealing = false;
    }

    // TakeDamage uses AddHealth with negative delta and invokes change
    public void TakeDamage(float attackDamage)
    {
        if (attackDamage <= 0f || !towerAlive) return;
        AddHealth(-attackDamage);

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

        enabled = false;      // stop Update loop
    }

    private void ManageHealth()
    {
        if (currentHealth <= 0 && towerAlive)
        {
            currentHealth = 0;
            OnTowerDestroyed();
        }

        if (currentHealth < skillManager.GetSkillValue(skillManager.GetSkill("Health")) && towerAlive)
        {
            StartHealing();
        }
    }

    private bool CanShoot()
    {
        float fireInterval = 1f / skillManager.GetSkillValue(skillManager.GetSkill("Attack Speed"));
        timeUntilFire += Time.deltaTime;

        if (timeUntilFire >= fireInterval)
        {
            timeUntilFire -= fireInterval; // Keeps ready to fire
            return true;
        }

        return false;
    }

    private void HandleTargeting()
    {
        FindTarget(); // Find the closest target

        if (target == null || !CheckTargetIsInRange())
        {
            target = null; // Clear target if not in range
            return;
        }

        RotateTowardsTarget(); // Rotate towards the target
    }

}
