using UnityEngine;

[CreateAssetMenu(menuName = "Outline/ProjectileDefinition")]
public class ProjectileDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string projectileName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("Classification")]
    public ProjectileType projectileType = ProjectileType.Standard;
    public ProjectileTrait traits = ProjectileTrait.None;
    
    [Header("Prefab")]
    [Tooltip("Prefab must contain a Bullet component.")]
    public GameObject projectilePrefab;
    
    [Header("Visual")]
    public Sprite icon;
    
    [Header("Base Stats Modifiers")]
    [Tooltip("Damage multiplier applied to turret's base damage")]
    public float damageMultiplier = 1.0f;
    [Tooltip("Speed multiplier for projectile travel speed")]
    public float speedMultiplier = 1.0f;
    
    [Header("Trait-Specific Parameters")]
    [Tooltip("For Penetrate: max enemies it can pass through (0 = infinite)")]
    public int maxPenetrations = 0;
    
    [Tooltip("For Piercing: damage per tick as % of base damage")]
    public float piercingDamagePercent = 10f;
    [Tooltip("For Piercing: duration in seconds")]
    public float piercingDuration = 3f;
    [Tooltip("For Piercing: ticks per second")]
    public float piercingTickRate = 1f;
    
    [Tooltip("For Explosive: explosion radius")]
    public float explosionRadius = 1.5f;
    [Tooltip("For Explosive: damage multiplier for AoE")]
    public float explosionDamageMultiplier = 0.5f;
    
    [Tooltip("For Slow: movement speed reduction (0.5 = 50% slower)")]
    public float slowMultiplier = 0.5f;
    [Tooltip("For Slow: duration in seconds")]
    public float slowDuration = 2f;
    
    [Tooltip("For IncoreCores/IncFragment: reward multiplier")]
    public float rewardMultiplier = 1.5f;
    
    [Tooltip("For Homing: turn rate in degrees per second")]
    public float homingTurnRate = 180f;
    
    [Tooltip("For Chain: max number of chain targets")]
    public int maxChainTargets = 3;
    [Tooltip("For Chain: chain range radius")]
    public float chainRange = 2f;
    [Tooltip("For Chain: damage multiplier per chain (0.8 = 20% reduction per jump)")]
    public float chainDamageMultiplier = 0.8f;
    
    [Header("Requirements")]
    [Tooltip("Minimum wave required to unlock")]
    public int unlockWave = 1;
    
    // Helper methods
    public bool HasTrait(ProjectileTrait trait) => (traits & trait) != 0;
    public bool IsType(ProjectileType type) => projectileType == type;
}
