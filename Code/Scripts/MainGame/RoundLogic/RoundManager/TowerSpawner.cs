using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint; // Reference to the spawn point

    private GameObject activeTowerInstance; // Reference to the active tower instance
    private readonly List<GameObject> spawnedTurrets = new List<GameObject>();

    // Spawn chassis + attach the player's selected turrets (dynamic loadout)
    public Tower SpawnTowerFromSelectedLoadout()
    {
        var playerData = PlayerManager.main?.playerData;
        if (playerData == null)
        {
            Debug.LogError("[TowerSpawner] PlayerData missing (PlayerManager.main is null).");
            return null;
        }

        // defensive: ensure selectedTurretIds exists and has at least 4 entries
        if (playerData.selectedTurretIds == null)
            playerData.selectedTurretIds = new List<string> { "", "", "", "", "" };
        else if (playerData.selectedTurretIds.Count < 5)
        {
            while (playerData.selectedTurretIds.Count < 5) playerData.selectedTurretIds.Add("");
        }

        Debug.Log($"[TowerSpawner] Spawning tower visual '{playerData.selectedTowerVisualId}' with turret ids: [{string.Join(",", playerData.selectedTurretIds)}]");

        // use the selected chassis visual id (existing system)
        var visual = TowerVisualManager.Instance?.GetVisualById(playerData.selectedTowerVisualId);
        if (visual == null || visual.visualPrefab == null)
        {
            Debug.LogError($"[TowerSpawner] Selected tower visual missing for id '{playerData.selectedTowerVisualId}'.");
            return null;
        }

        // cleanup existing
        DestroyTower();

        activeTowerInstance = Instantiate(visual.visualPrefab, spawnPoint.position, spawnPoint.rotation);
        var spawnedTower = activeTowerInstance.GetComponent<Tower>();
        if (spawnedTower == null) Debug.LogWarning("[TowerSpawner] Spawned chassis prefab has no Tower component.");

        // find mount points
        var mounts = activeTowerInstance.GetComponentsInChildren<TurretSlot>(includeInactive: true);
        if (mounts == null || mounts.Length == 0)
        {
            Debug.Log("[TowerSpawner] No TurretSlot components found on chassis prefab.");
            return spawnedTower;
        }

        // build index map
        var mountByIndex = new Dictionary<int, TurretSlot>();
        foreach (var m in mounts) mountByIndex[m.Index] = m;

        // registry checks
        var registry = TurretDefinitionManager.Instance;
        if (registry == null)
        {
            Debug.LogError("[TowerSpawner] TurretDefinitionManager.Instance is null. Create a TurretDefinitionManager in the scene and assign definitions.");
            return spawnedTower;
        }

        // spawn each selected turret id (index maps to mount index)
        for (int i = 0; i < playerData.selectedTurretIds.Count && i < 4; i++)
        {
            var turretId = playerData.selectedTurretIds[i]?.Trim();
            if (string.IsNullOrEmpty(turretId))
            {
                Debug.Log($"[TowerSpawner] Slot {i} empty, skipping.");
                continue;
            }

            if (!mountByIndex.TryGetValue(i, out var mount))
            {
                Debug.LogWarning($"[TowerSpawner] No mount with Index={i} on chassis; cannot place turret '{turretId}'.");
                continue;
            }

            var def = registry.GetById(turretId);
            if (def == null)
            {
                // helpful debug: list available ids
                Debug.LogWarning($"[TowerSpawner] TurretDefinition '{turretId}' not found for slot {i}.");
                continue;
            }

            var prefab = def.turretPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[TowerSpawner] Turret prefab missing on definition '{turretId}' (slot {i}).");
                continue;
            }

            // instantiate as child and zero local transform so prefab's pivot aligns with mount
            var turretGO = Instantiate(prefab, mount.transform.position, mount.transform.rotation, mount.transform);
            turretGO.transform.localPosition = Vector3.zero;
            turretGO.transform.localRotation = Quaternion.identity;
            turretGO.transform.localScale = Vector3.one;

            // small Z offset to render above chassis if you use Z ordering
            turretGO.transform.localPosition = new Vector3(turretGO.transform.localPosition.x, turretGO.transform.localPosition.y, -0.01f);

            // ensure child sprite renderers draw above chassis
            var srs = turretGO.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs) sr.sortingOrder = Mathf.Max(sr.sortingOrder, 1);

            var turret = turretGO.GetComponent<Turret>();
            if (turret != null)
            {
                // primary slot (index 0) uses full stats; others get a 0.75 debuff by default
                float slotDmgMult = (i == 0) ? 1f : 0.75f;
                float slotFRMult = (i == 0) ? 1f : 0.75f;
                turret.InitializeFromDefinition(def, slotDamageMult: slotDmgMult, slotFireRateMult: slotFRMult, owner: spawnedTower);
                Debug.Log($"[TowerSpawner] Spawned turret '{turretId}' at slot {i}.");
            }
            else
            {
                Debug.LogWarning($"[TowerSpawner] Spawned prefab for '{turretId}' has no Turret component (slot {i}).");
            }

            spawnedTurrets.Add(turretGO);
        }

        EventManager.TriggerEvent(EventNames.TowerSpawned, spawnedTower);
        return spawnedTower;
    }

    // backwards-compat wrapper
    public Tower SpawnTower() => SpawnTowerFromSelectedLoadout();

    public GameObject GetActiveTowerInstance() => activeTowerInstance;

    public void DestroyTower()
    {
        if (activeTowerInstance != null) { Destroy(activeTowerInstance); activeTowerInstance = null; }

        for (int i = 0; i < spawnedTurrets.Count; i++)
            if (spawnedTurrets[i] != null) Destroy(spawnedTurrets[i]);
        spawnedTurrets.Clear();
    }
}
