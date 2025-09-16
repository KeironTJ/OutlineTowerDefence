using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Outline/LoadoutDefinition")]
public class LoadoutDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public TowerVisualData chassisVisual; // existing visual SO that points to a chassis prefab

    [Header("Turret slots (index -> turret slot)")]
    public List<LoadoutTurretSlot> slots = new List<LoadoutTurretSlot>(); // expected size equals number of TurretSlot components on chassis
}

[System.Serializable]
public class LoadoutTurretSlot
{
    [Tooltip("Which turret definition is used in this slot (null = empty)")]
    public TurretDefinition turretDef;
    [Range(0.1f, 2f)] public float damageMultiplier = 1f;
    [Range(0.1f, 2f)] public float fireRateMultiplier = 1f;
    public bool locked = false; // progression lock
    public string note;
}