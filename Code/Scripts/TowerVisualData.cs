using UnityEngine;

[CreateAssetMenu(menuName = "Tower/VisualData")]
public class TowerVisualData : ScriptableObject
{
    public string id; // Unique identifier for this visual
    public string displayName;
    public Sprite previewSprite;
    public GameObject visualPrefab;
    public int unlockCost;
    public bool isPurchasable; // true = IAP/currency, false = progress
}