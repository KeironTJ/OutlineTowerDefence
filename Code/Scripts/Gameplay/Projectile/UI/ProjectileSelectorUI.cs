using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class ProjectileSelectorUI : MonoBehaviour
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
            Debug.LogError("[ProjectileSelectorUI] Content parent or option prefab is not assigned.");
            return;
        }

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        var projectileMgr = ProjectileDefinitionManager.Instance;
        if (projectileMgr == null)
        {
            Debug.LogWarning("[ProjectileSelectorUI] No ProjectileDefinitionManager in scene.");
            return;
        }

        var pm = PlayerManager.main;
        if (pm == null)
        {
            Debug.LogWarning("[ProjectileSelectorUI] PlayerManager not found.");
            return;
        }

        var unlocks = ProjectileUnlockManager.Instance;
        var turretMgr = TurretDefinitionManager.Instance;

        string currentId = pm.GetSelectedProjectileForSlot(slotIndex);
        string turretId = pm.GetSelectedTurretForSlot(slotIndex);
        TurretDefinition turretDef = !string.IsNullOrEmpty(turretId) ? turretMgr?.GetById(turretId) : null;
        bool turretAssigned = turretDef != null;

        List<ProjectileDefinition> defs = projectileMgr.GetAllProjectiles();
        if (defs == null || defs.Count == 0)
        {
            Debug.LogWarning("[ProjectileSelectorUI] No projectile definitions found.");
            return;
        }

        foreach (var def in defs)
        {
            if (def == null) continue;

            var go = Instantiate(optionPrefab, contentParent);
            var tile = go.GetComponent<ProjectileButton>();
            if (tile == null)
            {
                Debug.LogWarning("[ProjectileSelectorUI] optionPrefab missing ProjectileButton component.");
                continue;
            }

            bool isUnlocked = pm.IsProjectileUnlocked(def.id);
            bool isCompatible = turretAssigned && turretDef.AcceptsProjectileType(def.projectileType);
            bool isCurrent = !string.IsNullOrEmpty(currentId) && currentId == def.id;

            tile.Configure(def, slotIndex, isUnlocked && isCompatible, OnPicked, isCurrent);

            if (!isUnlocked)
            {
                string reason = "";
                ProjectileUnlockManager.CurrencyCost cost = default;
                bool canUnlock = unlocks != null && unlocks.CanUnlock(pm, def.id, out reason, out cost);

                if (canUnlock)
                {
                    tile.ConfigureForUnlock(def, cost,
                        tryUnlock: () => unlocks.TryUnlock(pm, def.id, out _),
                        onUnlocked: () =>
                        {
                            if (turretAssigned && turretDef.AcceptsProjectileType(def.projectileType))
                            {
                                pm.SetSelectedProjectileForSlot(slotIndex, def.id);
                                loadoutScreen?.UpdateSlotButtons();
                            }
                            PopulateOptions();
                        });
                }
                else
                {
                    if (!turretAssigned)
                        reason = string.IsNullOrEmpty(reason) ? "Assign a turret first" : reason;
                    else if (!turretDef.AcceptsProjectileType(def.projectileType))
                        reason = string.IsNullOrEmpty(reason) ? "Incompatible turret" : reason;
                    if (string.IsNullOrEmpty(reason)) reason = "Locked";
                    tile.ConfigureLockedReason(reason);
                }
            }
            else if (!turretAssigned)
            {
                tile.ConfigureLockedReason("Assign a turret first", "Unavailable");
            }
            else if (!turretDef.AcceptsProjectileType(def.projectileType))
            {
                tile.ConfigureLockedReason("Incompatible turret", "Unavailable");
            }
        }

        var rect = contentParent as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void OnPicked(string id)
    {
        var pm = PlayerManager.main;
        if (pm != null)
        {
            pm.SetSelectedProjectileForSlot(slotIndex, id);
        }

        loadoutScreen?.UpdateSlotButtons();
        gameObject.SetActive(false);
    }

    private void OnEnable() => PopulateOptions();

    private void OnDisable()
    {
        if (contentParent == null) return;
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var tile = contentParent.GetChild(i).GetComponent<ProjectileButton>();
            if (tile) Destroy(tile.gameObject);
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);
}
