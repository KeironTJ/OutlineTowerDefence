using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyDisplayUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    public void SetCurrency(CurrencyDefinition def, float amount, bool showIcon = true, bool showText = true)
    {
        if (iconImage) iconImage.sprite = def.icon;
        if (iconImage) iconImage.enabled = showIcon;
        if (amountText) amountText.text = showText ? NumberManager.FormatLargeNumber(amount) : "";
    }
}