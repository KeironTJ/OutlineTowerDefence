using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Main Menu Header Reference")]
    [SerializeField] private CurrencyDisplayUI coresUI;
    [SerializeField] private CurrencyDisplayUI prismsUI;
    [SerializeField] private CurrencyDisplayUI loopsUI;
    [SerializeField] private CurrencyDefinition coresDef;
    [SerializeField] private CurrencyDefinition prismsDef;
    [SerializeField] private CurrencyDefinition loopsDef;

    [Header("Player Information")]
    [SerializeField] private TextMeshProUGUI playerUsernameText;

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

    [Header("Tower Visual")]
    [SerializeField] private Image towerVisualImage;

    [Header("Upgrade Screen References (Category Roots)")]
    [SerializeField] private Transform attackUpgradesScreen;
    [SerializeField] private Transform DefenceUpgradesScreen;
    [SerializeField] private Transform SupportUpgradesScreen;
    [SerializeField] private Transform SpecialUpgradesScreen;

    [Header("Category Buttons (Tabs)")]
    [SerializeField] private GameObject attackUpgradesButton;
    [SerializeField] private GameObject defenceUpgradesButton;
    [SerializeField] private GameObject supportUpgradesButton;
    [SerializeField] private GameObject specialUpgradesButton;

    [Header("Prefabs / UI")]
    [SerializeField] private GameObject upgradeButtonPrefab; // must have / or will receive MenuSkill

    [Header("Main Menu Footer Screens")]
    [SerializeField] private GameObject mainScreenUI;
    [SerializeField] private GameObject upgradeScreenUI;
    [SerializeField] private GameObject rewardScreenUI;
    [SerializeField] private GameObject researchScreenUI;
    [SerializeField] private GameObject settingsScreenUI;

    [Header("Main Menu Footer Buttons")]
    [SerializeField] private GameObject mainButton;
    [SerializeField] private GameObject upgradeButton;
    [SerializeField] private GameObject rewardButton;
    [SerializeField] private GameObject researchButton;
    [SerializeField] private GameObject settingsButton;

    [Header("Services")]
    [SerializeField] private SkillService skillService;

    private PlayerManager playerManager;
    private int minDifficultyLevel = 1;
    private int maxDifficultyLevel;

    private void Start()
    {
        playerManager = PlayerManager.main;
        if (!skillService) skillService = SkillService.Instance;

        DisplayPlayerUsername();
        DisplayCurrency();

        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged += OnBalanceChanged;

        // (MenuSkill instances will self-refresh on SkillUpgraded; we don't need to track entries)
        SelectScreen(ScreenType.Main);

        SetPlayerMaxDifficulty(playerManager.GetMaxDifficulty());
        SetPlayerDifficulty(1);
        InitializeMetaUpgradeShop();
        TriggerDifficultyButtons();
        ToggleCategory(attackUpgradesScreen);
        SetTowerVisualImage();
    }

    private void OnDestroy()
    {
        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged;
    }

    // ================= CURRENCY / HEADER =================
    private void OnBalanceChanged(CurrencyType type, float _)
    {
        if (type == CurrencyType.Cores || type == CurrencyType.Prisms || type == CurrencyType.Loops)
            DisplayCurrency();
    }

    public void DisplayCurrency()
    {
        if (playerManager?.Wallet == null) return;
        coresUI.SetCurrency(coresDef, playerManager.Wallet.Get(CurrencyType.Cores));
        prismsUI.SetCurrency(prismsDef, playerManager.Wallet.Get(CurrencyType.Prisms));
        loopsUI.SetCurrency(loopsDef, playerManager.Wallet.Get(CurrencyType.Loops));
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

    // ================= META UPGRADE SHOP (Cores) =================
    private void InitializeMetaUpgradeShop()
    {
        ClearGrid(attackUpgradesScreen);
        ClearGrid(DefenceUpgradesScreen);
        ClearGrid(SupportUpgradesScreen);
        ClearGrid(SpecialUpgradesScreen);

        BuildButtonsForCategory(SkillCategory.Attack, attackUpgradesScreen);
        BuildButtonsForCategory(SkillCategory.Defence, DefenceUpgradesScreen);
        BuildButtonsForCategory(SkillCategory.Support, SupportUpgradesScreen);
        BuildButtonsForCategory(SkillCategory.Special, SpecialUpgradesScreen);

        attackUpgradesScreen.gameObject.SetActive(true);
        DefenceUpgradesScreen.gameObject.SetActive(false);
        SupportUpgradesScreen.gameObject.SetActive(false);
        SpecialUpgradesScreen.gameObject.SetActive(false);
    }

    private void ClearGrid(Transform root)
    {
        if (!root) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private int BuildButtonsForCategory(SkillCategory category, Transform root)
    {
        if (!root || !skillService || playerManager == null) return 0;
        int built = 0;

        foreach (var def in skillService.GetByCategory(category))
        {
            if (!def) continue;

            var go = Instantiate(upgradeButtonPrefab, root);
            if (!go) continue;

            var btn = go.GetComponent<Button>();
            if (!btn)
            {
                Debug.LogError("Upgrade button prefab missing Button component.");
                Destroy(go);
                continue;
            }

            // Attach / fetch MenuSkill (shared component with in-round menu)
            var menuSkill = go.GetComponent<MenuSkill>() ?? go.AddComponent<MenuSkill>();
            // inRound = false => uses cores, meta progression
            menuSkill.Bind(def.id, playerManager.Wallet, false);

            // Button wiring: just forward to MenuSkill (it handles TryUpgradePersistent internally)
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(menuSkill.OnClickUpgrade);

            built++;
        }

        return built;
    }

    // ================= CATEGORY TOGGLING =================
    public void ToggleCategory(Transform categoryParent)
    {
        attackUpgradesScreen.gameObject.SetActive(false);
        DefenceUpgradesScreen.gameObject.SetActive(false);
        SupportUpgradesScreen.gameObject.SetActive(false);
        SpecialUpgradesScreen.gameObject.SetActive(false);

        categoryParent.gameObject.SetActive(true);
        SetCategoryButtonColours(categoryParent);
    }

    public void SetCategoryButtonColours(Transform categoryParent)
    {
        ChangeButtonColor(attackUpgradesButton, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(defenceUpgradesButton, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(supportUpgradesButton, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(specialUpgradesButton, new Color(0, 0, 0.5f, 1f));

        if (categoryParent == attackUpgradesScreen)
            ChangeButtonColor(attackUpgradesButton, Color.blue);
        else if (categoryParent == DefenceUpgradesScreen)
            ChangeButtonColor(defenceUpgradesButton, Color.blue);
        else if (categoryParent == SupportUpgradesScreen)
            ChangeButtonColor(supportUpgradesButton, Color.blue);
        else if (categoryParent == SpecialUpgradesScreen)
            ChangeButtonColor(specialUpgradesButton, Color.blue);
    }

    // ================= FOOTER NAV =================
    public enum ScreenType { Main, Upgrade, Reward, Research, Settings }

    public void SelectScreen(ScreenType screenType)
    {
        mainScreenUI.SetActive(screenType == ScreenType.Main);
        upgradeScreenUI.SetActive(screenType == ScreenType.Upgrade);
        rewardScreenUI.SetActive(screenType == ScreenType.Reward);
        researchScreenUI.SetActive(screenType == ScreenType.Research);
        settingsScreenUI.SetActive(screenType == ScreenType.Settings);

        ResetScreenButtonColors();
        switch (screenType)
        {
            case ScreenType.Main:      ChangeButtonColor(mainButton, Color.black); break;
            case ScreenType.Upgrade:   ChangeButtonColor(upgradeButton, Color.black); break;
            case ScreenType.Reward:    ChangeButtonColor(rewardButton, Color.black); break;
            case ScreenType.Research:  ChangeButtonColor(researchButton, Color.black); break;
            case ScreenType.Settings:  ChangeButtonColor(settingsButton, Color.black); break;
        }
    }

    private void ResetScreenButtonColors()
    {
        ChangeButtonColor(mainButton, Color.blue);
        ChangeButtonColor(upgradeButton, Color.blue);
        ChangeButtonColor(rewardButton, Color.blue);
        ChangeButtonColor(researchButton, Color.blue);
        ChangeButtonColor(settingsButton, Color.blue);
    }

    public void SelectMainScreen()     => SelectScreen(ScreenType.Main);
    public void SelectUpgradeScreen()  => SelectScreen(ScreenType.Upgrade);
    public void SelectRewardScreen()   => SelectScreen(ScreenType.Reward);
    public void SelectResearchScreen() => SelectScreen(ScreenType.Research);
    public void SelectSettingsScreen() => SelectScreen(ScreenType.Settings);

    // ================= SCENE / QUIT =================
    public void ChooseScene(string sceneName)
    {
        SaveManager.main?.QueueImmediateSave();
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame() => Application.Quit();

    // ================= UTIL =================
    public void ChangeButtonColor(GameObject button, Color color)
    {
        if (!button) return;
        var img = button.GetComponent<Image>();
        if (img) img.color = color;
    }
}
