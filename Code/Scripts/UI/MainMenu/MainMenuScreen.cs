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
    private int minDifficultyLevel = 1;
    private int maxDifficultyLevel;

    private void Start()
    {
        playerManager = PlayerManager.main;

        DisplayPlayerUsername();
        SetPlayerMaxDifficulty(playerManager.GetMaxDifficulty());
        SetPlayerDifficulty(1);
        TriggerDifficultyButtons();
    }

    private void OnEnable()
    {
        DisplayPlayerUsername();
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
}