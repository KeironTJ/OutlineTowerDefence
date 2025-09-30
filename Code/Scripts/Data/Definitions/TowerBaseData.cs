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
}