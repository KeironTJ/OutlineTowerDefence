using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuScreen : MonoBehaviour
{
    [Header("Player Information")]
    [SerializeField] private TextMeshProUGUI playerUsernameText;

    [Header("Tower Visual")]
    [SerializeField] private Image towerVisualImage;

    [Header("Change Username Section")]
    [SerializeField] private GameObject changeUsernamePanel;
    [SerializeField] private TMP_InputField changeUsernameInputField;
    [SerializeField] private TextMeshProUGUI changeUsernameErrorText;
    [SerializeField] private Button changeUsernameButton;

    [Header("Difficulty")]
    [SerializeField] private TextMeshProUGUI difficultySelectionUI;
    [SerializeField] private TextMeshProUGUI highestWaveUI;
    [SerializeField] private Transform lowerDifficultyButton;
    [SerializeField] private Transform increaseDifficultyButton;
    [SerializeField] private int chosenDifficulty = 1;
    [SerializeField] private int highestWave;

    [Header("Turret Selection")]
    [SerializeField] private GameObject turretSelectionPanel;
    [SerializeField] private List<Button> slotButtons;

    [Header("Turret Slots (simple gating)")]
    [SerializeField] private SkillService skillService;          // assign in Inspector (or auto-find)
    [SerializeField] private string turretSlotsSkillId = "TurretSlots";
    [SerializeField] private int defaultTotalSlots = 1;          // fallback if skill not ready
    [SerializeField] private int debugOverrideTotalSlots = -1;   // set >=0 to force (for testing)

    private PlayerManager playerManager;
    private int minDifficultyLevel = 1;
    private int maxDifficultyLevel;

    private void Start()
    {
        playerManager = PlayerManager.main;
        skillService = SkillService.Instance;

        DisplayPlayerUsername();
        SetPlayerMaxDifficulty(playerManager.GetMaxDifficulty());
        SetPlayerDifficulty(1);
        TriggerDifficultyButtons();

        SetTowerVisualImage();

        UpdateSlotButtons();
    }

    private void OnEnable()
    {
        DisplayPlayerUsername();
        SetTowerVisualImage();
        UpdateSlotButtons();
    }

    // ================= PLAYER / USERNAME =================
    public void DisplayPlayerUsername()
    {
        if (playerUsernameText && playerManager?.playerData != null)
            playerUsernameText.text = playerManager.playerData.Username;
    }

    public void OpenChangeUsernamePanel()
    {
        changeUsernameInputField.text = playerManager.playerData.Username;
        changeUsernamePanel.SetActive(true);
    }
    public void CloseChangeUsernamePanel()
    {
        changeUsernamePanel.SetActive(false);
        changeUsernameErrorText.text = "";
        changeUsernameInputField.text = "";
    }
    public void ChangeUsername()
    {
        string newUsername = changeUsernameInputField.text.Trim();
        if (string.IsNullOrEmpty(newUsername))
        {
            changeUsernameErrorText.text = "Username cannot be empty.";
            return;
        }
        if (playerManager.UpdateUsername(newUsername))
        {
            changeUsernameErrorText.text = "";
            DisplayPlayerUsername();
            CloseChangeUsernamePanel();
        }
        else
            changeUsernameErrorText.text = "Failed to update username.";
    }

    // ================= TOWER VISUAL =================
    public void SetTowerVisualImage()
    {
        if (!towerVisualImage || playerManager?.playerData == null) return;
        var selectedId = playerManager.playerData.selectedTowerVisualId;
        var visuals = TowerVisualManager.Instance.allVisuals;
        foreach (var v in visuals)
        {
            if (v.id == selectedId)
            {
                towerVisualImage.sprite = v.previewSprite;
                break;
            }
        }
    }

    // ================= DIFFICULTY =================
    public void DisplayDifficulty() => difficultySelectionUI.text = chosenDifficulty.ToString();
    public void DisplayHighestWave()
    {
        highestWave = playerManager.GetHighestWave(chosenDifficulty);
        highestWaveUI.text = $"Best Wave: {highestWave}";
    }
    public void TriggerDifficultyButtons()
    {
        lowerDifficultyButton.gameObject.SetActive(chosenDifficulty > minDifficultyLevel);
        increaseDifficultyButton.gameObject.SetActive(chosenDifficulty < maxDifficultyLevel);
        SetPlayerDifficulty(chosenDifficulty);
        SetPlayerHighestWave(chosenDifficulty);
        DisplayDifficulty();
        DisplayHighestWave();
    }
    public void LowerPlayerDifficulty()
    {
        if (chosenDifficulty <= minDifficultyLevel) return;
        chosenDifficulty--;
        TriggerDifficultyButtons();
    }
    public void IncreasePlayerDifficulty()
    {
        if (chosenDifficulty >= maxDifficultyLevel) return;
        chosenDifficulty++;
        TriggerDifficultyButtons();
    }
    public void SetPlayerDifficulty(int d)
    {
        chosenDifficulty = d;
        playerManager.SetDifficulty(chosenDifficulty);
    }
    public void SetPlayerMaxDifficulty(int maxDifficulty) => maxDifficultyLevel = maxDifficulty;
    public void SetPlayerHighestWave(int difficulty) => highestWave = playerManager.GetHighestWave(difficulty);

    public void ChooseScene(string sceneName)
    {
        SaveManager.main?.QueueImmediateSave();
        SceneManager.LoadScene(sceneName);
    }

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
                Debug.LogWarning($"[MainMenu] No TurretDefinition for id '{selectedId}'");

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
                if (!isUnlocked) { Debug.Log($"[MainMenu] Slot {index} is locked"); return; }
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
        if (slotIndex >= GetUnlockedSlotsCount()) { Debug.Log($"[MainMenu] Slot {slotIndex} locked"); return; }

        // Ensure we have an EventSystem + GraphicRaycaster (otherwise buttons won't click)
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.LogWarning("[MainMenu] No EventSystem found. Created one at runtime.");
        }

        turretSelectionPanel.gameObject.SetActive(true);
        // Selector might be on a child; search in children (include inactive)
        TurretSelectorUI turretSelectorUI = turretSelectionPanel.GetComponent<TurretSelectorUI>();
        if (turretSelectorUI == null)
            turretSelectorUI = turretSelectionPanel.GetComponentInChildren<TurretSelectorUI>(true);

        if (turretSelectorUI != null)
        {
            turretSelectorUI.SetSlotIndex(slotIndex);
            turretSelectorUI.SetMainMenu(this); // allow selector to notify when a pick is made
            turretSelectorUI.PopulateOptions();
            Debug.Log($"[MainMenu] Opened turret selector for slot {slotIndex}");
        }
        else
        {
            Debug.LogError("[MainMenu] TurretSelectorUI component not found under 'turretSelectionPanel'. " +
                           "Add the component to that panel or a child and try again.");
        }
    }

}