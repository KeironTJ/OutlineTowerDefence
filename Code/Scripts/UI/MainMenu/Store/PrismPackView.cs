using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class PrismPackView : MonoBehaviour
{
    [Header("Core UI")]
    public Button purchaseButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI prismsAmountText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI discountText;
    public GameObject discountContainer;

    private void Reset()
    {
        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();
    }

    public void Apply(PrismPackDefinition pack, string currencySymbol, UnityAction onPurchase)
    {
        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();

        if (titleText != null)
            titleText.text = pack.displayName;

        if (prismsAmountText != null)
            prismsAmountText.text = $"x{pack.prismAmount:N0}";

        if (priceText != null)
            priceText.text = pack.GetFormattedPrice(currencySymbol);

        bool hasDiscount = pack.discountPercent > 0f;

        if (discountText != null)
        {
            discountText.gameObject.SetActive(hasDiscount);
            if (hasDiscount)
                discountText.text = $"Save {pack.discountPercent:0}%";
        }

        if (discountContainer != null)
            discountContainer.SetActive(hasDiscount);

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            if (onPurchase != null)
                purchaseButton.onClick.AddListener(onPurchase);
        }
    }
}
