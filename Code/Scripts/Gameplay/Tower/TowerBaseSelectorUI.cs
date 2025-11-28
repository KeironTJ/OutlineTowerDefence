using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerBaseSelectorUI : MonoBehaviour
{
    [Header("References")]
    public Transform TowerSelectorPanel;
    public Transform contentParent; // Assign the Content object of your ScrollView
    public GameObject optionPrefab; // Assign your TowerBaseOption prefab

    private void Start()
    {
        PopulateOptions();
    }

    public void PopulateOptions()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var towerBases = TowerBaseManager.Instance.allBases;
        var playerManager = PlayerManager.main;
        var selectedId = playerManager.playerData.selectedTowerBaseId;
        var unlockService = ContentUnlockService.Instance;

        foreach (var towerBase in towerBases)
        {
            GameObject optionGO = Instantiate(optionPrefab, contentParent);
            // Assume your prefab has these components:
            var image = optionGO.transform.Find("TowerPreview")?.GetComponent<Image>();
            var label = optionGO.transform.Find("TowerPreviewHeader")?.GetComponent<TMPro.TMP_Text>();
            var description = optionGO.transform.Find("TowerBaseDescription")?.GetComponent<TMPro.TMP_Text>();
            var button = optionGO.transform.Find("SelectTowerBtn").GetComponent<Button>();
            var buttonLabel = button.GetComponentInChildren<TMPro.TMP_Text>();

            bool isUnlocked = unlockService?.IsUnlocked(UnlockableContentType.TowerBase, towerBase.id)
                ?? playerManager.IsTowerBaseUnlocked(towerBase.id);
            Debug.Log($"Setting up base: {towerBase.id}, unlocked: {isUnlocked}, selected: {selectedId}");

            if (image) image.sprite = towerBase.previewSprite;
            if (label) label.text = towerBase.displayName;

            bool isUnlockedSnapshot = isUnlocked;
            bool isSelected = towerBase.id == selectedId;

            UnlockPathInfo unlockPath = default;
            bool canUnlock = false;
            string lockReason = string.Empty;

            if (!isUnlockedSnapshot)
            {
                if (unlockService != null)
                {
                    canUnlock = unlockService.CanUnlock(UnlockableContentType.TowerBase, towerBase.id, out unlockPath, out lockReason);
                    if (canUnlock)
                        lockReason = string.Empty;
                }
                else
                {
                    lockReason = "Unlock service unavailable";
                }
            }

            if (description)
                description.text = BuildDescription(towerBase.description, (!isUnlockedSnapshot && canUnlock) ? FormatUnlockFooter(unlockPath) : lockReason);

            button.interactable = isUnlockedSnapshot || canUnlock;
            button.onClick.RemoveAllListeners();
            if (buttonLabel) buttonLabel.text = isUnlockedSnapshot ? (isSelected ? "Selected" : "Select") : (canUnlock ? unlockPath.label : "Locked");

            // Highlight if selected
            var bgImage = optionGO.transform.Find("Background")?.GetComponent<Image>();
            if (bgImage != null && towerBase.id == selectedId)
                bgImage.color = Color.yellow;

            // Add click event
            string id = towerBase.id; // local copy for closure
            button.onClick.AddListener(() =>
            {
                if (isUnlockedSnapshot)
                {
                    SelectAndRefresh(id);
                    return;
                }

                if (canUnlock && unlockService != null)
                {
                    if (unlockService.TryUnlock(UnlockableContentType.TowerBase, id, out _))
                        SelectAndRefresh(id);
                }
            });
        }
    }

    private void SelectAndRefresh(string id)
    {
        PlayerManager.main.SelectTowerBase(id);
        var loadoutScreen = UnityEngine.Object.FindFirstObjectByType<LoadoutScreen>();
        if (loadoutScreen != null)
            loadoutScreen.SetTowerBaseImage();
        PopulateOptions();
        CloseTowerBaseSelection();
    }

    private static string BuildDescription(string baseDescription, string extra)
    {
        if (string.IsNullOrEmpty(extra))
            return baseDescription;
        return $"{baseDescription}\n<size=80%><color=#ff8080>{extra}</color></size>";
    }

    private static string FormatUnlockFooter(UnlockPathInfo path)
    {
        var cost = path.totalCost.ToLabel();
        if (string.IsNullOrEmpty(cost))
            return path.description;
        if (string.IsNullOrEmpty(path.description))
            return cost;
        return $"{cost} | {path.description}";
    }

    public void OpenTowerBaseSelection()
    {
        TowerSelectorPanel.gameObject.SetActive(true);
        TowerBaseSelectorUI towerBaseSelectorUI = TowerSelectorPanel.GetComponent<TowerBaseSelectorUI>();
        if (towerBaseSelectorUI != null)
        {
            towerBaseSelectorUI.PopulateOptions();
        }
    }

    public void CloseTowerBaseSelection()
    {
        TowerSelectorPanel.gameObject.SetActive(false);
    }
}
