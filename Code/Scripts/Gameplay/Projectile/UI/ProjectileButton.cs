using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectileButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image projectileImage;
    [SerializeField] private TextMeshProUGUI projectileNameText;
    [SerializeField] private TextMeshProUGUI projectileDescriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText; // label on the button
    [SerializeField] private TextMeshProUGUI footerText;        // optional: show cost/reason

    private string projectileId;
    private int slotIndex;
    private PlayerManager pm;
    private Action<string> onPicked;

    public void Configure(ProjectileDefinition def, int slotIndex, bool unlocked, Action<string> onPickedCallback = null, bool currentlySelected = false)
    {
        pm = PlayerManager.main;
        this.slotIndex = slotIndex;
        projectileId = def?.id ?? string.Empty;
        onPicked = onPickedCallback;

        if (projectileImage)
        {
            projectileImage.sprite = def?.icon;
            projectileImage.enabled = def?.icon != null;
            if (projectileImage.enabled)
                projectileImage.preserveAspect = true;
        }

        if (projectileNameText) projectileNameText.text = def?.projectileName ?? "(Unknown)";
        if (projectileDescriptionText) projectileDescriptionText.text = def?.description ?? string.Empty;

        if (!actionButtonText && actionButton)
            actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (footerText) footerText.text = string.Empty;

        if (!actionButton)
        {
            Debug.LogWarning("[ProjectileButton] Action button reference missing.");
            return;
        }

        actionButton.onClick.RemoveAllListeners();

        if (currentlySelected)
        {
            if (actionButtonText) actionButtonText.text = "Remove";
            actionButton.interactable = true;
            actionButton.onClick.AddListener(() =>
            {
                pm?.SetSelectedProjectileForSlot(this.slotIndex, string.Empty);
                onPicked?.Invoke(string.Empty);
            });
        }
        else if (unlocked)
        {
            if (actionButtonText) actionButtonText.text = "Select";
            actionButton.interactable = !string.IsNullOrEmpty(projectileId);
            actionButton.onClick.AddListener(() =>
            {
                pm?.SetSelectedProjectileForSlot(this.slotIndex, projectileId);
                onPicked?.Invoke(projectileId);
            });
        }
        else
        {
            if (actionButtonText) actionButtonText.text = "Locked";
            actionButton.interactable = false;
        }
    }

    public void ConfigureForUnlock(ProjectileDefinition def, ProjectileUnlockManager.CurrencyCost cost, Func<bool> tryUnlock, Action onUnlocked)
    {
        if (!actionButton)
        {
            Debug.LogWarning("[ProjectileButton] Cannot configure unlock – action button missing.");
            return;
        }

        if (!actionButtonText)
            actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (actionButtonText) actionButtonText.text = "Unlock";
        if (footerText) footerText.text = cost.ToLabel();

        actionButton.interactable = true;
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() =>
        {
            if (tryUnlock != null && tryUnlock())
            {
                onUnlocked?.Invoke();
            }
        });
    }

    public void ConfigureLockedReason(string reason, string buttonLabel = "Locked")
    {
        if (!actionButton)
            return;

        if (!actionButtonText)
            actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (actionButtonText) actionButtonText.text = buttonLabel;
        if (footerText) footerText.text = reason ?? "";
        actionButton.interactable = false;
        actionButton.onClick.RemoveAllListeners();
    }
}
