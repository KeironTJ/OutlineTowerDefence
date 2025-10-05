using System;

public class AchievementRuntime
{
    public AchievementDefinition definition;
    public AchievementProgressData progressData;
    
    public float Current => progressData.currentProgress;
    public int HighestTierCompleted => progressData.highestTierCompleted;
    public int HighestTierClaimed
    {
        get
        {
            if (progressData.claimedTierIndices == null || progressData.claimedTierIndices.Count == 0)
                return -1;

            int max = -1;
            for (int i = 0; i < progressData.claimedTierIndices.Count; i++)
            {
                int idx = progressData.claimedTierIndices[i];
                if (idx > max) max = idx;
            }
            return max;
        }
    }
    public bool HasUnclaimedRewards => HighestTierCompleted > HighestTierClaimed;
    
    public DateTime LastUpdatedUtc =>
        DateTime.TryParse(progressData.lastUpdatedIsoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt : DateTime.MinValue;

    public bool IsComplete => HighestTierCompleted >= (definition.tiers?.Length ?? 0) - 1;
    
    public AchievementTier GetCurrentTier()
    {
        if (definition.tiers == null || definition.tiers.Length == 0) return null;
        
        // Find the highest uncompleted tier
        for (int i = HighestTierCompleted + 1; i < definition.tiers.Length; i++)
        {
            if (Current < definition.tiers[i].targetAmount)
                return definition.tiers[i];
        }
        
        // All tiers complete or at final tier
        return definition.tiers[definition.tiers.Length - 1];
    }

    public AchievementTier GetNextUncompletedTier()
    {
        if (definition.tiers == null || definition.tiers.Length == 0) return null;
        
        int nextIndex = HighestTierCompleted + 1;
        if (nextIndex >= definition.tiers.Length) return null;
        
        return definition.tiers[nextIndex];
    }

    public AchievementTier GetNextUnclaimedTier()
    {
        if (definition.tiers == null || definition.tiers.Length == 0) return null;

        int highestClaimed = HighestTierClaimed;
        for (int i = highestClaimed + 1; i < definition.tiers.Length && i <= HighestTierCompleted; i++)
        {
            if (!IsTierClaimed(i))
                return definition.tiers[i];
        }

        return null;
    }

    public int GetNextClaimableTierIndex()
    {
        if (definition.tiers == null || definition.tiers.Length == 0) return -1;

        int highestClaimed = HighestTierClaimed;
        for (int i = highestClaimed + 1; i <= HighestTierCompleted && i < definition.tiers.Length; i++)
        {
            if (!IsTierClaimed(i))
                return i;
        }

        return -1;
    }

    public bool IsTierClaimed(int tierIndex)
    {
        if (tierIndex < 0) return false;
        if (progressData.claimedTierIndices == null) return false;
        return progressData.claimedTierIndices.Contains(tierIndex);
    }

    public int GetUnclaimedTierCount()
    {
        if (definition.tiers == null || definition.tiers.Length == 0) return 0;
        int count = 0;
        for (int i = 0; i <= HighestTierCompleted && i < definition.tiers.Length; i++)
        {
            if (!IsTierClaimed(i)) count++;
        }
        return count;
    }

    public float GetProgressToNextTier()
    {
        var nextTier = GetNextUncompletedTier();
        if (nextTier == null) return 1f;
        
        return Current / nextTier.targetAmount;
    }
}
