using UnityEngine;

/// <summary>
/// Handles unlocking speed multipliers through the research system.
/// Listens for research completion events and updates TimeScaleManager accordingly.
/// </summary>
public class SpeedUnlockManager : MonoBehaviour
{
    [Header("Speed Unlock Research IDs")]
    [Tooltip("Research IDs that unlock speed levels in order: 1.25x, 1.5x, 1.75x, 2x, etc.")]
    [SerializeField] private string[] speedUnlockResearchIds = new string[]
    {
        "RES_SPEED_125",  // 1.25x
        "RES_SPEED_150",  // 1.50x
        "RES_SPEED_175",  // 1.75x
        "RES_SPEED_200",  // 2.00x
        "RES_SPEED_225",  // 2.25x
        "RES_SPEED_250",  // 2.50x
        "RES_SPEED_275",  // 2.75x
        "RES_SPEED_300",  // 3.00x
        "RES_SPEED_325",  // 3.25x
        "RES_SPEED_350",  // 3.50x
        "RES_SPEED_375",  // 3.75x
        "RES_SPEED_400",  // 4.00x
        "RES_SPEED_425",  // 4.25x
        "RES_SPEED_450",  // 4.50x
        "RES_SPEED_475",  // 4.75x
        "RES_SPEED_500",  // 5.00x
    };
    
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
        
        // Check each speed unlock research
        for (int i = 0; i < speedUnlockResearchIds.Length; i++)
        {
            string researchId = speedUnlockResearchIds[i];
            
            if (string.IsNullOrEmpty(researchId))
                continue;
            
            // Check if this research is completed
            if (IsResearchCompleted(researchId))
            {
                // Calculate the speed for this tier
                // Base 1x + (i+1) * 0.25
                float speedForThisTier = 1f + (i + 1) * TimeScaleManager.GetSpeedIncrement();
                maxSpeed = Mathf.Max(maxSpeed, speedForThisTier);
            }
            else
            {
                // Since research should be sequential, stop at first uncompleted
                break;
            }
        }
        
        // Update the time scale manager
        timeScaleManager.SetMaxUnlockedSpeed(maxSpeed);
    }
    
    /// <summary>
    /// Check if a research item is completed
    /// </summary>
    private bool IsResearchCompleted(string researchId)
    {
        if (playerManager == null)
            return false;
        
        var progressList = playerManager.GetResearchProgress();
        if (progressList == null)
            return false;
        
        foreach (var progress in progressList)
        {
            if (progress != null && progress.researchId == researchId)
            {
                return progress.completedLevels > 0;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the research ID for a specific speed level
    /// </summary>
    public string GetResearchIdForSpeed(float speed)
    {
        if (speed <= 1f)
            return null;
        
        int index = Mathf.RoundToInt((speed - 1f) / TimeScaleManager.GetSpeedIncrement()) - 1;
        
        if (index >= 0 && index < speedUnlockResearchIds.Length)
        {
            return speedUnlockResearchIds[index];
        }
        
        return null;
    }
}
