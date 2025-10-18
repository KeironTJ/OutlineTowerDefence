using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsUIButtonProxy : MonoBehaviour
{
    public void Open() => OptionsUIManager.Instance?.OpenOptions();
    public void Close() => OptionsUIManager.Instance?.CloseOptions();
    public void Toggle() => OptionsUIManager.Instance?.ToggleOptions();
    public void ShowSettings() => OptionsUIManager.Instance?.ShowSettings();
    public void ShowProfile() => OptionsUIManager.Instance?.ShowProfile();
    public void ShowGameStats() => OptionsUIManager.Instance?.ShowGameStats();
    public void ShowRoundHistory() => OptionsUIManager.Instance?.ShowRoundHistory();
    public void ShowRewards() => OptionsUIManager.Instance?.ShowRewards();
    public void ShowDailyRewards() => OptionsUIManager.Instance?.OnRewardsButtonDaily();
    public void ShowWeeklyRewards() => OptionsUIManager.Instance?.OnRewardsButtonWeekly();
    public void ShowAchievements() => OptionsUIManager.Instance?.OnRewardsButtonAchievements();
    public void ShowStore() => OptionsUIManager.Instance?.ShowStore();
}

