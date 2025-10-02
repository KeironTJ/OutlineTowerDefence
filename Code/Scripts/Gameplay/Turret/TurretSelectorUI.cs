using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class TurretSelectorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject optionPrefab;

    private int slotIndex;
    private LoadoutScreen loadoutScreen; // optional owner to refresh after pick

    public void SetSlotIndex(int index) => slotIndex = index;
    public void SetLoadoutScreen(LoadoutScreen ls) => loadoutScreen = ls;

    public void PopulateOptions()
    {
        if (contentParent == null || optionPrefab == null)
        {
            Debug.LogError("[TurretSelectorUI] Content parent or option prefab is not assigned.");
            return;
        }

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        var mgr = TurretDefinitionManager.Instance;
        if (mgr == null) { Debug.LogWarning("No TurretDefinitionManager in scene"); return; }

        var pm = PlayerManager.main;
        var unlocks = TurretUnlockManager.Instance;
        var defs = mgr.GetAllTurrets();
        if (defs == null || defs.Count == 0) { Debug.LogWarning("No turret definitions found"); return; }

        string currentId = pm != null ? pm.GetSelectedTurretForSlot(slotIndex) : string.Empty;

        var optionData = new List<TurretOptionData>();

        foreach (var def in defs)
        {
            bool isUnlocked = pm?.playerData?.unlockedTurretIds != null && pm.playerData.unlockedTurretIds.Contains(def.id);
            bool isCurrent  = !string.IsNullOrEmpty(currentId) && currentId == def.id;

            string lockReason = "Not available";
            TurretUnlockManager.CurrencyCost cost = default;
            bool canUnlockNow = false;

            if (!isUnlocked)
            {
                if (unlocks != null)
                {
                    canUnlockNow = unlocks.CanUnlock(pm, def.id, out lockReason, out cost);
                    if (canUnlockNow)
                        lockReason = "";
                }
                else
                    lockReason = "Unlock system unavailable";
            }

            optionData.Add(new TurretOptionData
            {
                definition = def,
                isUnlocked = isUnlocked,
                isCurrent = isCurrent,
                canUnlock = canUnlockNow,
                cost = cost,
                lockReason = string.IsNullOrEmpty(lockReason) ? "Not available" : lockReason
            });
        }

        var ordered = optionData
            .OrderByDescending(o => o.SortPriority)
            .ThenBy(o => string.IsNullOrEmpty(o.definition?.turretName) ? o.definition?.id : o.definition.turretName)
            .ToList();

        foreach (var option in ordered)
        {
            var go = Instantiate(optionPrefab, contentParent);
            var tile = go.GetComponent<TurretButton>();
            if (tile == null)
            {
                Debug.LogWarning("[TurretSelectorUI] optionPrefab missing TurretButton component.");
                continue;
            }

            tile.Configure(option.definition, slotIndex, option.isUnlocked, OnPicked, option.isCurrent);

            if (!option.isUnlocked)
            {
                if (option.canUnlock && unlocks != null)
                {
                    tile.ConfigureForUnlock(option.definition, option.cost,
                        tryUnlock: () => unlocks.TryUnlock(pm, option.definition.id, out _),
                        onUnlocked: () =>
                        {
                            pm.SetSelectedTurretForSlot(slotIndex, option.definition.id);
                            loadoutScreen?.UpdateSlotButtons();
                            gameObject.SetActive(false);
                        });
                }
                else
                {
                    tile.ConfigureLockedReason(option.lockReason);
                }
            }
        }

        var rect = contentParent as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void OnPicked(string id)
    {
        // refresh loadout slots and close
        if (loadoutScreen != null) loadoutScreen.UpdateSlotButtons();
        else
        {
            var lScreen = FindFirstObjectByType<LoadoutScreen>();
            if (lScreen) lScreen.UpdateSlotButtons();
        }
        gameObject.SetActive(false);
    }

    private void OnEnable() => PopulateOptions();

    private void OnDisable()
    {
        // cleanup buttons
        if (contentParent == null) return;
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var tile = contentParent.GetChild(i).GetComponent<TurretButton>();
            if (tile) Destroy(tile.gameObject);
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);

    private class TurretOptionData
    {
        public TurretDefinition definition;
        public bool isUnlocked;
        public bool isCurrent;
        public bool canUnlock;
        public TurretUnlockManager.CurrencyCost cost;
        public string lockReason;

        public int SortPriority
        {
            get
            {
                if (isUnlocked)
                    return isCurrent ? 3 : 2;
                if (canUnlock)
                    return 1;
                return 0;
            }
        }
    }
}
