using System;
using System.Collections.Generic;

[Serializable]
public class AchievementProgressData
{
    public string achievementId;
    public float currentProgress;
    public int highestTierCompleted; // -1 = none, 0+ = tier index
    public string lastUpdatedIsoUtc;
    public List<int> claimedTierIndices;
    
    public AchievementProgressData()
    {
        highestTierCompleted = -1;
        currentProgress = 0f;
        lastUpdatedIsoUtc = DateTime.UtcNow.ToString("o");
        claimedTierIndices = new List<int>();
    }

    public AchievementProgressData(string achievementId)
    {
        this.achievementId = achievementId;
        highestTierCompleted = -1;
        currentProgress = 0f;
        lastUpdatedIsoUtc = DateTime.UtcNow.ToString("o");
        claimedTierIndices = new List<int>();
    }
}
