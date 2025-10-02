using System;

public class AchievementRuntime
{
    public AchievementDefinition definition;
    public AchievementProgressData progressData;
    
    public float Current => progressData.currentProgress;
    public int HighestTierCompleted => progressData.highestTierCompleted;
    
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

    public float GetProgressToNextTier()
    {
        var nextTier = GetNextUncompletedTier();
        if (nextTier == null) return 1f;
        
        return Current / nextTier.targetAmount;
    }
}
