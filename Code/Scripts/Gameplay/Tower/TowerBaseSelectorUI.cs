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
        var unlocked = PlayerManager.main.playerData.unlockedTowerBases;
        var selectedId = PlayerManager.main.playerData.selectedTowerBaseId;

        foreach (var towerBase in towerBases)
        {
            GameObject optionGO = Instantiate(optionPrefab, contentParent);
            // Assume your prefab has these components:
            var image = optionGO.transform.Find("TowerPreview").GetComponent<Image>();
            var label = optionGO.transform.Find("TowerPreviewHeader").GetComponent<TMPro.TMP_Text>();
            var description = optionGO.transform.Find("TowerBaseDescription").GetComponent<TMPro.TMP_Text>();
            var button = optionGO.transform.Find("SelectTowerBtn").GetComponent<Button>();

            Debug.Log($"Setting up base: {towerBase.id}, unlocked: {unlocked.Contains(towerBase.id)}, selected: {selectedId}");

            image.sprite = towerBase.previewSprite;
            label.text = towerBase.displayName;
            description.text = towerBase.description;

            bool isUnlocked = unlocked.Contains(towerBase.id);
            button.interactable = isUnlocked;
            // Optionally show lock/cost UI here

            // Highlight if selected
            var bgImage = optionGO.transform.Find("Background")?.GetComponent<Image>();
            if (bgImage != null && towerBase.id == selectedId)
                bgImage.color = Color.yellow;

            // Add click event
            string id = towerBase.id; // local copy for closure
            button.onClick.AddListener(() =>
            {
                Debug.Log($"Selected tower base: {id}");
                if (isUnlocked)
                {
                    PlayerManager.main.SelectTowerBase(id);
                    // Update LoadoutScreen image
                    var loadoutScreen = UnityEngine.Object.FindFirstObjectByType<LoadoutScreen>();
                    if (loadoutScreen != null)
                        loadoutScreen.SetTowerBaseImage();
                    CloseTowerBaseSelection(); // Close the panel after selection
                }
                else
                {
                    // Optionally handle unlock logic here
                }
            });
        }
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
