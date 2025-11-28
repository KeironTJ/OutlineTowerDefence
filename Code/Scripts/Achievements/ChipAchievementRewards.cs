using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example integration showing how to reward chips from achievement completions.
/// This is an optional add-on to demonstrate extensibility.
/// </summary>
public class ChipAchievementRewards : MonoBehaviour
{
    [System.Serializable]
    public class ChipReward
    {
        public string achievementId;
        public string chipId;
        public int chipCount = 1;
    }
    
    [Header("Chip Rewards Configuration")]
    [SerializeField] private List<ChipReward> chipRewards = new List<ChipReward>();
    
    [Header("Debug")]
    [SerializeField] private bool logRewards = true;
    
    private ChipService chipService;
    private AchievementManager achievementManager;
    private HashSet<string> rewardedAchievements = new HashSet<string>();
    
    private void Start()
    {
        chipService = ChipService.Instance;
        achievementManager = FindFirstObjectByType<AchievementManager>();
        
        // Subscribe to achievement events
        EventManager.StartListening(EventNames.AchievementTierCompleted, OnAchievementTierCompleted);
    }
    
    private void OnDestroy()
    {
        EventManager.StopListening(EventNames.AchievementTierCompleted, OnAchievementTierCompleted);
    }
    
    private void OnAchievementTierCompleted(object payload)
    {
        if (payload is not AchievementTierCompletedEvent evt) return;
        if (chipService == null) return;
        
        // Check if this achievement has chip rewards
        foreach (var reward in chipRewards)
        {
            if (reward.achievementId != evt.achievementId) continue;
            
            // Create unique key to prevent duplicate rewards
            string rewardKey = $"{evt.achievementId}_{evt.tierIndex}";
            if (rewardedAchievements.Contains(rewardKey))
            {
                if (logRewards)
                    Debug.Log($"[ChipAchievementRewards] Reward already given for {rewardKey}");
                continue;
            }
            
            // Grant the chip reward
            if (chipService.TryAddChip(reward.chipId, reward.chipCount))
            {
                rewardedAchievements.Add(rewardKey);
                
                var chipDef = chipService.GetDefinition(reward.chipId);
                string chipName = chipDef != null ? chipDef.chipName : reward.chipId;
                
                if (logRewards)
                {
                    Debug.Log($"[ChipAchievementRewards] Granted {reward.chipCount}x {chipName} " +
                             $"for completing {evt.achievementId} tier {evt.tierIndex}");
                }
                
                // Optional: Show notification to player
                // ShowChipRewardNotification(chipName, reward.chipCount);
            }
            else
            {
                Debug.LogWarning($"[ChipAchievementRewards] Failed to grant chip reward: {reward.chipId}");
            }
        }
    }
    
    /// <summary>
    /// Helper method to add a chip reward dynamically
    /// </summary>
    public void AddChipReward(string achievementId, string chipId, int count = 1)
    {
        chipRewards.Add(new ChipReward
        {
            achievementId = achievementId,
            chipId = chipId,
            chipCount = count
        });
    }
    
    /// <summary>
    /// Helper method to check if an achievement grants chip rewards
    /// </summary>
    public bool HasChipReward(string achievementId)
    {
        return chipRewards.Exists(r => r.achievementId == achievementId);
    }
    
    /// <summary>
    /// Get all chip rewards for an achievement
    /// </summary>
    public List<ChipReward> GetChipRewards(string achievementId)
    {
        return chipRewards.FindAll(r => r.achievementId == achievementId);
    }
}
