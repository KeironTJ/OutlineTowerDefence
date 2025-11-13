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
    private Coroutine refreshRoutine;
    
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
        
        QueueRefresh();
    }
    
    private void OnEnable()
    {
        EventManager.StartListening(EventNames.ResearchCompleted, OnResearchCompleted);
        QueueRefresh();
    }
    
    private void OnDisable()
    {
        EventManager.StopListening(EventNames.ResearchCompleted, OnResearchCompleted);

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }
    
    private void OnResearchCompleted(object data)
    {
        QueueRefresh();
    }
    
    /// <summary>
    /// Calculate and update the maximum unlocked speed based on completed research
    /// </summary>
    private void UpdateMaxUnlockedSpeed()
    {
        if (timeScaleManager == null || researchService == null || playerManager == null || !playerManager.IsInitialized)
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

    private void QueueRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine == null)
            refreshRoutine = StartCoroutine(RefreshWhenReady());
    }

    private System.Collections.IEnumerator RefreshWhenReady()
    {
        while (timeScaleManager == null || researchService == null || playerManager == null || !playerManager.IsInitialized)
        {
            if (timeScaleManager == null)
                timeScaleManager = TimeScaleManager.Instance;
            if (researchService == null)
                researchService = ResearchService.Instance;
            if (playerManager == null)
                playerManager = PlayerManager.main;

            yield return null;
        }

        UpdateMaxUnlockedSpeed();
        refreshRoutine = null;
    }
}
