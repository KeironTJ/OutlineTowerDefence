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

    private Transform target;
    private float timeUntilFire;

    // Coroutines
    private Coroutine healingCoroutine;

    // Managers
    private RoundManager roundManager;
    private EnemySpawner enemySpawner;
    private SkillManager skillManager;
    private UIManager uiManager;

    // GAME SEQUENCE

    private void Update()
    {
        DrawTargetingRange();
        ActivateShooting();
        ManageHealth();
    }

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

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void SetCurrentHealth(float health)
    {
        float maxHealth = roundManager.GetSkillValue(roundManager.GetSkill("Health"));

        if (currentHealth + health >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = health;
        }
    }

    void StartHealing()
    {
        float maxHealth = roundManager.GetSkillValue(roundManager.GetSkill("Health"));

        if (currentHealth < maxHealth && towerAlive)
        {
            isHealing = true;
            Debug.Log($"Healing?: {isHealing}");

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

                yield return null; // Wait for the next frame
            }
        }

        StopHealing();
        isHealing = false; // Reset the flag when done healing
    }

    // Add an initialization method to replace FindObjectOfType coroutines
    public void Initialize(RoundManager roundManager, EnemySpawner enemySpawner, SkillManager skillManager, UIManager uiManager)
    {
        this.roundManager = roundManager;
        this.enemySpawner = enemySpawner;
        this.skillManager = skillManager; // Initialize SkillManager
        this.uiManager = uiManager; // Initialize UIManager

        if (skillManager != null)
        {
            currentHealth = skillManager.GetSkillValue(skillManager.GetSkill("Health"));
        }
        else
        {
            Debug.LogError("SkillManager not found during initialization!");
        }
    }

    // Tower Actions
    public void TakeDamage(float attackDamage)
    {
        currentHealth -= attackDamage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnTowerDestroyed();
        }
    }

    private void Shoot()
    {
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
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 50;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
        }

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;

        float angle = 20f;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range"));
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * skillManager.GetSkillValue(skillManager.GetSkill("Targeting Range"));
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += 360f / lineRenderer.positionCount;
        }
    }

    // Tower States
    public event System.Action TowerDestroyed;

    public void OnTowerDestroyed()
    {
        // Trigger the TowerDestroyed event
        TowerDestroyed?.Invoke();

        // End Round
        towerAlive = false;

        // Stop Coroutines
        StopHealing();

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

    public void AddCredits(float basic, float premium, float luxury)
    {
        //TODO: Implement credit addition logic
        roundManager.IncreaseBasicCredits(basic);
        roundManager.IncreasePremiumCredits(premium);
        roundManager.IncreaseLuxuryCredits(luxury);
    }

}
