using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadoutScreen : MonoBehaviour
{
    [Header("Tower Base")]
    [SerializeField] private GameObject towerBaseSelectionPanel;
    [SerializeField] private Image towerBaseImage;
    [SerializeField] private TextMeshProUGUI towerBaseDescription;

    [Header("Turret Selection")]
    [SerializeField] private GameObject turretSelectionPanel;
    [SerializeField] private Transform turretSlotsContainer;
    [SerializeField] private GameObject turretSlotButtonPrefab;
    [SerializeField, Min(1)] private int maxSlotDisplayCount = 6;

    [Header("Turret Slots (simple gating)")]
    [SerializeField] private SkillService skillService;          // assign in Inspector (or auto-find)
    [SerializeField] private string turretSlotsSkillId = "TurretSlots";
    [SerializeField] private int defaultTotalSlots = 1;          // fallback if skill not ready
    [SerializeField] private int debugOverrideTotalSlots = -1;   // set >=0 to force (for testing)

    [Header("Projectile Selection")]
    [SerializeField] private GameObject projectileSelectionPanel;
    [SerializeField] private Transform projectileSlotsContainer;
    [SerializeField] private GameObject projectileSlotButtonPrefab;

    private PlayerManager playerManager;
    private readonly List<Button> turretSlotButtons = new List<Button>();
    private readonly List<Button> projectileButtons = new List<Button>();

    private readonly Dictionary<Button, TurretSlotUIElements> turretSlotCache = new Dictionary<Button, TurretSlotUIElements>();
    private readonly Dictionary<Button, ProjectileSlotUIElements> projectileSlotCache = new Dictionary<Button, ProjectileSlotUIElements>();

    private bool turretContainerScanned;
    private bool projectileContainerScanned;

    private class TurretSlotUIElements
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Image previewImage;
        public GameObject lockedPanel;
        public GameObject choosePanel;
    }

    private class ProjectileSlotUIElements
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Image previewImage;
        public GameObject lockedPanel;
        public GameObject choosePanel;
        public GameObject requiresTurretPanel;
    }

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
        if (maxSlotDisplayCount > 0)
            total = Mathf.Min(total, maxSlotDisplayCount);

        return Mathf.Max(1, total);
    }

    public void UpdateSlotButtons()
    {
        if (playerManager == null) playerManager = PlayerManager.main;
        var defs = TurretDefinitionManager.Instance?.GetAllTurrets();
        if (defs == null) return;

        int unlockedCount = GetUnlockedSlotsCount();
        int displayCount = Mathf.Max(unlockedCount, defaultTotalSlots);
        if (maxSlotDisplayCount > 0)
            displayCount = Mathf.Min(displayCount, maxSlotDisplayCount);

        EnsureTurretSlotButtons(displayCount);

        for (int i = 0; i < turretSlotButtons.Count; i++)
        {
            int index = i;

            string selectedId = playerManager.GetSelectedTurretForSlot(index);
            TurretDefinition def = TurretDefinitionManager.Instance != null
                ? TurretDefinitionManager.Instance.GetById(selectedId)
                : null;
            if (def == null && !string.IsNullOrEmpty(selectedId))
                Debug.LogWarning($"[LoadoutScreen] No TurretDefinition for id '{selectedId}'");

            var button = turretSlotButtons[i];
            if (!turretSlotCache.TryGetValue(button, out var elements) || elements == null)
            {
                elements = CacheTurretSlotElements(button);
                turretSlotCache[button] = elements;
            }

            bool isUnlocked = index < unlockedCount;
            bool hasDef = def != null;

            if (elements != null)
            {
                if (hasDef)
                {
                    if (elements.nameText)  elements.nameText.text = def.turretName;
                    if (elements.descriptionText)  elements.descriptionText.text = def.turretDescription;
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = def.previewSprite;
                        elements.previewImage.enabled = def.previewSprite != null;
                        elements.previewImage.preserveAspect = true;
                        elements.previewImage.color = Color.white;
                    }
                }
                else
                {
                    if (elements.nameText)  elements.nameText.text = isUnlocked ? "Empty" : "Locked";
                    if (elements.descriptionText)  elements.descriptionText.text = isUnlocked ? "Select a turret" : "Unlock more slots";
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = null;
                        elements.previewImage.enabled = false;
                    }
                }

                if (elements.choosePanel) elements.choosePanel.SetActive(isUnlocked && !hasDef);
                if (elements.lockedPanel) elements.lockedPanel.SetActive(!isUnlocked);
            }

            // Click to open selector for this slot
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!isUnlocked) { Debug.Log($"[LoadoutScreen] Slot {index} is locked"); return; }
                OpenTurretVisualSelection(index);
            });
        }

        UpdateProjectileButtons(unlockedCount);
    }

    public void OpenTurretSelection()
    {
        if (turretSelectionPanel == null)
        {
            Debug.LogError("[LoadoutScreen] Turret selection panel reference is missing.");
            return;
        }

        turretSelectionPanel.SetActive(true);
        for (int i = 0; i < turretSlotButtons.Count; i++)
        {
            int index = i; // Local copy for closure
            turretSlotButtons[i].onClick.RemoveAllListeners();
            turretSlotButtons[i].onClick.AddListener(() => OpenTurretVisualSelection(index));
        }
    }

    public void CloseTurretSelection()
    {
        if (turretSelectionPanel != null)
            turretSelectionPanel.SetActive(false);
    }

    public void OpenTurretVisualSelection(int slotIndex)
    {
        if (turretSelectionPanel == null)
        {
            Debug.LogError("[LoadoutScreen] Turret selection panel reference is missing.");
            return;
        }

        // Block opening if slot is locked by skill
        if (slotIndex >= GetUnlockedSlotsCount()) { Debug.Log($"[LoadoutScreen] Slot {slotIndex} locked"); return; }

        EnsureEventSystem();

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

    private void UpdateProjectileButtons(int unlockedCount)
    {
        EnsureProjectileButtons(unlockedCount);

        var projectileManager = ProjectileDefinitionManager.Instance;
        if (projectileManager == null)
        {
            Debug.LogWarning("[LoadoutScreen] ProjectileDefinitionManager not present; projectile slots cannot be populated.");
            return;
        }

        var turretManager = TurretDefinitionManager.Instance;

        for (int i = 0; i < projectileButtons.Count; i++)
        {
            int index = i;
            var button = projectileButtons[i];

            if (!projectileSlotCache.TryGetValue(button, out var elements) || elements == null)
            {
                elements = CacheProjectileSlotElements(button);
                projectileSlotCache[button] = elements;
            }

            bool slotUnlocked = index < unlockedCount;

            string turretId = slotUnlocked ? playerManager.GetSelectedTurretForSlot(index) : string.Empty;
            TurretDefinition turretDef = !string.IsNullOrEmpty(turretId) ? turretManager?.GetById(turretId) : null;
            bool turretAssigned = slotUnlocked && turretDef != null;

            string projectileId = turretAssigned ? playerManager.GetSelectedProjectileForSlot(index) : string.Empty;
            ProjectileDefinition projectileDef = (!string.IsNullOrEmpty(projectileId) ? projectileManager?.GetById(projectileId) : null);

            if (projectileDef != null && turretDef != null && !turretDef.AcceptsProjectileType(projectileDef.projectileType))
            {
                Debug.LogWarning($"[LoadoutScreen] Projectile '{projectileDef.id}' no longer compatible with turret '{turretDef.id}'. Clearing assignment.");
                playerManager.SetSelectedProjectileForSlot(index, string.Empty);
                projectileDef = null;
                projectileId = string.Empty;
            }

            if (elements != null)
            {
                if (!slotUnlocked)
                {
                    if (elements.nameText) elements.nameText.text = "Locked";
                    if (elements.descriptionText) elements.descriptionText.text = "Unlock more slots";
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = null;
                        elements.previewImage.enabled = false;
                    }
                }
                else if (!turretAssigned)
                {
                    if (elements.nameText) elements.nameText.text = "No turret";
                    if (elements.descriptionText) elements.descriptionText.text = "Select a turret first";
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = null;
                        elements.previewImage.enabled = false;
                    }
                }
                else if (projectileDef != null)
                {
                    if (elements.nameText) elements.nameText.text = projectileDef.projectileName;
                    if (elements.descriptionText) elements.descriptionText.text = projectileDef.description;
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = projectileDef.icon;
                        elements.previewImage.enabled = projectileDef.icon != null;
                        elements.previewImage.preserveAspect = true;
                        elements.previewImage.color = Color.white;
                    }
                }
                else
                {
                    if (elements.nameText) elements.nameText.text = "Empty";
                    if (elements.descriptionText) elements.descriptionText.text = "Select a projectile";
                    if (elements.previewImage)
                    {
                        elements.previewImage.sprite = null;
                        elements.previewImage.enabled = false;
                    }
                }

                if (elements.lockedPanel) elements.lockedPanel.SetActive(!slotUnlocked);
                if (elements.choosePanel) elements.choosePanel.SetActive(slotUnlocked && turretAssigned && projectileDef == null);
                if (elements.requiresTurretPanel) elements.requiresTurretPanel.SetActive(slotUnlocked && !turretAssigned);
            }

            button.interactable = slotUnlocked && turretAssigned;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!slotUnlocked)
                {
                    Debug.Log($"[LoadoutScreen] Projectile slot {index} is locked");
                    return;
                }
                if (!turretAssigned)
                {
                    Debug.Log($"[LoadoutScreen] Assign a turret to slot {index} before selecting a projectile");
                    return;
                }
                OpenProjectileVisualSelection(index);
            });
        }
    }

    private void EnsureTurretSlotButtons(int requiredCount)
    {
        if (!turretContainerScanned)
        {
            turretContainerScanned = true;
            if (turretSlotsContainer != null)
            {
                turretSlotButtons.Clear();
                turretSlotCache.Clear();
                for (int i = 0; i < turretSlotsContainer.childCount; i++)
                {
                    var child = turretSlotsContainer.GetChild(i);
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        turretSlotButtons.Add(btn);
                        turretSlotCache[btn] = CacheTurretSlotElements(btn);
                    }
                }
            }
        }

        TrimButtonList(turretSlotButtons, turretSlotCache, requiredCount);

        while (turretSlotButtons.Count < requiredCount)
        {
            if (turretSlotButtonPrefab == null || turretSlotsContainer == null)
            {
                Debug.LogError("[LoadoutScreen] Turret slot prefab or container not assigned.");
                return;
            }

            var instance = Instantiate(turretSlotButtonPrefab, turretSlotsContainer);
            instance.name = $"TurretSlot_{turretSlotButtons.Count}";
            var button = instance.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[LoadoutScreen] Turret slot prefab is missing a Button component.");
                Destroy(instance);
                return;
            }

            turretSlotButtons.Add(button);
            turretSlotCache[button] = CacheTurretSlotElements(button);
        }
    }

    private void EnsureProjectileButtons(int requiredCount)
    {
        if (!projectileContainerScanned)
        {
            projectileContainerScanned = true;
            if (projectileSlotsContainer != null)
            {
                projectileButtons.Clear();
                projectileSlotCache.Clear();
                for (int i = 0; i < projectileSlotsContainer.childCount; i++)
                {
                    var child = projectileSlotsContainer.GetChild(i);
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        projectileButtons.Add(btn);
                        projectileSlotCache[btn] = CacheProjectileSlotElements(btn);
                    }
                }
            }
        }

        TrimButtonList(projectileButtons, projectileSlotCache, requiredCount);

        while (projectileButtons.Count < requiredCount)
        {
            if (projectileSlotButtonPrefab == null || projectileSlotsContainer == null)
            {
                Debug.LogError("[LoadoutScreen] Projectile slot prefab or container not assigned.");
                return;
            }

            var instance = Instantiate(projectileSlotButtonPrefab, projectileSlotsContainer);
            instance.name = $"ProjectileSlot_{projectileButtons.Count}";
            var button = instance.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[LoadoutScreen] Projectile slot prefab is missing a Button component.");
                Destroy(instance);
                return;
            }

            projectileButtons.Add(button);
            projectileSlotCache[button] = CacheProjectileSlotElements(button);
        }
    }

    private void TrimButtonList(List<Button> buttons, Dictionary<Button, TurretSlotUIElements> cache, int requiredCount)
    {
        for (int i = buttons.Count - 1; i >= requiredCount; i--)
        {
            var button = buttons[i];
            buttons.RemoveAt(i);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (cache != null) cache.Remove(button);
                Destroy(button.gameObject);
            }
        }
    }

    private void TrimButtonList(List<Button> buttons, Dictionary<Button, ProjectileSlotUIElements> cache, int requiredCount)
    {
        for (int i = buttons.Count - 1; i >= requiredCount; i--)
        {
            var button = buttons[i];
            buttons.RemoveAt(i);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (cache != null) cache.Remove(button);
                Destroy(button.gameObject);
            }
        }
    }

    private TurretSlotUIElements CacheTurretSlotElements(Button button)
    {
        if (button == null) return null;
        var transform = button.transform;
        return new TurretSlotUIElements
        {
            nameText = transform.Find("Info/TurretName")?.GetComponent<TextMeshProUGUI>(),
            descriptionText = transform.Find("Info/TurretDescription")?.GetComponent<TextMeshProUGUI>(),
            previewImage = transform.Find("Image")?.GetComponent<Image>(),
            lockedPanel = transform.Find("LockedPanel")?.gameObject,
            choosePanel = transform.Find("ChooseTurretPanel")?.gameObject
        };
    }

    private ProjectileSlotUIElements CacheProjectileSlotElements(Button button)
    {
        if (button == null) return null;
        var transform = button.transform;
        return new ProjectileSlotUIElements
        {
            nameText = transform.Find("Info/ProjectileName")?.GetComponent<TextMeshProUGUI>(),
            descriptionText = transform.Find("Info/ProjectileDescription")?.GetComponent<TextMeshProUGUI>(),
            previewImage = transform.Find("Image")?.GetComponent<Image>(),
            lockedPanel = transform.Find("LockedPanel")?.gameObject,
            choosePanel = transform.Find("ChooseProjectilePanel")?.gameObject,
            requiresTurretPanel = transform.Find("RequiresTurretPanel")?.gameObject
        };
    }

    public void OpenProjectileVisualSelection(int slotIndex)
    {
        if (projectileSelectionPanel == null)
        {
            Debug.LogError("[LoadoutScreen] Projectile selection panel reference is missing.");
            return;
        }

        if (slotIndex >= GetUnlockedSlotsCount())
        {
            Debug.Log($"[LoadoutScreen] Projectile slot {slotIndex} locked");
            return;
        }

        string turretId = playerManager.GetSelectedTurretForSlot(slotIndex);
        if (string.IsNullOrEmpty(turretId))
        {
            Debug.Log($"[LoadoutScreen] Cannot select projectile for slot {slotIndex} without a turret assigned");
            return;
        }

        EnsureEventSystem();

        projectileSelectionPanel.gameObject.SetActive(true);
        ProjectileSelectorUI selector = projectileSelectionPanel.GetComponent<ProjectileSelectorUI>();
        if (selector == null)
            selector = projectileSelectionPanel.GetComponentInChildren<ProjectileSelectorUI>(true);

        if (selector != null)
        {
            selector.SetSlotIndex(slotIndex);
            selector.SetLoadoutScreen(this);
            selector.PopulateOptions();
            Debug.Log($"[LoadoutScreen] Opened projectile selector for slot {slotIndex}");
        }
        else
        {
            Debug.LogError("[LoadoutScreen] ProjectileSelectorUI component not found under 'projectileSelectionPanel'. Add the component to that panel or a child and try again.");
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.LogWarning("[LoadoutScreen] No EventSystem found. Created one at runtime.");
        }
    }
}
