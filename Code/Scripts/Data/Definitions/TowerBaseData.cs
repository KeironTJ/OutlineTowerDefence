using UnityEngine;

[CreateAssetMenu(menuName = "Tower/BaseData")]
public class TowerBaseData : ScriptableObject, IUnlockableDefinition
{
    public string id; // Unique identifier for this base
    public string displayName;
    public string description;
    public Sprite previewSprite;
    public GameObject towerBasePrefab;
    
    [Header("Stat Bonuses")]
    [Tooltip("Multiple stat bonuses this tower base provides")]
    public StatBonus[] statBonuses = new StatBonus[0];

    [Header("Unlocking")]
    [SerializeField] private UnlockProfile unlockProfile = new UnlockProfile();

    public string DefinitionId => id;
    public UnlockableContentType ContentType => UnlockableContentType.TowerBase;
    public UnlockProfile UnlockProfile
    {
        get
        {
            if (unlockProfile == null)
                unlockProfile = new UnlockProfile();
            return unlockProfile;
        }
    }
    
    /// <summary>
    /// Apply all stat bonuses from this tower base to the collector
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