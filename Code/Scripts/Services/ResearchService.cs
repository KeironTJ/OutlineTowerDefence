using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

/// <summary>
/// Service managing the research system
/// Follows patterns from ChipService and SkillService
/// </summary>
public class ResearchService : SingletonMonoBehaviour<ResearchService>, IStatContributor
{
    [SerializeField] private ResearchDefinition[] loadedDefinitions;
    
    private readonly Dictionary<string, ResearchDefinition> definitions = new Dictionary<string, ResearchDefinition>();
    
    // Events
    public event Action<string> ResearchStarted;
    public event Action<string, int> ResearchCompleted;
    public event Action<string> ResearchSpedUp;
    public event Action ResearchSlotsChanged;
    
    private PlayerManager playerManager;
    
    private bool EnsurePlayerManager()
    {
        if (playerManager == null)
            playerManager = PlayerManager.main;
        return playerManager != null;
    }
    
    private ResearchSystemConfig GetConfigInternal()
    {
        if (!EnsurePlayerManager())
            return null;

        var config = playerManager.GetResearchConfig();
        config?.EnsureValid();
        return config;
    }
    
    private List<ResearchProgressData> GetProgressInternal()
    {
        return EnsurePlayerManager() ? playerManager.GetResearchProgress() : null;
    }

    private void NormalizeActiveSlots(ResearchSystemConfig config)
    {
        if (config == null)
            return;

        var progressList = GetProgressInternal();
        if (progressList == null)
            return;

        int unlocked = config.GetUnlockedSlotCount();
        if (unlocked <= 0)
            return;

        var slotUsed = new bool[ResearchSystemConfig.MaxSlots];
        bool changed = false;

        foreach (var progress in progressList)
        {
            if (progress == null || !progress.isResearching)
                continue;

            int assigned = progress.slotIndex;
            if (assigned >= 0 && assigned < unlocked && !slotUsed[assigned])
            {
                slotUsed[assigned] = true;
                continue;
            }

            int replacement = -1;
            for (int i = 0; i < unlocked; i++)
            {
                if (!slotUsed[i])
                {
                    replacement = i;
                    break;
                }
            }

            if (replacement >= 0)
            {
                progress.slotIndex = replacement;
                slotUsed[replacement] = true;
                changed = true;
            }
        }

        if (changed && playerManager != null)
        {
            playerManager.SavePlayerData();
        }
    }

    private int FindAvailableSlotIndex(ResearchSystemConfig config, List<ResearchProgressData> progressList = null)
    {
        if (config == null)
            return -1;

        int unlocked = config.GetUnlockedSlotCount();
        if (unlocked <= 0)
            return -1;

        progressList ??= GetProgressInternal();
        var slotUsed = new bool[ResearchSystemConfig.MaxSlots];

        if (progressList != null)
        {
            foreach (var progress in progressList)
            {
                if (progress == null || !progress.isResearching)
                    continue;

                int assigned = progress.slotIndex;
                if (assigned >= 0 && assigned < unlocked)
                {
                    slotUsed[assigned] = true;
                }
            }
        }

        for (int i = 0; i < unlocked; i++)
        {
            if (!slotUsed[i])
                return i;
        }

        return -1;
    }
    
    protected override void OnAwakeAfterInit()
    {
        IndexDefinitions();
    }
    
    private void Start()
    {
        playerManager = PlayerManager.main;
        
        // Hook up events
        ResearchStarted += OnResearchStartedInternal;
        ResearchCompleted += OnResearchCompletedInternal;
        ResearchSpedUp += OnResearchSpedUpInternal;
    }
    
    protected override void OnDestroy()
    {
        ResearchStarted -= OnResearchStartedInternal;
        ResearchCompleted -= OnResearchCompletedInternal;
        ResearchSpedUp -= OnResearchSpedUpInternal;
        
        base.OnDestroy();
    }
    
    private void OnResearchStartedInternal(string researchId)
    {
        var def = GetDefinition(researchId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ResearchStarted, 
                new ResearchStartedEvent(researchId, def.displayName));
        }
    }
    
    private void OnResearchCompletedInternal(string researchId, int level)
    {
        var def = GetDefinition(researchId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ResearchCompleted, 
                new ResearchCompletedEvent(researchId, def.displayName, level));
            
            // Show notification
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowQuickNotification(
                    "Research Complete!",
                    $"{def.displayName} Level {level} completed",
                    NotificationSource.Research,
                    4f
                );
            }
        }
    }
    
    private void OnResearchSpedUpInternal(string researchId)
    {
        var def = GetDefinition(researchId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ResearchSpedUp, researchId);
        }
    }
    
    private void IndexDefinitions()
    {
        definitions.Clear();
        if (loadedDefinitions == null) return;
        
        foreach (var def in loadedDefinitions)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            if (!definitions.ContainsKey(def.id))
                definitions.Add(def.id, def);
            else
                UnityEngine.Debug.LogWarning($"[ResearchService] Duplicate research id: {def.id}");
        }
        
        UnityEngine.Debug.Log($"[ResearchService] Indexed {definitions.Count} research definitions");
    }
    
    // Definition Access
    public ResearchDefinition GetDefinition(string researchId)
    {
        return definitions.TryGetValue(researchId, out var def) ? def : null;
    }
    
    public IEnumerable<ResearchDefinition> GetAllDefinitions()
    {
        return definitions.Values;
    }
    
    public IEnumerable<ResearchDefinition> GetByType(ResearchType type)
    {
        return definitions.Values.Where(d => d.researchType == type);
    }
    
    // Progress Access
    public ResearchProgressData GetOrCreateProgress(string researchId)
    {
        var progressList = GetProgressInternal();
        if (progressList == null) return null;
        
        var progress = progressList.Find(p => p.researchId == researchId);
        if (progress == null)
        {
            progress = new ResearchProgressData(researchId);
            progressList.Add(progress);
            if (EnsurePlayerManager())
                playerManager.SavePlayerData();
        }
        return progress;
    }
    
    public ResearchProgressData GetProgress(string researchId)
    {
        var progressList = GetProgressInternal();
        if (progressList == null) return null;
        return progressList.Find(p => p.researchId == researchId);
    }
    
    public int GetLevel(string researchId)
    {
        var progress = GetProgress(researchId);
        return progress?.currentLevel ?? 0;
    }
    
    // Research Availability
    public bool IsResearchAvailable(string researchId)
    {
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        var progress = GetProgress(researchId);
        if (progress != null && progress.currentLevel >= def.maxLevel)
            return false; // Already maxed
        
        // Check prerequisites
        if (def.prerequisiteResearchIds != null)
        {
            foreach (var prereqId in def.prerequisiteResearchIds)
            {
                if (string.IsNullOrEmpty(prereqId)) continue;
                var prereqDef = GetDefinition(prereqId);
                if (prereqDef == null) continue;
                
                var prereqProgress = GetProgress(prereqId);
                if (prereqProgress == null || prereqProgress.currentLevel < prereqDef.maxLevel)
                    return false; // Prerequisite not completed
            }
        }
        
        return true;
    }
    
    // Research State
    public bool IsResearching(string researchId)
    {
        var progress = GetProgress(researchId);
        return progress != null && progress.isResearching;
    }
    
    public int GetActiveResearchCount()
    {
        var config = GetConfigInternal();
        NormalizeActiveSlots(config);
        var progressList = GetProgressInternal();
        if (progressList == null) return 0;
        return progressList.Count(p => p.isResearching);
    }
    
    public IEnumerable<ResearchProgressData> GetActiveResearch()
    {
        var config = GetConfigInternal();
        NormalizeActiveSlots(config);
        var progressList = GetProgressInternal();
        if (progressList == null) return Enumerable.Empty<ResearchProgressData>();
        return progressList.Where(p => p.isResearching);
    }

    public ResearchProgressData GetActiveProgressInSlot(int slotIndex)
    {
        if (slotIndex < 0) return null;
        var progressList = GetProgressInternal();
        if (progressList == null) return null;
        return progressList.FirstOrDefault(p => p != null && p.isResearching && p.slotIndex == slotIndex);
    }

    public IEnumerable<ResearchProgressData> GetPausedResearch()
    {
        var progressList = GetProgressInternal();
        if (progressList == null) return Enumerable.Empty<ResearchProgressData>();
        return progressList.Where(p => p != null && p.isPaused && p.pausedRemainingSeconds > 0f);
    }

    public int GetUnlockedSlotCount()
    {
        var config = GetConfigInternal();
        return config?.GetUnlockedSlotCount() ?? 1;
    }

    public float GetSlotUnlockCost(int slotIndex)
    {
        var config = GetConfigInternal();
        return config?.GetUnlockCostForSlot(slotIndex) ?? ResearchSystemConfig.GetDefaultUnlockCost(slotIndex);
    }

    public bool CanUnlockSlot(int slotIndex, out float cost)
    {
        cost = 0f;
        var config = GetConfigInternal();
        if (config == null || !EnsurePlayerManager())
            return false;

        if (slotIndex < 0)
            slotIndex = 0;
        if (slotIndex >= ResearchSystemConfig.MaxSlots)
            slotIndex = ResearchSystemConfig.MaxSlots - 1;

        int unlocked = config.GetUnlockedSlotCount();
        cost = config.GetUnlockCostForSlot(slotIndex);

        if (slotIndex < unlocked)
            return false;
        if (slotIndex > unlocked)
            return false;

    return playerManager.GetPrisms() >= cost;
    }

    public bool TryUnlockSlot(int slotIndex)
    {
        var config = GetConfigInternal();
        if (config == null || !EnsurePlayerManager())
            return false;

        if (slotIndex < 0)
            slotIndex = 0;
        if (slotIndex >= ResearchSystemConfig.MaxSlots)
            slotIndex = ResearchSystemConfig.MaxSlots - 1;

        int unlocked = config.GetUnlockedSlotCount();
        if (slotIndex < unlocked)
            return false;
        if (slotIndex > unlocked)
            return false;

        float cost = config.GetUnlockCostForSlot(slotIndex);
    if (cost > 0f && !playerManager.TrySpend(CurrencyType.Prisms, cost))
            return false;

        config.SetUnlockedSlotCount(unlocked + 1);
        playerManager.SavePlayerData();
        ResearchSlotsChanged?.Invoke();
        return true;
    }
    
    // Time Calculations
    public float GetRemainingTime(string researchId)
    {
        var progress = GetProgress(researchId);
        if (progress == null || !progress.isResearching) return 0f;
        
        if (string.IsNullOrEmpty(progress.startTimeIsoUtc))
            return progress.durationSeconds;
        
        try
        {
            var startTime = DateTime.Parse(progress.startTimeIsoUtc);
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            var remaining = progress.durationSeconds - elapsed;
            return Mathf.Max(0f, (float)remaining);
        }
        catch
        {
            return progress.durationSeconds;
        }
    }
    
    // Start Research
    private bool PauseResearchInternal(ResearchProgressData progress, bool refundCores)
    {
        if (progress == null)
            return false;

        var def = GetDefinition(progress.researchId);
        if (def == null)
            return false;

        float remaining = GetRemainingTime(progress.researchId);
        if (remaining < 0f)
            remaining = 0f;

        int nextLevel = progress.currentLevel + 1;
        float cost = def.GetCoreCostForLevel(nextLevel);

        progress.isResearching = false;
        progress.startTimeIsoUtc = "";
        progress.durationSeconds = 0f;
        progress.isPaused = true;
        progress.pausedRemainingSeconds = remaining;
        progress.slotIndex = -1;
        progress.pausedInvestedCores = refundCores ? cost : 0f;

        if (refundCores && EnsurePlayerManager())
        {
            playerManager.AddCurrency(cores: cost);
        }

        return true;
    }

    public bool CanStartResearch(string researchId)
    {
        if (!EnsurePlayerManager()) return false;
        if (!IsResearchAvailable(researchId)) return false;
        if (IsResearching(researchId)) return false;
        
        var config = GetConfigInternal();
        if (config == null) return false;

        var progressList = GetProgressInternal();
        if (progressList == null) return false;

        NormalizeActiveSlots(config);

        int unlockedSlots = config.GetUnlockedSlotCount();
        int activeCount = progressList.Count(p => p.isResearching);
        if (activeCount >= unlockedSlots) return false;

        if (FindAvailableSlotIndex(config, progressList) < 0) return false;
        
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        var progress = GetProgress(researchId);
        int nextLevel = (progress?.currentLevel ?? 0) + 1;
        
        if (nextLevel > def.maxLevel) return false;
        
        float cost = def.GetCoreCostForLevel(nextLevel);
        return playerManager.GetCores() >= cost;
    }
    
    public bool TryStartResearch(string researchId)
    {
        if (!CanStartResearch(researchId)) return false;
        
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
    var progress = GetOrCreateProgress(researchId);
    if (progress == null) return false;

    var config = GetConfigInternal();
    if (config == null) return false;

    var progressList = GetProgressInternal();
    if (progressList == null) return false;

    NormalizeActiveSlots(config);
    int slotIndex = FindAvailableSlotIndex(config, progressList);
    if (slotIndex < 0) return false;
        
        int nextLevel = progress.currentLevel + 1;
        float cost = def.GetCoreCostForLevel(nextLevel);
        
        if (!playerManager.TrySpend(CurrencyType.Cores, cost))
            return false;
        
        float duration = def.GetTimeForLevel(nextLevel);
        progress.isResearching = true;
        progress.startTimeIsoUtc = DateTime.UtcNow.ToString("o");
        progress.durationSeconds = duration;
        progress.slotIndex = slotIndex;
        progress.isPaused = false;
        progress.pausedRemainingSeconds = 0f;
        progress.pausedInvestedCores = 0f;
        
        playerManager.SavePlayerData();
        ResearchStarted?.Invoke(researchId);
        
        return true;
    }
    
    // Complete Research (called when time expires)
    public bool TryCompleteResearch(string researchId)
    {
        var progress = GetProgress(researchId);
        if (progress == null || !progress.isResearching) return false;
        
        var remaining = GetRemainingTime(researchId);
        if (remaining > 0.1f) return false; // Not yet complete
        
        progress.currentLevel++;
        progress.isResearching = false;
        progress.startTimeIsoUtc = "";
        progress.durationSeconds = 0f;
        progress.slotIndex = -1;
        
        playerManager.SavePlayerData();
        ResearchCompleted?.Invoke(researchId, progress.currentLevel);
        
        // Apply effects
        ApplyResearchEffects(researchId, progress.currentLevel);
        
        return true;
    }
    
    // Speed Up Research with Loops
    public bool CanSpeedUpResearch(string researchId, float secondsToSpeedup)
    {
        if (!EnsurePlayerManager()) return false;
        if (!IsResearching(researchId)) return false;
        
        var remaining = GetRemainingTime(researchId);
        if (remaining <= 0f) return false;
        
        secondsToSpeedup = Mathf.Min(secondsToSpeedup, remaining);
        
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        float loopsCost = def.GetLoopsCostForSpeedup(secondsToSpeedup);
        return playerManager.GetLoops() >= loopsCost;
    }
    
    public bool TrySpeedUpResearch(string researchId, float secondsToSpeedup)
    {
        if (!CanSpeedUpResearch(researchId, secondsToSpeedup)) return false;
        
        var progress = GetProgress(researchId);
        if (progress == null) return false;
        
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        var remaining = GetRemainingTime(researchId);
        secondsToSpeedup = Mathf.Min(secondsToSpeedup, remaining);
        
        float loopsCost = def.GetLoopsCostForSpeedup(secondsToSpeedup);
        
        if (!playerManager.TrySpend(CurrencyType.Loops, loopsCost))
            return false;
        
        // Reduce duration by moving start time forward
        try
        {
            var startTime = DateTime.Parse(progress.startTimeIsoUtc);
            var newStartTime = startTime.AddSeconds(secondsToSpeedup);
            progress.startTimeIsoUtc = newStartTime.ToString("o");
        }
        catch
        {
            progress.durationSeconds = Mathf.Max(0f, progress.durationSeconds - secondsToSpeedup);
        }
        
        playerManager.SavePlayerData();
        ResearchSpedUp?.Invoke(researchId);
        
        // Check if now complete
        if (GetRemainingTime(researchId) <= 0.1f)
        {
            TryCompleteResearch(researchId);
        }
        
        return true;
    }
    
    // Instant Complete with Prisms
    public bool CanInstantCompleteResearch(string researchId)
    {
        if (!EnsurePlayerManager()) return false;
        if (!IsResearching(researchId)) return false;
        
        var remaining = GetRemainingTime(researchId);
        if (remaining <= 0f) return false;
        
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        float prismsCost = def.GetPrismsCostForInstant(remaining);
        return playerManager.GetPrisms() >= prismsCost;
    }
    
    public bool TryInstantCompleteResearch(string researchId)
    {
        if (!CanInstantCompleteResearch(researchId)) return false;
        
        var remaining = GetRemainingTime(researchId);
        var def = GetDefinition(researchId);
        if (def == null) return false;
        
        float prismsCost = def.GetPrismsCostForInstant(remaining);
        
        if (!playerManager.TrySpend(CurrencyType.Prisms, prismsCost))
            return false;
        
        // Set research to complete immediately
        var progress = GetProgress(researchId);
        if (progress != null)
        {
            progress.startTimeIsoUtc = DateTime.UtcNow.AddSeconds(-progress.durationSeconds).ToString("o");
        }
        
        playerManager.SavePlayerData();
        
        return TryCompleteResearch(researchId);
    }
    
    // Cancel Research
    public bool TryCancelResearch(string researchId, bool refund = false)
    {
        var progress = GetProgress(researchId);
        if (progress == null || !progress.isResearching) return false;
        
        if (refund)
        {
            var def = GetDefinition(researchId);
            if (def != null)
            {
                int level = progress.currentLevel + 1;
                float cost = def.GetCoreCostForLevel(level);
                playerManager.AddCurrency(cores: cost);
            }
        }
        
        progress.isResearching = false;
        progress.startTimeIsoUtc = "";
        progress.durationSeconds = 0f;
        progress.slotIndex = -1;
        
        playerManager.SavePlayerData();
        return true;
    }
    
    // Apply Research Effects
    private void ApplyResearchEffects(string researchId, int level)
    {
        var def = GetDefinition(researchId);
        if (def == null) return;
        
        // Unlock items based on research type
        if (!string.IsNullOrEmpty(def.unlockTargetId))
        {
            switch (def.researchType)
            {
                case ResearchType.TowerBase:
                    playerManager.UnlockTowerBase(def.unlockTargetId);
                    break;
                    
                case ResearchType.Turret:
                    playerManager.UnlockTurret(def.unlockTargetId);
                    break;
                    
                case ResearchType.Projectile:
                    playerManager.UnlockProjectile(def.unlockTargetId);
                    break;
            }
        }
        
        // BaseStat type affects stat contributions (handled in Contribute method)
    }
    
    // Check for completed research on update
    private void Update()
    {
        if (!EnsurePlayerManager()) return;
        
        var activeResearch = GetActiveResearch().ToList();
        foreach (var research in activeResearch)
        {
            var remaining = GetRemainingTime(research.researchId);
            if (remaining <= 0.1f)
            {
                TryCompleteResearch(research.researchId);
            }
        }
    }
    
    // IStatContributor Implementation
    public void Contribute(StatCollector collector)
    {
        if (collector == null) return;
        
        var progressList = GetProgressInternal();
        if (progressList == null) return;
        
        foreach (var progress in progressList)
        {
            if (progress.currentLevel <= 0) continue;
            
            var def = GetDefinition(progress.researchId);
            if (def == null || def.researchType != ResearchType.BaseStat) continue;
            if (def.statBonuses == null || def.statBonuses.Length == 0) continue;
            
            // Apply stat bonuses scaled by level
            foreach (var bonus in def.statBonuses)
            {
                if (bonus == null || !bonus.IsValid) continue;
                
                float scaledValue = bonus.value * progress.currentLevel;
                bonus.ApplyTo(collector, scaledValue);

                UnityEngine.Debug.Log($"[ResearchService] Applied research bonus from '{def.id}' level {progress.currentLevel}: {bonus.targetStat}  += {scaledValue}");
            }
        }
    }
}
