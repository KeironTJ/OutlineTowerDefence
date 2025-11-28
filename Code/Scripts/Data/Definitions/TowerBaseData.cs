using UnityEngine;

[CreateAssetMenu(menuName = "Tower/BaseData")]
public class TowerBaseData : ScriptableObject
{
    public string id; // Unique identifier for this base
    public string displayName;
    public string description;
    public Sprite previewSprite;
    public GameObject towerBasePrefab;
    public int unlockCost;
    public bool isPurchasable; // true = IAP/currency, false = progress
    
    [Header("Stat Bonuses")]
    [Tooltip("Multiple stat bonuses this tower base provides")]
    public StatBonus[] statBonuses = new StatBonus[0];
    
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