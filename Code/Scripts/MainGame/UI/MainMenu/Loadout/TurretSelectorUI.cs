using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class TurretSelectorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject optionPrefab;

    private int slotIndex;
    private MainMenuScreen mainMenu; // optional owner to refresh after pick

    public void SetSlotIndex(int index) => slotIndex = index;
    public void SetMainMenu(MainMenuScreen mm) => mainMenu = mm;

    public void PopulateOptions()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        var mgr = TurretDefinitionManager.Instance;
        if (mgr == null) { Debug.LogWarning("No TurretDefinitionManager in scene"); return; }

        var pm = PlayerManager.main;
        var unlocked = pm?.playerData?.unlockedTurretIds ?? new List<string>();
        var defs = mgr.GetAllTurrets();
        if (defs == null || defs.Count == 0) { Debug.LogWarning("No turret definitions found"); return; }

        // current selection for this slot
        string currentId = pm != null ? pm.GetSelectedTurretForSlot(slotIndex) : string.Empty;

        foreach (var def in defs)
        {
            var go = Instantiate(optionPrefab, contentParent);
            var tile = go.GetComponent<TurretButton>();
            if (tile == null) { Debug.LogWarning("optionPrefab missing TurretButton"); continue; }

            bool isUnlocked = unlocked.Contains(def.id);
            bool isCurrent  = !string.IsNullOrEmpty(currentId) && currentId == def.id;
            tile.Configure(def, slotIndex, isUnlocked, OnPicked, isCurrent);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    private void OnPicked(string id)
    {
        // refresh main slots and close
        if (mainMenu != null) mainMenu.UpdateSlotButtons();
        else
        {
            var screen = FindFirstObjectByType<MainMenuScreen>();
            if (screen) screen.UpdateSlotButtons();
        }
        gameObject.SetActive(false);
    }

    private void OnEnable() => PopulateOptions();

    private void OnDisable()
    {
        // cleanup buttons
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var tile = contentParent.GetChild(i).GetComponent<TurretButton>();
            if (tile) Destroy(tile.gameObject);
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);
}
