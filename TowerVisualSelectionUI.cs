using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerVisualSelectionUI : MonoBehaviour
{
    [Header("References")]
    public Transform TowerSelectorPanel;
    public Transform contentParent; // Assign the Content object of your ScrollView
    public GameObject optionPrefab; // Assign your TowerVisualOption prefab

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

        var visuals = TowerVisualManager.Instance.allVisuals;
        var unlocked = PlayerManager.main.playerData.unlockedTowerVisuals;
        var selectedId = PlayerManager.main.playerData.selectedTowerVisualId;

        foreach (var visual in visuals)
        {
            GameObject optionGO = Instantiate(optionPrefab, contentParent);
            // Assume your prefab has these components:
            var image = optionGO.transform.Find("TowerPreview").GetComponent<Image>();
            var label = optionGO.transform.Find("TowerPreviewHeader").GetComponent<TMPro.TMP_Text>();
            var button = optionGO.transform.Find("SelectTowerBtn").GetComponent<Button>();

            Debug.Log($"Setting up visual: {visual.id}, unlocked: {unlocked.Contains(visual.id)}, selected: {selectedId}");

            image.sprite = visual.previewSprite;
            label.text = visual.displayName;

            bool isUnlocked = unlocked.Contains(visual.id);
            button.interactable = isUnlocked;
            // Optionally show lock/cost UI here

            // Highlight if selected
            var bgImage = optionGO.transform.Find("Background")?.GetComponent<Image>();
            if (bgImage != null && visual.id == selectedId)
                bgImage.color = Color.yellow;

            // Add click event
            string id = visual.id; // local copy for closure
            button.onClick.AddListener(() =>
            {
                Debug.Log($"Selected tower visual: {id}");
                if (isUnlocked)
                {
                    PlayerManager.main.SelectTowerVisual(id);
                    // Update MainMenuUIManager image
                    var mainMenu = FindObjectOfType<MainMenuUIManager>();
                    if (mainMenu != null)
                        mainMenu.SetTowerVisualImage();
                    CloseTowerVisualSelection(); // Close the panel after selection
                }
                else
                {
                    // Optionally handle unlock logic here
                }
            });
        }
    }
    
    public void OpenTowerVisualSelection()
    {
        TowerSelectorPanel.gameObject.SetActive(true);
        TowerVisualSelectionUI towerVisualSelectionUI = TowerSelectorPanel.GetComponent<TowerVisualSelectionUI>();
        if (towerVisualSelectionUI != null)
        {
            towerVisualSelectionUI.PopulateOptions();
        }
    }
    
    public void CloseTowerVisualSelection()
    {
        TowerSelectorPanel.gameObject.SetActive(false);
    }
}
