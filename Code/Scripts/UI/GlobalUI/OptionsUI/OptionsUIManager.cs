using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsUIManager : SingletonMonoBehaviour<OptionsUIManager>
{
    [Header("Panels")]
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject gameStatsPanel;
    [SerializeField] private GameObject roundHistoryPanel;   // the panel root in Options
    [SerializeField] private GameObject storePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject chipsPanel;
    [SerializeField] private ChipSelectorUI chipSelectorUI;

    [SerializeField] private RewardsUIController rewardsController; // assign in Inspector if possible

    protected override void OnAwakeAfterInit()
    {
        // Auto-bind children if missing (by name)
        if (!background) background = transform.Find("Background") ? transform.Find("Background").gameObject : null;
        if (!optionsPanel) optionsPanel = transform.Find("OptionsPanel") ? transform.Find("OptionsPanel").gameObject : null;
        if (!profilePanel) profilePanel = transform.Find("ProfilePanel") ? transform.Find("ProfilePanel").gameObject : null;
        if (!gameStatsPanel) gameStatsPanel = transform.Find("GameStatsPanel") ? transform.Find("GameStatsPanel").gameObject : null;
        if (!roundHistoryPanel) roundHistoryPanel = transform.Find("RoundHistoryPanel") ? transform.Find("RoundHistoryPanel").gameObject : null;
        if (!settingsPanel) settingsPanel = transform.Find("SettingsPanel") ? transform.Find("SettingsPanel").gameObject : null;
        if (!storePanel) storePanel = transform.Find("StorePanel") ? transform.Find("StorePanel").gameObject : null;
        if (!chipsPanel) chipsPanel = transform.Find("ChipsPanel") ? transform.Find("ChipsPanel").gameObject : null;
        if (!chipSelectorUI && chipsPanel)
            chipSelectorUI = chipsPanel.GetComponentInChildren<ChipSelectorUI>(true);

        CloseOptions();
    }

    public void OpenOptions()
    {
        SafeSetActive(background, true);
        SafeSetActive(optionsPanel, true);
        HideSubPanels();
    }

    public void CloseOptions()
    {
        SafeSetActive(optionsPanel, false);
        SafeSetActive(profilePanel, false);
        SafeSetActive(gameStatsPanel, false);
        SafeSetActive(roundHistoryPanel, false);
        SafeSetActive(settingsPanel, false);
        SafeSetActive(chipsPanel, false);
        SafeSetActive(background, false);
    }

    public void ToggleOptions()
    {
        bool open = IsOpen();
        if (open) CloseOptions();
        else OpenOptions();
    }

    public void ShowProfile() => ShowSubPanel(profilePanel);
    public void ShowGameStats() => ShowSubPanel(gameStatsPanel);
    public void ShowSettings() => ShowSubPanel(settingsPanel);
    public void ShowRoundHistory() => ShowSubPanel(roundHistoryPanel);
    public void ShowRewards() => OnRewardsButton();
    public void ShowStore() => ShowSubPanel(storePanel);
    public void ShowChips()
    {
        if (!IsValid(chipsPanel))
        {
            Debug.LogWarning("OptionsUIManager: Chips panel missing.");
            return;
        }

        HideSubPanels();
        SafeSetActive(chipsPanel, true);

        if (chipSelectorUI == null)
            chipSelectorUI = chipsPanel.GetComponentInChildren<ChipSelectorUI>(true);

        chipSelectorUI?.RefreshUI();
    }

    public void ShowPanel(GameObject panel)
    {
        if (!IsValid(panel)) { Debug.LogWarning("OptionsUIManager: Panel missing."); return; }
        HideSubPanels();
        SafeSetActive(panel, true);
    }

    private void ShowSubPanel(GameObject panel)
    {
        if (!IsValid(panel)) { Debug.LogWarning("OptionsUIManager: SubPanel missing."); return; }

        // Ensure menu is open
        //SafeSetActive(background, true);
        //SafeSetActive(optionsPanel, true);

        // Show just the requested subpanel
        HideSubPanels();
        SafeSetActive(panel, true);
    }

    public void HideSubPanels()
    {
        SafeSetActive(profilePanel, false);
        SafeSetActive(gameStatsPanel, false);
        SafeSetActive(settingsPanel, false);
        SafeSetActive(roundHistoryPanel, false);
        SafeSetActive(storePanel, false);
        SafeSetActive(chipsPanel, false);
    }

    private bool IsOpen()
    {
        return background && background.activeSelf && optionsPanel && optionsPanel.activeSelf;
    }

    private static bool IsValid(GameObject go) => go != null && go.scene.IsValid();

    private static void SafeSetActive(GameObject go, bool state)
    {
        if (go && go.activeSelf != state) go.SetActive(state);
    }

    public void OnRewardsButton()
    {
        if (rewardsController == null)
            rewardsController = FindFirstObjectByType<RewardsUIController>();
        if (rewardsController != null)
            rewardsController.Toggle();
        else
            Debug.LogWarning("RewardsUIController not found. Make sure it's added in MainMenu and persists.");
    }

    public void OnRewardsButtonDaily()
    {
        if (rewardsController == null)
            rewardsController = FindFirstObjectByType<RewardsUIController>();
        if (rewardsController != null)
            rewardsController.OpenDailyRewards();
        else
            Debug.LogWarning("RewardsUIController not found. Make sure it's added in MainMenu and persists.");
    }

    public void OnRewardsButtonWeekly()
    {
        if (rewardsController == null)
            rewardsController = FindFirstObjectByType<RewardsUIController>();
        if (rewardsController != null)
            rewardsController.OpenWeeklyRewards();
        else
            Debug.LogWarning("RewardsUIController not found. Make sure it's added in MainMenu and persists.");
    }

    public void OnRewardsButtonAchievements()
    {
        if (rewardsController == null)
            rewardsController = FindFirstObjectByType<RewardsUIController>();
        if (rewardsController != null)
            rewardsController.OpenAchievements();
        else
            Debug.LogWarning("RewardsUIController not found. Make sure it's added in MainMenu and persists.");
    }

}
