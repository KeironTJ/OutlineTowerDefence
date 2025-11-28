using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class ProjectileSelectorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject optionPrefab;

    private int slotIndex;
    private LoadoutScreen loadoutScreen;

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
        {
            var child = contentParent.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        var pm = PlayerManager.main;
        if (pm == null)
        {
            Debug.LogWarning("[ProjectileSelectorUI] PlayerManager not found.");
            return;
        }

        var projectileMgr = ProjectileDefinitionManager.Instance;
        if (projectileMgr == null)
        {
            Debug.LogWarning("[ProjectileSelectorUI] ProjectileDefinitionManager missing.");
            return;
        }

        var defs = projectileMgr.GetAllProjectiles();
        if (defs == null || defs.Count == 0)
        {
            Debug.LogWarning("[ProjectileSelectorUI] No projectile definitions found.");
            return;
        }

        var unlockService = ContentUnlockService.Instance;
        var turretMgr = TurretDefinitionManager.Instance;

        string currentProjectileId = pm.GetSelectedProjectileForSlot(slotIndex);
        string turretId = pm.GetSelectedTurretForSlot(slotIndex);
        TurretDefinition turretDef = !string.IsNullOrEmpty(turretId) ? turretMgr?.GetById(turretId) : null;
        bool turretAssigned = turretDef != null;

        var optionData = new List<ProjectileOptionData>(defs.Count);

        foreach (var def in defs)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;

            bool isUnlocked = unlockService?.IsUnlocked(UnlockableContentType.Projectile, def.id)
                ?? pm.IsProjectileUnlocked(def.id);
            bool isCurrent = !string.IsNullOrEmpty(currentProjectileId) && currentProjectileId == def.id;
            bool isCompatible = turretAssigned && turretDef.AcceptsProjectileType(def.projectileType);

            string lockReason = string.Empty;
            UnlockPathInfo unlockPath = default;
            bool canUnlock = false;

            if (!isUnlocked)
            {
                if (unlockService != null)
                {
                    canUnlock = unlockService.CanUnlock(UnlockableContentType.Projectile, def.id, out unlockPath, out lockReason);
                    if (canUnlock)
                        lockReason = string.Empty;
                }
                else
                {
                    lockReason = "Unlock system unavailable";
                }
            }

            if (!turretAssigned)
            {
                lockReason = string.IsNullOrEmpty(lockReason) ? "Assign a turret first" : lockReason;
            }
            else if (!isCompatible)
            {
                lockReason = string.IsNullOrEmpty(lockReason) ? "Incompatible turret" : lockReason;
            }
            else if (!isUnlocked && !canUnlock)
            {
                lockReason = string.IsNullOrEmpty(lockReason) ? "Locked" : lockReason;
            }

            optionData.Add(new ProjectileOptionData
            {
                definition = def,
                isUnlocked = isUnlocked,
                isCurrent = isCurrent,
                turretAssigned = turretAssigned,
                isCompatible = isCompatible,
                canUnlock = canUnlock,
                unlockPath = unlockPath,
                lockReason = string.IsNullOrEmpty(lockReason) ? "Locked" : lockReason
            });
        }

        var ordered = optionData
            .OrderByDescending(o => o.SortPriority)
            .ThenBy(o => string.IsNullOrEmpty(o.definition?.projectileName) ? o.definition?.id : o.definition.projectileName)
            .ToList();

        foreach (var option in ordered)
        {
            var go = Instantiate(optionPrefab, contentParent);
            var tile = go.GetComponent<ProjectileButton>();
            if (tile == null)
            {
                Debug.LogWarning("[ProjectileSelectorUI] optionPrefab missing ProjectileButton component.");
                Destroy(go);
                continue;
            }

            bool canSelect = option.CanSelect;
            tile.Configure(option.definition, slotIndex, canSelect, OnPicked, option.isCurrent);

            if (canSelect)
                continue;

            if (!option.turretAssigned)
            {
                tile.ConfigureLockedReason("Assign a turret first", "Unavailable");
            }
            else if (!option.isCompatible)
            {
                tile.ConfigureLockedReason("Incompatible turret", option.isUnlocked ? "Unavailable" : "Locked");
            }
            else if (!option.isUnlocked)
            {
                if (option.canUnlock && unlockService != null)
                {
                    var localDef = option.definition;
                    tile.ConfigureForUnlock(option.unlockPath,
                        tryUnlock: () => unlockService.TryUnlock(UnlockableContentType.Projectile, localDef.id, out _),
                        onUnlocked: () =>
                        {
                            if (turretAssigned && turretDef != null && turretDef.AcceptsProjectileType(localDef.projectileType))
                            {
                                pm.SetSelectedProjectileForSlot(slotIndex, localDef.id);
                            }

                            loadoutScreen?.UpdateSlotButtons();
                            PopulateOptions();
                        });
                }
                else
                {
                    tile.ConfigureLockedReason(option.lockReason);
                }
            }
            else
            {
                tile.ConfigureLockedReason(option.lockReason);
            }
        }

        var rect = contentParent as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void OnPicked(string projectileId)
    {
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
            if (tile != null)
            {
                if (Application.isPlaying)
                    Destroy(tile.gameObject);
                else
                    DestroyImmediate(tile.gameObject);
            }
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);

    private class ProjectileOptionData
    {
        public ProjectileDefinition definition;
        public bool isUnlocked;
        public bool isCurrent;
        public bool turretAssigned;
        public bool isCompatible;
        public bool canUnlock;
        public UnlockPathInfo unlockPath;
        public string lockReason;

        public bool CanSelect => turretAssigned && isUnlocked && isCompatible;

        public int SortPriority
        {
            get
            {
                if (CanSelect)
                    return isCurrent ? 4 : 3;
                if (turretAssigned && canUnlock)
                    return 2;
                if (turretAssigned && isUnlocked && !isCompatible)
                    return 1;
                return 0;
            }
        }
    }
}
