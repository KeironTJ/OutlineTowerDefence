using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image turretImage;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private TextMeshProUGUI turretDescriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText; // label on the button
    [SerializeField] private TextMeshProUGUI footerText;        // optional: show cost/reason

    private string turretId;
    private int slotIndex;
    private PlayerManager pm;
    private Action<string> onPicked;

    public void Configure(TurretDefinition def, int slotIndex, bool unlocked, Action<string> onPickedCallback = null, bool currentlySelected = false)
    {
        pm = PlayerManager.main;
        this.slotIndex = slotIndex;
        onPicked = onPickedCallback;
        turretId = def?.id ?? string.Empty;

        if (turretImage) turretImage.sprite = def?.previewSprite;
        if (displayNameText) displayNameText.text = def?.displayName ?? "(Unknown)";
        if (turretDescriptionText) turretDescriptionText.text = def?.turretDescription ?? "";

        if (!actionButtonText && actionButton) actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

        actionButton.onClick.RemoveAllListeners();

        if (currentlySelected)
        {
            if (actionButtonText) actionButtonText.text = "Remove";
            if (footerText) footerText.text = "";
            actionButton.interactable = true;
            actionButton.onClick.AddListener(() =>
            {
                pm.SetSelectedTurretForSlot(this.slotIndex, "");
                onPicked?.Invoke(string.Empty);
            });
        }
        else if (unlocked)
        {
            if (actionButtonText) actionButtonText.text = "Select";
            if (footerText) footerText.text = "";
            actionButton.interactable = !string.IsNullOrEmpty(turretId);
            actionButton.onClick.AddListener(() =>
            {
                pm.SetSelectedTurretForSlot(this.slotIndex, turretId);
                onPicked?.Invoke(turretId);
            });
        }
        else
        {
            // default to locked; TurretSelectorUI can switch to unlock mode via ConfigureForUnlock
            if (actionButtonText) actionButtonText.text = "Locked";
            if (footerText) footerText.text = "";
            actionButton.interactable = false;
        }
    }

    public void ConfigureForUnlock(UnlockPathInfo path, Func<bool> tryUnlock, Action onUnlocked)
    {
        if (!actionButtonText && actionButton) actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (actionButtonText) actionButtonText.text = path.label;
        if (footerText) footerText.text = BuildFooter(path);

        actionButton.interactable = true;
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() =>
        {
            if (tryUnlock())
                onUnlocked?.Invoke();
        });
    }

    private static string BuildFooter(UnlockPathInfo path)
    {
        var cost = path.totalCost.ToLabel();
        if (string.IsNullOrEmpty(cost))
            return path.description ?? string.Empty;
        if (string.IsNullOrEmpty(path.description))
            return cost;
        return $"{cost} | {path.description}";
    }

    public void ConfigureLockedReason(string reason)
    {
        if (actionButtonText) actionButtonText.text = "Locked";
        if (footerText) footerText.text = reason ?? "";
        actionButton.interactable = false;
        actionButton.onClick.RemoveAllListeners();
    }
}
