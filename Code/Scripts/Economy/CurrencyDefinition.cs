using UnityEngine;

[CreateAssetMenu(menuName = "Currency/Definition")]
public class CurrencyDefinition : ScriptableObject
{
    public CurrencyType type;
    public Sprite icon;
    public string displayName;
}