using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unlocks/Turret Unlock", fileName = "TurretUnlock_")]
public class TurretUnlockDefinition : ScriptableObject
{
    [Header("Target")]
    public string turretId;                 // must match TurretDefinition.id

    [Header("Requirements")]
    public int requiredHighestWave = 0;     
    public int requiredMaxDifficulty = 0;   
    public string[] prerequisiteTurretIds;  
    public bool grantByDefault = false;     

    [Header("Costs")]
    public int costCores = 0;
    public int costPrisms = 0;
    public int costLoops = 0;

    [Header("Presentation")]
    public string lockedHint = "";          // optional UI hint (overrides auto-generated)
}