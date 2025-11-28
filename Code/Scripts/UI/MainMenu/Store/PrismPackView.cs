using System;
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
    public TextMeshProUGUI statusText;

    private void Reset()
    {
        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();
    }

    public void Apply(PrismPackDefinition pack, string currencySymbol, StoreService.PrismPackAvailability availability, UnityAction onPurchase)
    {
        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();

        if (titleText != null)
            titleText.text = pack.displayName;

        if (prismsAmountText != null)
            prismsAmountText.text = $"x{pack.prismAmount:N0}";

        if (priceText != null)
        {
            if (availability.isDailyFree)
                priceText.text = availability.isAvailable ? "Free" : "Claimed";
            else
                priceText.text = pack.GetFormattedPrice(currencySymbol);
        }

        bool hasDiscount = !availability.isDailyFree && pack.discountPercent > 0f;

        if (discountText != null)
        {
            discountText.gameObject.SetActive(hasDiscount);
            if (hasDiscount)
                discountText.text = $"Save {pack.discountPercent:0}%";
        }

        if (discountContainer != null)
            discountContainer.SetActive(hasDiscount);

        if (statusText != null)
        {
            if (availability.isDailyFree && availability.isOnCooldown && availability.cooldownRemaining > TimeSpan.Zero)
            {
                statusText.text = $"Next in {FormatShortDuration(availability.cooldownRemaining)}";
                statusText.gameObject.SetActive(true);
            }
            else if (availability.isDailyFree)
            {
                statusText.text = "Free today";
                statusText.gameObject.SetActive(true);
            }
            else
            {
                statusText.gameObject.SetActive(false);
            }
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            if (onPurchase != null && availability.isAvailable)
                purchaseButton.onClick.AddListener(onPurchase);
            purchaseButton.interactable = availability.isAvailable;
        }
    }

    private static string FormatShortDuration(TimeSpan span)
    {
        if (span.TotalHours >= 24)
            return string.Format("{0}d {1}h", (int)Math.Floor(span.TotalDays), span.Hours);
        if (span.TotalHours >= 1)
            return string.Format("{0}h {1}m", (int)Math.Floor(span.TotalHours), span.Minutes);
        return string.Format("{0}m {1}s", Math.Max(span.Minutes, 0), Math.Max(span.Seconds, 0));
    }
}
