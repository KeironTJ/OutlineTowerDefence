using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unlocks/Projectile Unlock", fileName = "ProjectileUnlock_")]
public class ProjectileUnlockDefinition : ScriptableObject
{
    [Header("Target")]
    public string projectileId;             // must match ProjectileDefinition.id

    [Header("Requirements")]
    public int requiredHighestWave = 0;     
    public int requiredMaxDifficulty = 0;   
    public string[] prerequisiteProjectileIds;  
    public bool grantByDefault = false;     

    [Header("Costs")]
    public int costCores = 0;
    public int costPrisms = 0;
    public int costLoops = 0;

    [Header("Presentation")]
    public string lockedHint = "";          // optional UI hint (overrides auto-generated)
}
