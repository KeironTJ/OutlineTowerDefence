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

    private PlayerManager playerManager;

    [Header("Difficulty Range")]
    [SerializeField] private int minDifficultyLevel = 1;
    [SerializeField] private int maxDifficultyUnlocked;
    [SerializeField] private int maxDifficultyLevel = 9;

    private void Start()
    {
        if (!EnsurePlayerManager()) return;

        RefreshDifficultyContext();

        int initialDifficulty = playerManager.GetDifficulty();
        if (!playerManager.CanSelectDifficulty(initialDifficulty))
            initialDifficulty = playerManager.ClampDifficultyToUnlocked(initialDifficulty);

        SetPlayerDifficulty(initialDifficulty > 0 ? initialDifficulty : minDifficultyLevel);
        DisplayPlayerUsername();
        TriggerDifficultyButtons();
    }

    private void OnEnable()
    {
        if (!EnsurePlayerManager()) return;

        RefreshDifficultyContext();
        DisplayPlayerUsername();
        TriggerDifficultyButtons();
    }

    private bool EnsurePlayerManager()
    {
        if (playerManager != null) return true;
        playerManager = PlayerManager.main;
        if (playerManager == null)
        {
            Debug.LogWarning("[MainMenuScreen] PlayerManager not ready; UI will retry next enable.");
            return false;
        }
        return true;
    }

    private void RefreshDifficultyContext()
    {
        if (playerManager == null) return;
        minDifficultyLevel = playerManager.GetMinDifficultyLevel();
        maxDifficultyLevel = playerManager.GetMaxConfiguredDifficultyLevel();
        maxDifficultyUnlocked = playerManager.GetHighestUnlockedDifficulty();
    }

    // ================= PLAYER / USERNAME =================
    public void DisplayPlayerUsername()
    {
        if (!EnsurePlayerManager()) return;
        if (playerUsernameText && playerManager?.playerData != null)
            playerUsernameText.text = playerManager.playerData.Username;
    }

    public void OpenChangeUsernamePanel()
    {
        if (!EnsurePlayerManager()) return;
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
        if (!EnsurePlayerManager()) return;
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

    // ================= DIFFICULTY =================
    public void DisplayDifficulty()
    {
        if (difficultySelectionUI)
            difficultySelectionUI.text = chosenDifficulty.ToString();
    }
    public void DisplayHighestWave()
    {
        if (!EnsurePlayerManager()) return;
        highestWave = playerManager.GetHighestWave(chosenDifficulty);
        if (highestWaveUI)
            highestWaveUI.text = $"Best Wave: {highestWave}";
    }
    public void TriggerDifficultyButtons()
    {
        if (!EnsurePlayerManager()) return;

        RefreshDifficultyContext();

        chosenDifficulty = playerManager.ClampDifficultyToUnlocked(chosenDifficulty);

        if (lowerDifficultyButton)
            lowerDifficultyButton.gameObject.SetActive(chosenDifficulty > minDifficultyLevel);

        bool canIncrease = chosenDifficulty < maxDifficultyLevel && playerManager.CanSelectDifficulty(chosenDifficulty + 1);
        if (increaseDifficultyButton)
            increaseDifficultyButton.gameObject.SetActive(canIncrease);

        SetPlayerDifficulty(chosenDifficulty);
        SetPlayerHighestWave(chosenDifficulty);
        DisplayDifficulty();
        DisplayHighestWave();
    }
    public void LowerPlayerDifficulty()
    {
        if (!EnsurePlayerManager()) return;
        if (chosenDifficulty <= minDifficultyLevel) return;
        SetPlayerDifficulty(chosenDifficulty - 1);
        TriggerDifficultyButtons();
    }
    public void IncreasePlayerDifficulty()
    {
        if (!EnsurePlayerManager()) return;
        int target = Mathf.Min(chosenDifficulty + 1, maxDifficultyLevel);
        if (!playerManager.CanSelectDifficulty(target)) return;
        SetPlayerDifficulty(target);
        TriggerDifficultyButtons();
    }
    public void SetPlayerDifficulty(int d)
    {
        if (!EnsurePlayerManager()) return;
        chosenDifficulty = playerManager.ClampDifficultyToUnlocked(d);
        playerManager.SetDifficulty(chosenDifficulty);
    }
    public void SetPlayerHighestWave(int difficulty)
    {
        if (!EnsurePlayerManager()) return;
        highestWave = playerManager.GetHighestWave(difficulty);
    }

    public void ChooseScene(string sceneName)
    {
        SaveManager.main?.QueueImmediateSave();
        SceneManager.LoadScene(sceneName);
    }
}