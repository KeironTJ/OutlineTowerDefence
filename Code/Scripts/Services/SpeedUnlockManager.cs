using UnityEngine;

/// <summary>
/// Handles unlocking speed multipliers through the research system.
/// Listens for research completion events and updates TimeScaleManager accordingly.
/// Uses a single research item with multiple levels, where each level unlocks a speed tier.
/// </summary>
public class SpeedUnlockManager : MonoBehaviour
{
    [Header("Speed Unlock Research")]
    [Tooltip("Research ID that unlocks speed levels. Each research level unlocks 0.25x speed increment.")]
    [SerializeField] private string speedResearchId = "RES_GAME_SPEED";
    
    private TimeScaleManager timeScaleManager;
    private ResearchService researchService;
    private PlayerManager playerManager;
    
    private void Start()
    {
        timeScaleManager = TimeScaleManager.Instance;
        researchService = ResearchService.Instance;
        playerManager = PlayerManager.main;
        
        if (timeScaleManager == null)
        {
            Debug.LogError("SpeedUnlockManager: TimeScaleManager not found!");
            enabled = false;
            return;
        }
        
        // Initialize max unlocked speed based on completed research
        UpdateMaxUnlockedSpeed();
    }
    
    private void OnEnable()
    {
        EventManager.StartListening(EventNames.ResearchCompleted, OnResearchCompleted);
    }
    
    private void OnDisable()
    {
        EventManager.StopListening(EventNames.ResearchCompleted, OnResearchCompleted);
    }
    
    private void OnResearchCompleted(object data)
    {
        // Update max speed when any research is completed
        // (in case it's a speed unlock)
        UpdateMaxUnlockedSpeed();
    }
    
    /// <summary>
    /// Calculate and update the maximum unlocked speed based on completed research
    /// </summary>
    private void UpdateMaxUnlockedSpeed()
    {
        if (timeScaleManager == null || researchService == null)
            return;
        
        // Base speed is always unlocked
        float maxSpeed = 1f;
        
        // Get the current level of the speed research
        int currentLevel = GetResearchLevel(speedResearchId);
        
        if (currentLevel > 0)
        {
            // Each level unlocks 0.25x speed increment
            // Level 1 = 1.25x, Level 2 = 1.5x, etc.
            maxSpeed = 1f + (currentLevel * TimeScaleManager.GetSpeedIncrement());
            
            // Cap at maximum speed (5x)
            maxSpeed = Mathf.Min(maxSpeed, 5f);
        }
        
        // Update the time scale manager
        timeScaleManager.SetMaxUnlockedSpeed(maxSpeed);
    }
    
    /// <summary>
    /// Get the current level of a research item
    /// </summary>
    private int GetResearchLevel(string researchId)
    {
        if (playerManager == null || string.IsNullOrEmpty(researchId))
            return 0;
        
        var progressList = playerManager.GetResearchProgress();
        if (progressList == null)
            return 0;
        
        foreach (var progress in progressList)
        {
            if (progress != null && progress.researchId == researchId)
            {
                return progress.currentLevel;
            }
        }
        
        return 0;
    }
}
