using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the notification system
/// This is a reference implementation - you can copy patterns from here
/// </summary>
public class NotificationExamples : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField] private bool enableTestHotkeys = true;

    private void Update()
    {
        if (!enableTestHotkeys) return;

        // Press Q for quick notification
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowQuickNotificationExample();
        }

        // Press M for modal notification
        if (Input.GetKeyDown(KeyCode.M))
        {
            ShowModalNotificationExample();
        }

        // Press R for rewards notification
        if (Input.GetKeyDown(KeyCode.R))
        {
            ShowRewardsNotificationExample();
        }

        // Press A for achievement notification
        if (Input.GetKeyDown(KeyCode.A))
        {
            ShowAchievementExample();
        }

        // Press O for objective notification
        if (Input.GetKeyDown(KeyCode.O))
        {
            ShowObjectiveExample();
        }
    }

    /// <summary>
    /// Example: Simple quick notification
    /// Use for: Minor events, status updates, progress notifications
    /// </summary>
    private void ShowQuickNotificationExample()
    {
        if (NotificationManager.Instance == null)
        {
            Debug.LogWarning("NotificationManager not found!");
            return;
        }

        NotificationManager.Instance.ShowQuickNotification(
            "Wave Complete!",
            "You survived wave 10! Keep going!",
            NotificationSource.System,
            duration: 3f
        );
    }

    /// <summary>
    /// Example: Modal notification without rewards
    /// Use for: Important announcements, tutorial messages
    /// </summary>
    private void ShowModalNotificationExample()
    {
        if (NotificationManager.Instance == null) return;

        NotificationManager.Instance.ShowModalNotification(
            "Welcome!",
            "This is a modal notification that requires your attention. Click close to continue.",
            null, // No rewards
            NotificationSource.System,
            "welcome_message"
        );
    }

    /// <summary>
    /// Example: Modal notification with currency rewards
    /// Use for: Pack openings, bonus rewards, milestone rewards
    /// </summary>
    private void ShowRewardsNotificationExample()
    {
        if (NotificationManager.Instance == null) return;

        var rewards = new NotificationReward[]
        {
            new NotificationReward(
                NotificationRewardType.Currency,
                "",
                1000,
                CurrencyTypes.PrismShards
            ),
            new NotificationReward(
                NotificationRewardType.Currency,
                "",
                50,
                CurrencyTypes.Cores
            )
        };

        NotificationManager.Instance.ShowModalNotification(
            "Daily Bonus!",
            "You've received your daily login bonus!",
            rewards,
            NotificationSource.System,
            "daily_bonus"
        );
    }

    /// <summary>
    /// Example: Achievement unlock notification
    /// Use for: Achievement completions, milestones
    /// </summary>
    private void ShowAchievementExample()
    {
        if (NotificationManager.Instance == null) return;

        var rewards = new NotificationReward[]
        {
            new NotificationReward(
                NotificationRewardType.Currency,
                "",
                500,
                CurrencyTypes.PrismShards
            ),
            new NotificationReward(
                NotificationRewardType.UnlockChip,
                "speed_chip_tier_1"
            )
        };

        NotificationManager.Instance.ShowModalNotification(
            "Achievement Unlocked!",
            "Speed Demon - Complete 100 waves without losing a life!",
            rewards,
            NotificationSource.Achievement,
            "speed_demon_achievement"
        );
    }

    /// <summary>
    /// Example: Objective completion notification
    /// Use for: Daily/weekly objective completions
    /// </summary>
    private void ShowObjectiveExample()
    {
        if (NotificationManager.Instance == null) return;

        NotificationManager.Instance.ShowQuickNotification(
            "Objective Complete!",
            "Kill 100 Enemies - Claimed 250 Prism Shards!",
            NotificationSource.Objective,
            4f
        );
    }

    /// <summary>
    /// Example: Multiple unlocks at once
    /// Use for: Pack openings, achievement batch claims
    /// </summary>
    public void ShowMultipleUnlocksExample()
    {
        if (NotificationManager.Instance == null) return;

        var rewards = new NotificationReward[]
        {
            new NotificationReward(NotificationRewardType.UnlockChip, "damage_chip"),
            new NotificationReward(NotificationRewardType.UnlockChip, "speed_chip"),
            new NotificationReward(NotificationRewardType.UnlockTurret, "laser_turret"),
            new NotificationReward(NotificationRewardType.Currency, "", 1000, CurrencyTypes.PrismShards)
        };

        NotificationManager.Instance.ShowModalNotification(
            "Starter Pack Opened!",
            "You received multiple items!",
            rewards,
            NotificationSource.Store,
            "starter_pack_001"
        );
    }

    /// <summary>
    /// Example: Research completion (future feature)
    /// Use for: Timed research completions
    /// </summary>
    public void ShowResearchCompleteExample()
    {
        if (NotificationManager.Instance == null) return;

        var rewards = new NotificationReward[]
        {
            new NotificationReward(NotificationRewardType.UnlockProjectile, "plasma_round")
        };

        NotificationManager.Instance.ShowModalNotification(
            "Research Complete!",
            "Advanced Plasma Technology research has finished!",
            rewards,
            NotificationSource.Research,
            "plasma_research_tier_1"
        );
    }

    /// <summary>
    /// Example: Check pending notifications for a source
    /// Use for: Determining if badges should be shown
    /// </summary>
    public void CheckPendingNotifications()
    {
        if (NotificationManager.Instance == null) return;

        int achievementCount = NotificationManager.Instance.GetPendingCount(NotificationSource.Achievement);
        int objectiveCount = NotificationManager.Instance.GetPendingCount(NotificationSource.Objective);
        int totalCount = NotificationManager.Instance.GetTotalPendingCount();

        Debug.Log($"Pending Achievements: {achievementCount}");
        Debug.Log($"Pending Objectives: {objectiveCount}");
        Debug.Log($"Total Pending: {totalCount}");
    }

    /// <summary>
    /// Example: Integrating with custom game events
    /// </summary>
    private void OnPlayerLevelUp(int newLevel)
    {
        if (NotificationManager.Instance == null) return;

        // Determine rewards based on level
        var rewards = new NotificationReward[]
        {
            new NotificationReward(
                NotificationRewardType.Currency,
                "",
                newLevel * 100, // Scale reward with level
                CurrencyTypes.PrismShards
            )
        };

        NotificationManager.Instance.ShowModalNotification(
            "Level Up!",
            $"You reached level {newLevel}!",
            rewards,
            NotificationSource.System,
            $"level_up_{newLevel}"
        );
    }

    /// <summary>
    /// Example: Batch notifications for multiple events
    /// </summary>
    public void ProcessMultipleEvents()
    {
        if (NotificationManager.Instance == null) return;

        // Quick notifications will queue and show in order
        NotificationManager.Instance.ShowQuickNotification(
            "Wave 1 Complete", "Great start!", NotificationSource.System, 2f);

        NotificationManager.Instance.ShowQuickNotification(
            "Wave 2 Complete", "Keep it up!", NotificationSource.System, 2f);

        NotificationManager.Instance.ShowQuickNotification(
            "Wave 3 Complete", "You're on fire!", NotificationSource.System, 2f);

        // The system will queue these and show them one at a time
    }
}
