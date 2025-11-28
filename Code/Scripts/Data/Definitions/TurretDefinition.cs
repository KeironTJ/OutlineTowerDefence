using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Outline/TurretDefinition")]
public class TurretDefinition : ScriptableObject, IUnlockableDefinition
{
    public string id;
    public string displayName;
    public string turretDescription;
    [Tooltip("Prefab must contain a Turret component.")]
    public GameObject turretPrefab;
    public Sprite previewSprite;

    // default stats (design-time); per-slot multipliers will apply at spawn
    public float baseRotationSpeed = 360f;

    [Header("Projectile Constraints")]
    [Tooltip("Types of projectiles this turret can use. Empty list = accepts all types.")]
    public List<ProjectileType> allowedProjectileTypes = new List<ProjectileType>();
    
    [Tooltip("Default projectile ID if none is assigned")]
    public string defaultProjectileId = "STD_BULLET";

    [Header("Optional skill id suffixes (SkillService keys will be {id}_{suffix})")]
    [Tooltip("Leave empty to skip skill-modification for that stat")]
    public string skillSuffixDamage = "damage";
    public string skillSuffixFireRate = "fireRate";
    public string skillSuffixRange = "range";
    public string skillSuffixRotation = "rotation";
    
    [Header("Stat Bonuses")]
    [Tooltip("Multiple stat bonuses this turret provides")]
    public StatBonus[] statBonuses = new StatBonus[0];

    [Header("Unlocking")]
    [SerializeField] private UnlockProfile unlockProfile = new UnlockProfile();

    public string DefinitionId => id;
    public UnlockableContentType ContentType => UnlockableContentType.Turret;
    public UnlockProfile UnlockProfile
    {
        get
        {
            if (unlockProfile == null)
                unlockProfile = new UnlockProfile();
            return unlockProfile;
        }
    }
    
    // Helper method to check if turret accepts a projectile type
    public bool AcceptsProjectileType(ProjectileType type)
    {
        // Empty list means accept all types
        if (allowedProjectileTypes == null || allowedProjectileTypes.Count == 0)
            return true;
        return allowedProjectileTypes.Contains(type);
    }
    
    /// <summary>
    /// Apply all stat bonuses from this turret to the collector
    /// </summary>
    public void ApplyStatBonuses(StatCollector collector)
    {
        if (collector == null || statBonuses == null) return;
        
        foreach (var bonus in statBonuses)
        {
            if (bonus != null && bonus.IsValid)
            {
                bonus.ApplyTo(collector);
            }
        }
    }
}