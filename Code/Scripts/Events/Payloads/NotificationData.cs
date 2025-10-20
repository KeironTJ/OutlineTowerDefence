using System;

[Serializable]
public class NotificationData
{
    public NotificationType type;
    public string title;
    public string description;
    public NotificationPriority priority;
    public float displayDuration; // For auto-dismiss notifications (seconds)
    public NotificationReward[] rewards; // Optional rewards to claim
    public string sourceId; // ID of the achievement, objective, chip, etc. that triggered this
    public NotificationSource source; // What triggered this notification
    
    public NotificationData(
        NotificationType type,
        string title,
        string description,
        NotificationPriority priority = NotificationPriority.Normal,
        float displayDuration = 3f,
        NotificationReward[] rewards = null,
        string sourceId = "",
        NotificationSource source = NotificationSource.System)
    {
        this.type = type;
        this.title = title;
        this.description = description;
        this.priority = priority;
        this.displayDuration = displayDuration;
        this.rewards = rewards;
        this.sourceId = sourceId;
        this.source = source;
    }
}

public enum NotificationType
{
    Quick,      // Auto-dismiss after duration
    Modal       // Requires user interaction
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum NotificationSource
{
    System,
    Achievement,
    Objective,
    Skill,
    Chip,
    Store,
    Research,
    Loadout
}

[Serializable]
public class NotificationReward
{
    public NotificationRewardType rewardType;
    public string rewardId;
    public int amount;
    public CurrencyTypes currencyType;
    
    public NotificationReward(NotificationRewardType type, string id = "", int amount = 0, CurrencyTypes currency = CurrencyTypes.PrismShards)
    {
        rewardType = type;
        rewardId = id;
        this.amount = amount;
        currencyType = currency;
    }
}

public enum NotificationRewardType
{
    Currency,
    UnlockTurret,
    UnlockProjectile,
    UnlockChip,
    UnlockSkill,
    UnlockTowerBase
}
