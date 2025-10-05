using UnityEngine;

/// <summary>
/// Example integration script showing how to use the projectile traits system.
/// This is a reference implementation - adapt to your game's needs.
/// </summary>
public class ProjectileIntegrationExample : MonoBehaviour
{
    [Header("Example: Assign Projectile to Turret at Runtime")]
    [SerializeField] private Turret targetTurret;
    [SerializeField] private string projectileDefinitionId = "EXPLOSIVE_BOLT";
    
    private void Start()
    {
        // Example 1: Direct assignment to turret
        if (targetTurret != null)
        {
            ExampleDirectAssignment();
        }
        
        // Example 2: Player-driven selection
        ExamplePlayerSelection();
        
        // Example 3: Query available projectiles
        ExampleQueryProjectiles();
    }
    
    /// <summary>
    /// Example 1: Directly assign a projectile to a turret instance
    /// </summary>
    private void ExampleDirectAssignment()
    {
        Debug.Log("=== Example 1: Direct Assignment ===");
        
        // Validate that the projectile definition exists
        if (ProjectileDefinitionManager.Instance == null)
        {
            Debug.LogError("ProjectileDefinitionManager not found in scene!");
            return;
        }
        
        var projDef = ProjectileDefinitionManager.Instance.GetById(projectileDefinitionId);
        if (projDef == null)
        {
            Debug.LogWarning($"Projectile definition '{projectileDefinitionId}' not found!");
            return;
        }
        
        // Assign to turret
        targetTurret.SetProjectileDefinition(projectileDefinitionId);
        Debug.Log($"Assigned projectile '{projDef.projectileName}' to turret");
    }
    
    /// <summary>
    /// Example 2: Use PlayerManager for player-driven projectile selection
    /// </summary>
    private void ExamplePlayerSelection()
    {
        Debug.Log("=== Example 2: Player Selection ===");
        
        if (PlayerManager.main == null)
        {
            Debug.LogWarning("PlayerManager not available");
            return;
        }
        
        // Unlock some projectiles for the player
        PlayerManager.main.UnlockProjectile("STD_BULLET");
        PlayerManager.main.UnlockProjectile("EXPLOSIVE_MISSILE");
        PlayerManager.main.UnlockProjectile("PIERCING_SHARD");
        
        // Check if projectile is unlocked
        bool isUnlocked = PlayerManager.main.IsProjectileUnlocked("EXPLOSIVE_MISSILE");
        Debug.Log($"EXPLOSIVE_MISSILE unlocked: {isUnlocked}");
        
        // Assign projectile to turret slot 0
        PlayerManager.main.SetSelectedProjectileForSlot(0, "EXPLOSIVE_MISSILE");
        
        // Get the selected projectile for a slot
        string selected = PlayerManager.main.GetSelectedProjectileForSlot(0);
        Debug.Log($"Slot 0 projectile: {selected}");
    }
    
    /// <summary>
    /// Example 3: Query and filter projectiles
    /// </summary>
    private void ExampleQueryProjectiles()
    {
        Debug.Log("=== Example 3: Query Projectiles ===");
        
        if (ProjectileDefinitionManager.Instance == null) return;
        
        // Get all projectiles
        var allProjectiles = ProjectileDefinitionManager.Instance.GetAll();
        Debug.Log($"Total projectiles: {allProjectiles.Count}");
        
        // Get projectiles by type
        var shardProjectiles = ProjectileDefinitionManager.Instance.GetByType(ProjectileType.Shard);
        Debug.Log($"Shard projectiles: {shardProjectiles.Count}");
        foreach (var proj in shardProjectiles)
        {
            Debug.Log($"  - {proj.projectileName} (ID: {proj.id})");
        }
        
        // Get projectiles with specific trait
        var explosiveProjectiles = ProjectileDefinitionManager.Instance.GetByTrait(ProjectileTrait.Explosive);
        Debug.Log($"Explosive projectiles: {explosiveProjectiles.Count}");
        foreach (var proj in explosiveProjectiles)
        {
            Debug.Log($"  - {proj.projectileName} (Traits: {proj.traits})");
        }
        
        // Check if a projectile has specific traits
        var projDef = ProjectileDefinitionManager.Instance.GetById("EXPLOSIVE_BOLT");
        if (projDef != null)
        {
            bool hasExplosive = projDef.HasTrait(ProjectileTrait.Explosive);
            bool hasSlow = projDef.HasTrait(ProjectileTrait.Slow);
            Debug.Log($"EXPLOSIVE_BOLT - Explosive: {hasExplosive}, Slow: {hasSlow}");
        }
    }
    
    /// <summary>
    /// Example: Check turret compatibility with projectile
    /// </summary>
    public bool CanTurretUseProjectile(TurretDefinition turretDef, ProjectileDefinition projDef)
    {
        if (turretDef == null || projDef == null) return false;
        
        // Check if turret accepts this projectile type
        bool accepts = turretDef.AcceptsProjectileType(projDef.projectileType);
        
        if (!accepts)
        {
            Debug.Log($"Turret '{turretDef.displayName}' cannot use projectile '{projDef.projectileName}' " +
                     $"(Type: {projDef.projectileType})");
        }
        
        return accepts;
    }
    
    /// <summary>
    /// Example: Get projectiles compatible with a turret
    /// </summary>
    public void ListCompatibleProjectiles(TurretDefinition turretDef)
    {
        if (turretDef == null || ProjectileDefinitionManager.Instance == null) return;
        
        Debug.Log($"Compatible projectiles for {turretDef.displayName}:");
        
        var allProjectiles = ProjectileDefinitionManager.Instance.GetAll();
        foreach (var proj in allProjectiles)
        {
            if (turretDef.AcceptsProjectileType(proj.projectileType))
            {
                Debug.Log($"  - {proj.projectileName} (Type: {proj.projectileType}, Traits: {proj.traits})");
            }
        }
    }
}
