using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image turretImage;
    [SerializeField] private TextMeshProUGUI turretNameText;
    [SerializeField] private TextMeshProUGUI turretDescriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonText; // label on the button

    private string turretId;
    private int slotIndex;
    private PlayerManager playerManager;
    private Action<string> onPicked;

    // Configure tile. If currentlySelected==true the button becomes "Remove".
    public void Configure(TurretDefinition def, int slotIndex, bool unlocked, Action<string> onPickedCallback = null, bool currentlySelected = false)
    {
        playerManager = PlayerManager.main;
        this.slotIndex = slotIndex;
        onPicked = onPickedCallback;
        turretId = def?.id ?? string.Empty;

        if (turretImage) turretImage.sprite = def?.previewSprite;
        if (turretNameText) turretNameText.text = def?.turretName ?? "(Unknown)";
        if (turretDescriptionText) turretDescriptionText.text = def?.turretDescription ?? "";

        if (!selectButtonText && selectButton) selectButtonText = selectButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (selectButton)
        {
            selectButton.onClick.RemoveAllListeners();

            if (currentlySelected)
            {
                if (selectButtonText) selectButtonText.text = "Remove";
                selectButton.interactable = true;
                selectButton.onClick.AddListener(RemoveThis);
            }
            else
            {
                if (selectButtonText) selectButtonText.text = unlocked ? "Select" : "Locked";
                selectButton.interactable = unlocked && !string.IsNullOrEmpty(turretId);
                selectButton.onClick.AddListener(SelectThis);
            }
        }
    }

    private void SelectThis()
    {
        if (playerManager == null || string.IsNullOrEmpty(turretId)) return;
        playerManager.SetSelectedTurretForSlot(slotIndex, turretId);
        onPicked?.Invoke(turretId);
    }

    private void RemoveThis()
    {
        if (playerManager == null) return;
        playerManager.SetSelectedTurretForSlot(slotIndex, ""); // clear slot
        onPicked?.Invoke(string.Empty);
    }

    private void OnDestroy()
    {
        if (selectButton) selectButton.onClick.RemoveAllListeners();


}    }   
