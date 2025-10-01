using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadoutScreen : MonoBehaviour
{
    [Header("Tower Base")]
    [SerializeField] private Image towerBaseImage;
    [SerializeField] private TextMeshProUGUI towerBaseDescription;

    [Header("Turret Selection")]
    [SerializeField] private GameObject turretSelectionPanel;
    [SerializeField] private List<Button> slotButtons;

    [Header("Turret Slots (simple gating)")]
    [SerializeField] private SkillService skillService;          // assign in Inspector (or auto-find)
    [SerializeField] private string turretSlotsSkillId = "TurretSlots";
    [SerializeField] private int defaultTotalSlots = 1;          // fallback if skill not ready
    [SerializeField] private int debugOverrideTotalSlots = -1;   // set >=0 to force (for testing)

    private PlayerManager playerManager;

    private void Start()
    {
        playerManager = PlayerManager.main;
        skillService = SkillService.Instance;

        SetTowerBaseImage();
        UpdateSlotButtons();
    }

    private void OnEnable()
    {
        SetTowerBaseImage();
        UpdateSlotButtons();
    }

    // ================= TOWER BASE =================
    public void SetTowerBaseImage()
    {
        if (!towerBaseImage || playerManager?.playerData == null) return;
        var selectedId = playerManager.playerData.selectedTowerBaseId;
        var towerBases = TowerBaseManager.Instance.allBases;
        foreach (var towerBase in towerBases)
        {
            if (towerBase.id == selectedId)
            {
                towerBaseImage.sprite = towerBase.previewSprite;
                towerBaseDescription.text = towerBase.description;
                break;
            }
        }
    }

    // ================= TURRET SELECTION =================
    // Total unlocked slots (1-based: e.g., 3 means indices 0..2 are unlocked)
    private int GetUnlockedSlotsCount()
    {
        int total = defaultTotalSlots;
        if (debugOverrideTotalSlots >= 0)
            total = debugOverrideTotalSlots;
        else if (skillService != null)
        {
            try
            {
                // Prefer: int GetValue(string id). Fallback to GetLevel if that's your API.
                var t = skillService.GetType();
                var mGetValue = t.GetMethod("GetValue", new[] { typeof(string) });
                if (mGetValue != null) total = Mathf.Max(defaultTotalSlots, System.Convert.ToInt32(mGetValue.Invoke(skillService, new object[] { turretSlotsSkillId })));
                else
                {
                    var mGetLevel = t.GetMethod("GetLevel", new[] { typeof(string) });
                    if (mGetLevel != null) total = Mathf.Max(defaultTotalSlots, System.Convert.ToInt32(mGetLevel.Invoke(skillService, new object[] { turretSlotsSkillId })));
                }
            }
            catch { total = defaultTotalSlots; }
        }
        return Mathf.Clamp(total, 1, slotButtons?.Count ?? 1);
    }

    public void UpdateSlotButtons()
    {
        if (playerManager == null) playerManager = PlayerManager.main;
        var defs = TurretDefinitionManager.Instance?.GetAllTurrets();
        if (defs == null) return;

        int unlockedCount = GetUnlockedSlotsCount();

        for (int i = 0; i < slotButtons.Count; i++)
        {
            int index = i;

            string selectedId = playerManager.GetSelectedTurretForSlot(index);
            TurretDefinition def = TurretDefinitionManager.Instance.GetById(selectedId);
            if (def == null && !string.IsNullOrEmpty(selectedId))
                Debug.LogWarning($"[LoadoutScreen] No TurretDefinition for id '{selectedId}'");

            var root = slotButtons[i].transform;

            var nameTxt    = root.Find("Info/TurretName")?.GetComponent<TextMeshProUGUI>();
            var descTxt    = root.Find("Info/TurretDescription")?.GetComponent<TextMeshProUGUI>();
            var previewImg = root.Find("Image")?.GetComponent<Image>();
            var lockedGO   = root.Find("LockedPanel")?.gameObject;
            var chooseGO   = root.Find("ChooseTurretPanel")?.gameObject;

            bool isUnlocked = index < unlockedCount;
            bool hasDef = def != null;

            if (hasDef)
            {
                if (nameTxt)  nameTxt.text = def.turretName;
                if (descTxt)  descTxt.text = def.turretDescription;
                if (previewImg)
                {
                    previewImg.sprite = def.previewSprite;
                    previewImg.enabled = def.previewSprite != null;
                    previewImg.preserveAspect = true;
                    previewImg.color = Color.white;
                }
            }
            else
            {
                if (nameTxt)  nameTxt.text = isUnlocked ? "Empty" : "Locked";
                if (descTxt)  descTxt.text = isUnlocked ? "Select a turret" : "Unlock more slots";
                if (previewImg) { previewImg.sprite = null; previewImg.enabled = false; }
            }

            // Panels
            if (chooseGO) chooseGO.SetActive(isUnlocked && !hasDef);
            if (lockedGO) lockedGO.SetActive(!isUnlocked);

            // Click to open selector for this slot
            slotButtons[i].interactable = isUnlocked;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() =>
            {
                if (!isUnlocked) { Debug.Log($"[LoadoutScreen] Slot {index} is locked"); return; }
                OpenTurretVisualSelection(index);
            });
        }
    }

    public void OpenTurretSelection()
    {
        turretSelectionPanel.SetActive(true);
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int index = i; // Local copy for closure
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OpenTurretVisualSelection(index));
        }
    }

    public void CloseTurretSelection()
    {
        turretSelectionPanel.SetActive(false);
    }

    public void OpenTurretVisualSelection(int slotIndex)
    {
        // Block opening if slot is locked by skill
        if (slotIndex >= GetUnlockedSlotsCount()) { Debug.Log($"[LoadoutScreen] Slot {slotIndex} locked"); return; }

        // Ensure we have an EventSystem + GraphicRaycaster (otherwise buttons won't click)
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.LogWarning("[LoadoutScreen] No EventSystem found. Created one at runtime.");
        }

        turretSelectionPanel.gameObject.SetActive(true);
        // Selector might be on a child; search in children (include inactive)
        TurretSelectorUI turretSelectorUI = turretSelectionPanel.GetComponent<TurretSelectorUI>();
        if (turretSelectorUI == null)
            turretSelectorUI = turretSelectionPanel.GetComponentInChildren<TurretSelectorUI>(true);

        if (turretSelectorUI != null)
        {
            turretSelectorUI.SetSlotIndex(slotIndex);
            turretSelectorUI.SetLoadoutScreen(this); // allow selector to notify when a pick is made
            turretSelectorUI.PopulateOptions();
            Debug.Log($"[LoadoutScreen] Opened turret selector for slot {slotIndex}");
        }
        else
        {
            Debug.LogError("[LoadoutScreen] TurretSelectorUI component not found under 'turretSelectionPanel'. " +
                           "Add the component to that panel or a child and try again.");
        }
    }
}
