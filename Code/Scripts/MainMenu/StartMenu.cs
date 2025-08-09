using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class StartMenu : MonoBehaviour
{

    [Header("Main Menu Header Reference")]
    [SerializeField] private TextMeshProUGUI premiumCreditsUI;
    [SerializeField] private TextMeshProUGUI specialCreditsUI;
    [SerializeField] private TextMeshProUGUI luxuryCreditsUI;

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

    // Tower Visual Selection
    [Header("Tower Visual")]
    [SerializeField] private Image towerVisualImage;


    [Header("Upgrade Screen References")]
    // Screens
    [SerializeField] private Transform attackUpgradesScreen;
    [SerializeField] private Transform DefenceUpgradesScreen;
    [SerializeField] private Transform SupportUpgradesScreen;
    [SerializeField] private Transform SpecialUpgradesScreen;

    // Buttons
    [SerializeField] private GameObject attackUpgradesButton;
    [SerializeField] private GameObject defenceUpgradesButton;
    [SerializeField] private GameObject supportUpgradesButton;
    [SerializeField] private GameObject specialUpgradesButton;

    [SerializeField] private GameObject upgradeButtonPrefab;

    [Header("Main Menu Footer")]
    // SCREENS
    [SerializeField] private GameObject mainScreenUI;
    [SerializeField] private GameObject upgradeScreenUI;
    [SerializeField] private GameObject rewardScreenUI;
    [SerializeField] private GameObject researchScreenUI;
    [SerializeField] private GameObject settingsScreenUI;

    // SCREEN BUTTONS
    [SerializeField] private GameObject mainButton;
    [SerializeField] private GameObject upgradeButton;
    [SerializeField] private GameObject rewardButton;
    [SerializeField] private GameObject researchButton;
    [SerializeField] private GameObject settingsButton;


    private PlayerManager playerManager;

    private int minDifficultyLevel = 1;
    private int maxDifficultyLevel;

    private void Start()
    {
        playerManager = PlayerManager.main;
        DisplayPlayerUsername();
        SetPlayerMaxDifficulty(playerManager.GetMaxDifficulty());
        SetPlayerDifficulty(1); // In future can customize this to suit player last chosen level
        SelectScreen(ScreenType.Main);
        InitializeShop();
        TriggerDifficultyButtons();
        ToggleCategory(attackUpgradesScreen);
        // Load and display the selected tower visual image
        SetTowerVisualImage();

    }

    // Sets the tower image in the start menu to the selected tower visual

    private void Update()
    {
        DisplayCredits();
    }

    // HEADER METHODS

    public void DisplayCredits()
    {
        premiumCreditsUI.text = "£" + NumberManager.FormatLargeNumber(playerManager.playerData.premiumCredits);
        specialCreditsUI.text = "£" + NumberManager.FormatLargeNumber(playerManager.playerData.specialCredits);
        luxuryCreditsUI.text = "£" + NumberManager.FormatLargeNumber(playerManager.playerData.luxuryCredits);
    }

    // PLAYER INFORMATION METHODS
    public void DisplayPlayerUsername()
    {
        playerUsernameText.text = playerManager.playerData.Username;
    }

    public void SetTowerVisualImage()
    {
        if (towerVisualImage == null) return;
        var selectedId = playerManager.playerData.selectedTowerVisualId;
        var visuals = TowerVisualManager.Instance.allVisuals;
        foreach (var visual in visuals)
        {
            if (visual.id == selectedId)
            {
                towerVisualImage.sprite = visual.previewSprite;
                break;
            }
        }
    }

    // Change Username Methods
    public void OpenChangeUsernamePanel()
    {
        changeUsernameInputField.text = playerManager.playerData.Username;
        changeUsernamePanel.SetActive(true);
    }

    public void CloseChangeUsernamePanel()
    {
        changeUsernamePanel.SetActive(false);
        changeUsernameErrorText.text = ""; // Clear any previous error messages
        changeUsernameInputField.text = ""; // Clear the input field
    }

    public void ChangeUsername()
    {
        string newUsername = changeUsernameInputField.text.Trim();
        if (!string.IsNullOrEmpty(newUsername))
        {
            if (playerManager.UpdateUsername(newUsername))
            {
                changeUsernameErrorText.text = ""; // Clear any previous error messages
                DisplayPlayerUsername();
                CloseChangeUsernamePanel();
            }
            else
            {
                changeUsernameErrorText.text = "Failed to update username. It may already be taken.";
            }

        }
        else
        {
            changeUsernameErrorText.text = "Username cannot be empty.";
        }
    }

    // PLAY SCREEN METHODS

    public void DisplayDifficulty()
    {
        difficultySelectionUI.text = chosenDifficulty.ToString();
    }

    public void DisplayHighestWave()
    {
        highestWave = playerManager.GetHighestWave(chosenDifficulty);
        highestWaveUI.text = $"Best Wave: {highestWave.ToString()}";
    }

    public void TriggerDifficultyButtons()
    {
        lowerDifficultyButton.gameObject.SetActive(chosenDifficulty > minDifficultyLevel);
        increaseDifficultyButton.gameObject.SetActive(chosenDifficulty < maxDifficultyLevel);
        SetPlayerDifficulty(chosenDifficulty);
        SetPlayerHighestWave(chosenDifficulty);
        DisplayDifficulty();
        DisplayHighestWave();
        Debug.Log($"Difficulty set to: {chosenDifficulty}, Highest Wave: {highestWave}");
    }

    public void LowerPlayerDifficulty()
    {
        if (chosenDifficulty > minDifficultyLevel)
        {
            chosenDifficulty--;
            TriggerDifficultyButtons();
        }
    }

    public void SetPlayerDifficulty(int difficulty)
    {
        chosenDifficulty = difficulty;
        playerManager.SetDifficulty(chosenDifficulty);
    }

    public void SetPlayerMaxDifficulty(int maxDifficulty)
    {
        maxDifficultyLevel = maxDifficulty;
    }

    public void SetPlayerHighestWave(int difficulty)
    {
        highestWave = playerManager.GetHighestWave(difficulty);
    }

    public void IncreasePlayerDifficulty()
    {
        if (chosenDifficulty < maxDifficultyLevel)
        {
            chosenDifficulty++;
            TriggerDifficultyButtons();
        }
    }

    // THIS METHOFS STARTS A NEW ROUND !!
    public void ChooseScene(string sceneName)
    {
        playerManager.saveLoadManager.SaveData(playerManager.playerData);
        SceneManager.LoadScene(sceneName);
    }


    // UPGRADE SHOP MEHTODS

    private void InitializeShop()
    {
        CreateSkillsButtons(playerManager.attackSkills, attackUpgradesScreen);
        CreateSkillsButtons(playerManager.defenceSkills, DefenceUpgradesScreen);
        CreateSkillsButtons(playerManager.supportSkills, SupportUpgradesScreen);
        CreateSkillsButtons(playerManager.specialSkills, SpecialUpgradesScreen);

        attackUpgradesScreen.gameObject.SetActive(true);
        DefenceUpgradesScreen.gameObject.SetActive(false);
        SupportUpgradesScreen.gameObject.SetActive(false);
        SpecialUpgradesScreen.gameObject.SetActive(false);
    }


    private void CreateSkillsButtons(Dictionary<string, Skill> skills, Transform parent)
    {
        foreach (var skill in skills.Values)
        {
            if (skill.skillActive && skill.playerSkillUnlocked)
            {
                GameObject button = Instantiate(upgradeButtonPrefab, parent);
                Button buttonComponent = button.GetComponent<Button>();
                UpdateButtonText(skill, buttonComponent);

                buttonComponent.onClick.AddListener(() => OnSkillButtonClicked(skill, buttonComponent));
            }
        }
    }

    public void ToggleCategory(Transform categoryParent)
    {
        // FIRST HIDE ALL SCREENS (Reset)
        attackUpgradesScreen.gameObject.SetActive(false);
        DefenceUpgradesScreen.gameObject.SetActive(false);
        SupportUpgradesScreen.gameObject.SetActive(false);
        SpecialUpgradesScreen.gameObject.SetActive(false);

        // SHOW THE REQUIRED SCREEN ONLY
        categoryParent.gameObject.SetActive(true);
        SetCategoryButtonColours(categoryParent);

    }

    public void SetCategoryButtonColours(Transform categoryParent)
    {
        ChangeButtonColor(attackUpgradesButton, new Color(0, 0, 0.5f, 1f)); // Blue with 50% opacity
        ChangeButtonColor(defenceUpgradesButton, new Color(0, 0, 0.5f, 1f)); // Blue with 50% opacity
        ChangeButtonColor(supportUpgradesButton, new Color(0, 0, 0.5f, 1f)); // Blue with 50% opacity
        ChangeButtonColor(specialUpgradesButton, new Color(0, 0, 0.5f, 1f)); // Blue with 50% opacity

        if (categoryParent.name == "AttackUpgradesParent")
        {
            ChangeButtonColor(attackUpgradesButton, new Color(0, 0, 1, 1f)); // Blue with 100% opacity
        }
        else if (categoryParent.name == "DefenceUpgradesParent")
        {
            ChangeButtonColor(defenceUpgradesButton, new Color(0, 0, 1, 1f)); // Blue with 100% opacity
        }
        else if (categoryParent.name == "SupportUpgradesParent")
        {
            ChangeButtonColor(supportUpgradesButton, new Color(0, 0, 1, 1f)); // Blue with 100% opacity
        }
        else if (categoryParent.name == "SpecialUpgradesParent")
        {
            ChangeButtonColor(specialUpgradesButton, new Color(0, 0, 1, 1f)); // Blue with 100% opacity
        }
    }




    // Upgrades the skill associated with the button. 
    private void OnSkillButtonClicked(Skill skill, Button button)
    {
        if (playerManager.SpendPremiumCredits(playerManager.GetSkillCost(skill)))
        {
            playerManager.UpgradeSkill(skill, 1);
            UpdateButtonText(skill, button);
        }
    }

    // UPDATE THE BUTTON TEXT UI
    private void UpdateButtonText(Skill skill, Button button)
    {
        TextMeshProUGUI[] textFields = button.GetComponentsInChildren<TextMeshProUGUI>();
        if (textFields.Length >= 3)
        {
            textFields[0].text = skill.skillName;
            textFields[1].text = NumberManager.FormatLargeNumber(playerManager.GetSkillCost(skill));
            textFields[2].text = NumberManager.FormatLargeNumber(playerManager.GetSkillValue(skill));
        }
    }


    // FOOTER METHODS

    public enum ScreenType
    {
        Main,
        Upgrade,
        Reward,
        Research,
        Settings
    }

    public void SelectScreen(ScreenType screenType)
    {
        mainScreenUI.SetActive(screenType == ScreenType.Main);
        upgradeScreenUI.SetActive(screenType == ScreenType.Upgrade);
        rewardScreenUI.SetActive(screenType == ScreenType.Reward);
        researchScreenUI.SetActive(screenType == ScreenType.Research);
        settingsScreenUI.SetActive(screenType == ScreenType.Settings);

        // Reset all button colors to default
        ResetScreenButtonColors();

        // Change the color of the selected button
        switch (screenType)
        {
            case ScreenType.Main:
                ChangeButtonColor(mainButton, Color.black);
                break;
            case ScreenType.Upgrade:
                ChangeButtonColor(upgradeButton, Color.black);
                break;
            case ScreenType.Reward:
                ChangeButtonColor(rewardButton, Color.black);
                break;
            case ScreenType.Research:
                ChangeButtonColor(researchButton, Color.black);
                break;
            case ScreenType.Settings:
                ChangeButtonColor(settingsButton, Color.black);
                break;
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

    public void SelectMainScreen()
    {
        SelectScreen(ScreenType.Main);
    }

    public void SelectUpgradeScreen()
    {
        SelectScreen(ScreenType.Upgrade);
    }

    public void SelectRewardScreen()
    {
        SelectScreen(ScreenType.Reward);
    }

    public void SelectResearchScreen()
    {
        SelectScreen(ScreenType.Research);
    }

    public void SelectSettingsScreen()
    {
        SelectScreen(ScreenType.Settings);
    }

    public void QuitGame()
    {
        Application.Quit();
    }


    // SUPPORTING METHODS
    public void ChangeButtonColor(GameObject button, Color color)
    {
        var buttonImage = button.GetComponent<UnityEngine.UI.Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }
}
