using UnityEngine;

[CreateAssetMenu(menuName = "Outline/TurretDefinition")]
public class TurretDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [Tooltip("Prefab must contain a Turret component.")]
    public GameObject turretPrefab;
    public Sprite previewSprite;

    // default stats (design-time); per-slot multipliers will apply at spawn
    public float baseRotationSpeed = 360f;

    [Header("Optional skill id suffixes (SkillService keys will be {id}_{suffix})")]
    [Tooltip("Leave empty to skip skill-modification for that stat")]
    public string skillSuffixDamage = "damage";
    public string skillSuffixFireRate = "fireRate";
    public string skillSuffixRange = "range";
    public string skillSuffixRotation = "rotation";
}