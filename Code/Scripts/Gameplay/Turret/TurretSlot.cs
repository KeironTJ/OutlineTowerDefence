using UnityEngine;

/// <summary>
/// Attach to empty child transforms on chassis prefab to mark turret mounting points.
/// Set Index to map to LoadoutDefinition.slots ordering.
/// </summary>
public class TurretSlot : MonoBehaviour
{
    [Tooltip("Index 0..N mapping to loadout slots")]
    public int Index = 0;
}