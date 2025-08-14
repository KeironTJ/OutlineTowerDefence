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

    
    // GAME SEQUENCE

    private void Update()
    {
        if (!towerAlive) return;

        DrawTargetingRange();
        ActivateShooting();
        ManageHealth();
    }

    public void Initialize(RoundManager roundManager, EnemySpawner enemySpawner, SkillManager skillManager, UIManager uiManager)
    {
        this.roundManager = roundManager;
        this.enemySpawner = enemySpawner;
        this.skillManager = skillManager; 
        this.uiManager = uiManager;

        if (skillManager != null)
        {
            currentHealth = skillManager.GetSkillValue(skillManager.GetSkill("Health"));
            InvokeHealthChanged();
        }
        else
        {
            Debug.LogError("SkillManager not found during initialization!");
        }
    }

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
        if (bulletPrefab == null || firingPoint == null || target == null) return; // NEW guards

        GameObject bulletObj = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        bulletScript.SetTarget(target, this);
        bulletScript.SetSpeed(skillManager.GetSkillValue(skillManager.GetSkill("Bullet Speed")));
        bulletScript.SetDamage(skillManager.GetSkillValue(skillManager.GetSkill("Attack Damage")));
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
        // NEW: cache renderer/material; update only when range changes
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
        float maxHealth = (roundManager != null)
            ? roundManager.GetSkillValue(roundManager.GetSkill("Health"))
            : currentHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void SetCurrentHealth(float health)
    {
        float maxHealth = roundManager.GetSkillValue(roundManager.GetSkill("Health"));
        currentHealth = Mathf.Clamp(currentHealth + health, 0f, maxHealth);
        InvokeHealthChanged();                          
    }

    public void TakeDamage(float attackDamage)
    {
        if (attackDamage <= 0f || !towerAlive) return;   
        currentHealth -= attackDamage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            InvokeHealthChanged();                      
            OnTowerDestroyed();
            return;
        }
        InvokeHealthChanged();                           
    }

    void StartHealing()
    {
        float maxHealth = roundManager.GetSkillValue(roundManager.GetSkill("Health"));

        if (currentHealth < maxHealth && towerAlive)
        {
            isHealing = true;

            if (healingCoroutine == null)
            {
                healingCoroutine = StartCoroutine(HealCurrentHealth());
            }
        }
    }

    private void StopHealing()
    {
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
            isHealing = false; // Reset the flag if you stop healing prematurely 
        }
    }

    private IEnumerator HealCurrentHealth()
    {
        float maxHealth = skillManager.GetSkillValue(skillManager.GetSkill("Health"));

        while (currentHealth < maxHealth)
        {
            maxHealth = skillManager.GetSkillValue(skillManager.GetSkill("Health"));
            float healSpeed = skillManager.GetSkillValue(skillManager.GetSkill("Heal Speed"));

            while (currentHealth < maxHealth)
            {
                float healAmount = healSpeed * Time.deltaTime; // Increment health based on deltaTime
                currentHealth += healAmount;

                if (currentHealth > maxHealth)
                {
                    currentHealth = maxHealth; // Cap health at maxHealth
                    break;
                }

                InvokeHealthChanged();
                yield return null; // Wait for the next frame
            }
        }

        StopHealing();
        isHealing = false; // Reset the flag when done healing
    }



    

    // Tower States


    public void OnTowerDestroyed()
    {
        if (!towerAlive) return;   

        towerAlive = false;
        StopHealing();
        InvokeHealthChanged();      
        TowerDestroyed?.Invoke();

        enabled = false;      // stop Update loop
    }

    // Modularize health management into a dedicated method
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

    // Modularize shooting logic into a dedicated method
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

    // Rewards
    public void AddCredits(float basic, float premium, float luxury, float special)
    {
        if (basic != 0f)
        {
            roundManager.IncreaseBasicCredits(basic);
        }

        var rewards = new Dictionary<CurrencyType, float>();
        if (premium != 0f) rewards[CurrencyType.Premium] = premium;
        if (luxury != 0f) rewards[CurrencyType.Luxury] = luxury;
        if (special != 0f) rewards[CurrencyType.Special] = special;

        if (rewards.Count > 0)
        {
            EventManager.TriggerEvent(EventNames.CreditsEarned, rewards);
        }
    }

}
