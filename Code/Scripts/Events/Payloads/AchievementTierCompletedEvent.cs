using UnityEngine;

[System.Serializable]
public struct AchievementTierCompletedEvent
{
    public string achievementId;
    public int tierIndex;
    public string tierName;

    public AchievementTierCompletedEvent(string achievementId, int tierIndex, string tierName)
    {
        this.achievementId = achievementId;
        this.tierIndex = tierIndex;
        this.tierName = tierName;
    }
}
