using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Turret : MonoBehaviour
{
    [Header("Firing")]
    public GameObject projectilePrefab;   // prefab with Bullet/Projectile script (LEGACY - use projectileDefinitionId)
    public Transform firingPoint;         // assigned in turret prefab
    public LayerMask enemyMask;
    [SerializeField] private Transform turretHead;
    
    [Header("Projectile System")]
    [Tooltip("ID of the projectile definition to use. Overrides projectilePrefab if set.")]
    public string projectileDefinitionId = "";

    // runtime stats (final values used while firing/rotating)
    private float runtimeDamage;
    private float runtimeFireRate; // shots per second
    private float runtimeRange;
    private float runtimeRotationSpeed;

    private TurretDefinition activeDefinition;
    private string skillPrefix = ""; // set from definition.id for skill keys

    // runtime
    private Transform currentTarget;
    private bool active = true;
    private Tower ownerChassis;

    // cooldownRemaining is time left until next shot (seconds). 0 = ready.
    private float cooldownRemaining;
    [SerializeField] private float baseRotationSpeed; // fallback if definition missing
    [Header("Targeting")]
    [Tooltip("Degrees tolerance required to consider turret 'aimed' at the target.")]
    [SerializeField] private float aimToleranceDeg = 5f;

    // stored slot multipliers (so we can recompute when skills change)
    private float storedSlotDamageMult = 1f;
    private float storedSlotFireRateMult = 1f;

    // Cached visuals
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;
    private const int rangeSegments = 50;
    private float lastRange = -1f;

    private SkillService skillService => SkillService.Instance;

    // skill ids (global constants you keep/use elsewhere)
    private const string attackDamageSkillId = "Attack Damage";
    private const string attackSpeedSkillId = "Attack Speed";
    private const string bulletSpeedSkillId = "Bullet Speed";
    private const string criticalChanceSkillId = "Critical Chance";
    private const string criticalMultiplierSkillId = "Critical Multiplier";
    private const string targetingRangeSkillId = "Targeting Range";

    private void Start()
    {
        if (turretHead == null) turretHead = this.transform;
        if (baseRotationSpeed <= 0f) baseRotationSpeed = 360f;
    }

    public void InitializeFromDefinition(TurretDefinition def, float slotDamageMult = 1f, float slotFireRateMult = 1f, Tower owner = null)
    {
        activeDefinition = def;
        ownerChassis = owner;

        storedSlotDamageMult = slotDamageMult;
        storedSlotFireRateMult = slotFireRateMult;

        if (def != null)
        {
            baseRotationSpeed = def.baseRotationSpeed;
            // prefix used for skill keys like "{id}_damage"
            skillPrefix = string.IsNullOrEmpty(def.id) ? "" : def.id + "_";
        }

        // compute initial runtime stats (composition)
        RecomputeRuntimeStats();

        // start ready to fire after initialization
        cooldownRemaining = 0f;

        // subscribe to skill upgrades to refresh if needed
        if (skillService != null)
            skillService.SkillUpgraded += OnSkillUpgraded;
    }

    private void OnDestroy()
    {
        if (skillService != null)
            skillService.SkillUpgraded -= OnSkillUpgraded;
    }

    // recompute runtime stats using SkillService only (definition only supplies suffix keys)
    private void RecomputeRuntimeStats()
    {
        if (skillService == null)
        {
            // conservative defaults if SkillService missing
            runtimeDamage = 0f;
            runtimeFireRate = 0.0001f;
            runtimeRange = 0.01f;
            runtimeRotationSpeed = baseRotationSpeed;
            return;
        }

        // Core/global base values come from SkillService (GetValueSafe should provide sensible defaults)
        float globalAttack = skillService.GetValueSafe(attackDamageSkillId);
        float globalAttackSpeed = skillService.GetValueSafe(attackSpeedSkillId);
        float globalRange = skillService.GetValueSafe(targetingRangeSkillId);

        // Turret-specific skill entries (treated as multipliers for damage/fireRate/range)
        float turretDamageMultiplier = 1f;
        float turretFireRateMultiplier = 1f;
        float turretRangeMultiplier = 1f;
        float turretRotationMultiplier = 1f;

        if (activeDefinition != null)
        {
            if (!string.IsNullOrEmpty(activeDefinition.skillSuffixDamage))
            {
                string key = skillPrefix + activeDefinition.skillSuffixDamage;
                if (skillService.IsSkillAvailable(key))
                    turretDamageMultiplier = skillService.GetValueSafe(key);
            }

            if (!string.IsNullOrEmpty(activeDefinition.skillSuffixFireRate))
            {
                string key = skillPrefix + activeDefinition.skillSuffixFireRate;
                if (skillService.IsSkillAvailable(key))
                    turretFireRateMultiplier = skillService.GetValueSafe(key);
            }

            if (!string.IsNullOrEmpty(activeDefinition.skillSuffixRange))
            {
                string key = skillPrefix + activeDefinition.skillSuffixRange;
                if (skillService.IsSkillAvailable(key))
                    turretRangeMultiplier = skillService.GetValueSafe(key);
            }

            if (!string.IsNullOrEmpty(activeDefinition.skillSuffixRotation))
            {
                string key = skillPrefix + activeDefinition.skillSuffixRotation;
                if (skillService.IsSkillAvailable(key))
                    turretRotationMultiplier = skillService.GetValueSafe(key);
            }
        }

        // Compose final runtime stats (slot multipliers still apply)
        runtimeDamage = globalAttack * turretDamageMultiplier * storedSlotDamageMult;
        runtimeFireRate = globalAttackSpeed * turretFireRateMultiplier * storedSlotFireRateMult;
        runtimeRange = globalRange * turretRangeMultiplier;
        runtimeRotationSpeed = baseRotationSpeed * turretRotationMultiplier;

        // diagnostics
        //Debug.Log($"[Turret] RecomputeRuntimeStats dmg={runtimeDamage:F2} fireRate={runtimeFireRate:F2} range={runtimeRange:F2} rotSpeed={runtimeRotationSpeed:F2}");
    }

    // called when any skill upgrades - we check prefix match and recompute
    private void OnSkillUpgraded(string skillId)
    {
        if (activeDefinition == null) return;
        if (string.IsNullOrEmpty(skillPrefix)) return;

        // if the upgraded skill affects this turret or is a global attack skill, recompute runtime stats
        if (skillId.StartsWith(skillPrefix) ||
            skillId == attackDamageSkillId || skillId == attackSpeedSkillId || skillId == targetingRangeSkillId)
        {
            RecomputeRuntimeStats();
        }
    }

    private void Update()
    {
        if (!active) return;

        // cooldown always counts down regardless of target presence so turret 'charges' continuously
        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining < 0f) cooldownRemaining = 0f;

        DrawTargetingRange();
        ActivateShooting();
    }

    // --- Shooting / Targeting ---
    public void ActivateShooting()
    {
        HandleTargeting();
        if (currentTarget == null) return;

        if (IsAimedAtTarget() && CanShoot())
            Shoot();
    }

    private bool IsAimedAtTarget()
    {
        if (!currentTarget) return false;

        // use firingPoint (actual muzzle) when available, otherwise use turretHead transform
        Transform reference = (firingPoint != null) ? firingPoint : turretHead;
        if (reference == null) return false;

        Vector2 toTarget = (currentTarget.position - reference.position);
        if (toTarget.sqrMagnitude <= Mathf.Epsilon) return true;

        // 'forward' vector for the muzzle: firingPoint.up is commonly used for 2D turrets
        Vector2 forward = reference.up;
        float angle = Vector2.Angle(forward, toTarget.normalized);
        return angle <= aimToleranceDeg;
    }

    private void OnDrawGizmos()
    {
        if (firingPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firingPoint.position, firingPoint.position + firingPoint.up * 1.5f);
        }
    }

    private bool CanShoot()
    {
        // Use the turret's runtimeFireRate (shots per second)
        float fireInterval = 1f / Mathf.Max(0.0001f, runtimeFireRate);

        if (cooldownRemaining <= 0f)
        {
            // consume interval immediately so subsequent calls this frame won't fire again
            cooldownRemaining = fireInterval;
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
        if (!firingPoint || !currentTarget) return;
        
        // Get projectile definition and prefab
        ProjectileDefinition projDef = null;
        GameObject prefabToUse = projectilePrefab;
        
        // First try to use projectileDefinitionId if set
        if (!string.IsNullOrEmpty(projectileDefinitionId) && ProjectileDefinitionManager.Instance != null)
        {
            projDef = ProjectileDefinitionManager.Instance.GetById(projectileDefinitionId);
            if (projDef != null && projDef.projectilePrefab != null)
            {
                prefabToUse = projDef.projectilePrefab;
            }
        }
        // Fallback to definition's default if available
        else if (activeDefinition != null && !string.IsNullOrEmpty(activeDefinition.defaultProjectileId) 
                 && ProjectileDefinitionManager.Instance != null)
        {
            projDef = ProjectileDefinitionManager.Instance.GetById(activeDefinition.defaultProjectileId);
            if (projDef != null && projDef.projectilePrefab != null)
            {
                prefabToUse = projDef.projectilePrefab;
            }
        }
        
        if (!prefabToUse) return;

        // Instantiate oriented to the firingPoint so projectile faces barrel direction
        GameObject bulletObj = Instantiate(prefabToUse, firingPoint.position, firingPoint.rotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        if (bulletScript)
        {
            bulletScript.SetTarget(currentTarget);
            bulletScript.SetSpeed(GetSkillValue(bulletSpeedSkillId));
            bulletScript.SetOriginAndMaxRange(firingPoint.position, runtimeRange);

            // Use runtimeDamage composed from SkillService/definition/slot multipliers
            float baseDamage = runtimeDamage;
            float critChance = GetCritChance01();
            float critMult = GetCritMultiplier();

            // Pass damage and crit params to bullet; crit is resolved on hit
            bulletScript.ConfigureDamage(baseDamage, critChance, critMult, rollNow: false);
            
            // Set projectile definition for trait-based behavior
            if (projDef != null)
            {
                // Get upgrade level from PlayerManager if available
                int upgradeLevel = 0;
                if (PlayerManager.main != null && !string.IsNullOrEmpty(projectileDefinitionId))
                {
                    upgradeLevel = PlayerManager.main.GetProjectileUpgradeLevel(projectileDefinitionId);
                }
                bulletScript.SetProjectileDefinition(projDef, upgradeLevel);
            }
        }

        EventManager.TriggerEvent(EventNames.BulletFired, bulletScript);
    }

    private void HandleTargeting()
    {
        FindTarget();
        if (currentTarget == null || !CheckTargetIsInRange())
        {
            currentTarget = null;
            return;
        }
        RotateTowardsTarget();
    }

    private void FindTarget()
    {
        float range = runtimeRange; // use runtime value
        if (range <= 0f) { currentTarget = null; return; }

        // Use OverlapCircleAll to detect colliders inside the range (works for "overlap" checks)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);
        float closest = float.PositiveInfinity;
        Transform best = null;

        foreach (var col in hits)
        {
            if (col == null) continue;
            // prefer an Enemy component (in case collider is on a child)
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            float dist = Vector2.Distance(enemy.transform.position, transform.position);
            if (dist < closest)
            {
                closest = dist;
                best = enemy.transform;
            }
        }

        if (best != null && closest <= range)
            currentTarget = best;
        else
            currentTarget = null;
    }

    private bool CheckTargetIsInRange()
    {
        return currentTarget != null && Vector2.Distance(currentTarget.position, transform.position) <= runtimeRange;
    }

    private void RotateTowardsTarget()
    {
        if (!currentTarget || turretHead == null) return;

        Vector2 direction = currentTarget.position - turretHead.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

        // use runtimeRotationSpeed (already scaled in RecomputeRuntimeStats) so skills affect rotation
        turretHead.rotation = Quaternion.RotateTowards(
            turretHead.rotation, targetRot, runtimeRotationSpeed * Time.deltaTime);
    }

    private void DrawTargetingRange()
    {
        EnsureRangeRendererConfigured();

        float range = runtimeRange;
        // hide if no range
        if (range <= 0f)
        {
            if (rangeRenderer != null)
                rangeRenderer.enabled = false;
            lastRange = -1f;
            return;
        }

        if (rangeRenderer == null) return;

        // ensure position count is set before calling SetPosition
        if (rangeRenderer.positionCount != rangeSegments)
            rangeRenderer.positionCount = rangeSegments;

        // if range didn't change, no need to rebuild
        if (Mathf.Approximately(range, lastRange)) return;
        lastRange = range;

        rangeRenderer.enabled = true;
        // draw in world space so circle stays fixed regardless of turret rotation/parenting
        float step = (Mathf.PI * 2f) / rangeRenderer.positionCount;
        for (int i = 0; i < rangeRenderer.positionCount; i++)
        {
            float ang = i * step;
            float x = Mathf.Cos(ang) * range;
            float y = Mathf.Sin(ang) * range;
            Vector3 worldPos = transform.position + new Vector3(x, y, 0f);
            rangeRenderer.SetPosition(i, worldPos);
        }
    }

    private void EnsureRangeRendererConfigured()
    {
        if (rangeRenderer == null)
        {
            if (!TryGetComponent(out rangeRenderer))
                rangeRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (rangeRenderer == null) return;

        rangeRenderer.enabled = true;
        rangeRenderer.loop = true;
        // use world space so the circle is not affected by turret rotation/parenting
        rangeRenderer.useWorldSpace = true;
        rangeRenderer.startWidth = 0.02f;
        rangeRenderer.endWidth = 0.02f;
        rangeRenderer.positionCount = rangeSegments;
        rangeRenderer.startColor = rangeRenderer.endColor = Color.yellow;

        if (rangeMaterial == null)
            rangeMaterial = new Material(Shader.Find("Sprites/Default"));
        rangeRenderer.material = rangeMaterial;
    }

    // --- Skill Helpers ---
    private float GetSkillValue(string id)
    {
        // use GetValueSafe so missing keys return sensible default (implementation-specific)
        return (skillService != null) ? skillService.GetValueSafe(id) : 0f;
    }
    
    // --- Projectile Management ---
    public void SetProjectileDefinition(string definitionId)
    {
        // Validate that this turret accepts the projectile type
        if (activeDefinition != null && ProjectileDefinitionManager.Instance != null)
        {
            var projDef = ProjectileDefinitionManager.Instance.GetById(definitionId);
            if (projDef != null)
            {
                if (activeDefinition.AcceptsProjectileType(projDef.projectileType))
                {
                    projectileDefinitionId = definitionId;
                }
                else
                {
                    Debug.LogWarning($"Turret '{activeDefinition.id}' does not accept projectile type '{projDef.projectileType}'");
                }
            }
        }
        else
        {
            projectileDefinitionId = definitionId;
        }
    }
    
    public string GetProjectileDefinitionId() => projectileDefinitionId;

    public void SetActive(bool v) => active = v;
}