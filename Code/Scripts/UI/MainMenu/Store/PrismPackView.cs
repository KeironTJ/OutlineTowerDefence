using UnityEngine;
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
    public TextMeshProUGUI descriptionText;
    public Image iconImage;

    private void Reset()
    {
        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();
    }
}
