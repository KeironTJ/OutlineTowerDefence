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
        var unlocks = TurretUnlockManager.Instance;
        var defs = mgr.GetAllTurrets();
        if (defs == null || defs.Count == 0) { Debug.LogWarning("No turret definitions found"); return; }

        string currentId = pm != null ? pm.GetSelectedTurretForSlot(slotIndex) : string.Empty;

        foreach (var def in defs)
        {
            var go = Instantiate(optionPrefab, contentParent);
            var tile = go.GetComponent<TurretButton>();
            if (tile == null) { Debug.LogWarning("optionPrefab missing TurretButton"); continue; }

            bool isUnlocked = pm?.playerData?.unlockedTurretIds != null && pm.playerData.unlockedTurretIds.Contains(def.id);
            bool isCurrent  = !string.IsNullOrEmpty(currentId) && currentId == def.id;

            tile.Configure(def, slotIndex, isUnlocked, OnPicked, isCurrent);

            if (!isUnlocked)
            {
                string reason = "";
                TurretUnlockManager.CurrencyCost cost = default;
                bool canUnlock = unlocks != null && unlocks.CanUnlock(pm, def.id, out reason, out cost);

                if (canUnlock)
                {
                    tile.ConfigureForUnlock(def, cost,
                        tryUnlock: () => unlocks.TryUnlock(pm, def.id, out _),
                        onUnlocked: () =>
                        {
                            // optional: auto-assign on unlock, then close
                            pm.SetSelectedTurretForSlot(slotIndex, def.id);
                            mainMenu?.UpdateSlotButtons();
                            gameObject.SetActive(false);
                        });
                }
                else
                {
                    tile.ConfigureLockedReason(string.IsNullOrEmpty(reason) ? "Not available" : reason);
                }
            }
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
