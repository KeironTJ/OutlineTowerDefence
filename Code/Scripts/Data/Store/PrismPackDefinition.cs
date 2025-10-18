using UnityEngine;

[System.Serializable]
public class PrismPackDefinition
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Pack Values")]
    [Min(1)]
    public int prismAmount = 100;
    [Tooltip("Base price in the chosen currency before any discounts are applied.")]
    [Min(0f)]
    public float basePrice = 0f;
    [Tooltip("Percentage discount applied to the base price. Keep flexible for future live operations.")]
    [Range(0f, 100f)]
    public float discountPercent = 0f;
    [Tooltip("Optional real-money product identifier (for future IAP integration). Leave empty for now.")]
    public string productId;

    public float GetFinalPrice()
    {
        float multiplier = 1f - Mathf.Clamp(discountPercent, 0f, 100f) / 100f;
        float raw = basePrice * multiplier;
        return Mathf.Max(0f, Mathf.Round(raw * 100f) / 100f);
    }

    public string GetFormattedPrice(string currencySymbol = "$")
    {
        return string.IsNullOrEmpty(currencySymbol)
            ? GetFinalPrice().ToString("0.00")
            : string.Format("{0}{1:0.00}", currencySymbol, GetFinalPrice());
    }
}
