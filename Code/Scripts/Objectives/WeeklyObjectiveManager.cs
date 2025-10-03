using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;

public class WeeklyObjectiveManager : MonoBehaviour
{
    public static WeeklyObjectiveManager main;

    [Header("Config")]
    [SerializeField] private int slotLengthDays = 7; // 7 or 14 days
    [SerializeField] private List<ObjectiveDefinition> allObjectives; // Populate in Inspector (weekly objectives)
    
    // Fixed weekly tiers for daily completion objectives
    private readonly int[] weeklyTiers = { 3, 6, 10, 15, 20, 30, 40, 55 };

    private readonly List<ObjectiveRuntime> activeWeekly = new List<ObjectiveRuntime>();

    private PlayerData PlayerData => SaveManager.main?.Current?.player;

    private bool initialized;
    private double nextSlotCheckTime;
    private int dailyCompletionsThisWeek = 0; // Track daily completions for tier objectives

    private void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);
    }

    // Ensure weekly definitions are available (Resources/Data/Objectives/Weekly)
    private void EnsureDefinitionsLoaded()
    {
        if (allObjectives != null && allObjectives.Count > 0) return;
        var loaded = Resources.LoadAll<ObjectiveDefinition>("Data/Objectives/Weekly");
        if (loaded != null && loaded.Length > 0)
            allObjectives = new List<ObjectiveDefinition>(loaded);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StartListening(EventNames.RoundEnded, OnRoundCompleted);
        EventManager.StartListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StartListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StartListening(EventNames.SkillUnlocked, OnSkillUnlocked);
        EventManager.StartListening(EventNames.WaveCompleted, OnWaveCompleted);
        
        // Subscribe to daily objective completions
        DailyObjectiveManager.OnDailyObjectiveCompleted += OnDailyObjectiveCompleted;

        // Load definitions before any init that depends on them
        EnsureDefinitionsLoaded();

        if (SaveManager.main != null)
        {
            SaveManager.main.OnAfterLoad += OnSaveLoaded;
            if (SaveManager.main.Current != null)
            {
                EnsureInitialized();
                EvaluateSlots();
            }
            else
            {
                StartCoroutine(WaitForSaveAndInitialize());
            }
        }
        else
        {
            StartCoroutine(WaitForSaveAndInitialize());
        }
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StopListening(EventNames.RoundEnded, OnRoundCompleted);
        EventManager.StopListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StopListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StopListening(EventNames.SkillUnlocked, OnSkillUnlocked);
        EventManager.StopListening(EventNames.WaveCompleted, OnWaveCompleted);
        
        // Unsubscribe from daily objective completions
        DailyObjectiveManager.OnDailyObjectiveCompleted -= OnDailyObjectiveCompleted;

        if (SaveManager.main != null)
            SaveManager.main.OnAfterLoad -= OnSaveLoaded;
    }

    private void OnSaveLoaded(PlayerSavePayload payload)
    {
        EnsureInitialized();
        EvaluateSlots();
    }

    private System.Collections.IEnumerator WaitForSaveAndInitialize()
    {
        while (SaveManager.main == null || SaveManager.main.Current == null || PlayerData == null)
            yield return new WaitForSeconds(0.1f);

        EnsureInitialized();
        EvaluateSlots();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (!initialized) EnsureInitialized();
            EvaluateSlots();
        }
    }

    private void EnsureInitIfNeeded()
    {
        if (!initialized) EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        if (PlayerData == null) return;
        EnsureDefinitionsLoaded();

        // Load daily completion counter from save data
        dailyCompletionsThisWeek = PlayerData.weeklyDailyCompletions;

        RebuildFromSave();
        EstablishBaselineIfNeeded();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.unscaledTime >= nextSlotCheckTime)
        {
            EvaluateSlots();
            nextSlotCheckTime = Time.unscaledTime + 60f; // Check every minute for weekly
        }
    }

    private void RebuildFromSave()
    {
        activeWeekly.Clear();
        if (PlayerData == null) return;
        if (allObjectives == null) return;

        var map = new Dictionary<string, ObjectiveDefinition>();
        foreach (var def in allObjectives)
        {
            if (def == null) continue;
            if (def.period == ObjectivePeriod.Weekly && !string.IsNullOrEmpty(def.id))
                map[def.id] = def;
        }

        foreach (var pd in PlayerData.weeklyObjectives)
            if (map.TryGetValue(pd.objectiveId, out var def))
                activeWeekly.Add(new ObjectiveRuntime { definition = def, progressData = pd });
    }

    private void EstablishBaselineIfNeeded()
    {
        if (string.IsNullOrEmpty(PlayerData.lastWeeklyObjectiveSlotKey))
        {
            var key = CurrentSlotKey();
            PlayerData.lastWeeklyObjectiveSlotKey = key;
            InitializeWeeklyTiers();
            SaveManager.main.QueueImmediateSave();
        }
    }

    private string CurrentSlotKey()
    {
        return WeeklySlotKey(DateTime.UtcNow);
    }

    private string WeeklySlotKey(DateTime utcTime)
    {
        // Calculate week start (Monday as week start)
        DateTime weekStart = GetWeekStart(utcTime, slotLengthDays);
        return weekStart.ToString("yyyyMMdd");
    }

    private DateTime GetWeekStart(DateTime utcTime, int lengthDays)
    {
        // Find Monday of the current week
        int daysFromMonday = ((int)utcTime.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        DateTime monday = utcTime.Date.AddDays(-daysFromMonday);
        
        // For multi-week cycles, find the cycle start
        if (lengthDays > 7)
        {
            DateTime epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Reference Monday
            int daysSinceEpoch = (int)(monday - epoch).TotalDays;
            int cyclesSinceEpoch = daysSinceEpoch / lengthDays;
            DateTime cycleStart = epoch.AddDays(cyclesSinceEpoch * lengthDays);
            return cycleStart;
        }
        
        return monday;
    }

    private void EvaluateSlots()
    {
        if (PlayerData == null) return;

        string stored = PlayerData.lastWeeklyObjectiveSlotKey;
        if (string.IsNullOrEmpty(stored))
        {
            PlayerData.lastWeeklyObjectiveSlotKey = CurrentSlotKey();
            SaveManager.main.QueueImmediateSave();
            return;
        }

        DateTime lastSlotStart;
        if (!DateTime.TryParseExact(stored, "yyyyMMdd", CultureInfo.InvariantCulture, 
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out lastSlotStart))
        {
            PlayerData.lastWeeklyObjectiveSlotKey = CurrentSlotKey();
            SaveManager.main.QueueImmediateSave();
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime currentSlotStart = GetWeekStart(now, slotLengthDays);

        if (lastSlotStart == currentSlotStart) return;

        bool anyChange = false;
        int rollovers = 0;

        DateTime iter = lastSlotStart;
        while (iter.AddDays(slotLengthDays) <= now)
        {
            iter = iter.AddDays(slotLengthDays);
            rollovers++;
            anyChange = true;
        }

        string newKey = currentSlotStart.ToString("yyyyMMdd");
        if (PlayerData.lastWeeklyObjectiveSlotKey != newKey)
        {
            PlayerData.lastWeeklyObjectiveSlotKey = newKey;
            anyChange = true;
        }

        if (anyChange)
        {
            SaveManager.main.QueueImmediateSave();
            CloudSyncService.main?.ScheduleUpload();
        }

        if (rollovers > 0)
        {
            OnSlotRollover?.Invoke(newKey);
            // Reset weekly objectives for new week
            ResetWeeklyObjectives();
        }
    }

    private void OnDailyObjectiveCompleted()
    {
        dailyCompletionsThisWeek++;
        PlayerData.weeklyDailyCompletions = dailyCompletionsThisWeek;
        Debug.Log($"Daily objective completed! Weekly count: {dailyCompletionsThisWeek}");
        
        // Update weekly tier progress
        UpdateWeeklyTierProgress();
        SaveManager.main.QueueSave();
    }
    
    private void UpdateWeeklyTierProgress()
    {
        foreach (var rt in activeWeekly)
        {
            var def = rt.definition;
            if (def.type == ObjectiveType.CompleteDailyObjectives) // Using existing type for daily completion tracking
            {
                int targetCount = (int)def.targetAmount;
                if (dailyCompletionsThisWeek >= targetCount && !rt.progressData.completed)
                {
                    rt.progressData.currentProgress = targetCount;
                    rt.progressData.completed = true;
                    
                    if (!def.manualClaim)
                    {
                        GrantReward(def);
                        rt.progressData.claimed = true;
                    }
                    
                    Debug.Log($"Weekly tier objective completed: {targetCount} daily objectives");
                    OnProgress?.Invoke(rt);
                }
                else if (dailyCompletionsThisWeek < targetCount)
                {
                    // Update progress
                    rt.progressData.currentProgress = Mathf.Min(dailyCompletionsThisWeek, targetCount);
                    OnProgress?.Invoke(rt);
                }
            }
        }
    }
    
    private void InitializeWeeklyTiers()
    {
        // Create weekly tier objectives for daily completion tracking
        foreach (int tierTarget in weeklyTiers)
        {
            var data = new ObjectiveProgressData
            {
                objectiveId = $"weekly_daily_tier_{tierTarget}",
                currentProgress = 0f,
                completed = false,
                claimed = false,
                assignedAtIsoUtc = DateTime.UtcNow.ToString("o")
            };
            
            PlayerData.weeklyObjectives.Add(data);
            
            // Create runtime objective (we'll need to define these in ScriptableObjects)
            // For now, create a basic definition
            var tierObjective = CreateTierObjectiveDefinition(tierTarget);
            if (tierObjective != null)
            {
                activeWeekly.Add(new ObjectiveRuntime { definition = tierObjective, progressData = data });
            }
        }
    }
    
    private ObjectiveDefinition CreateTierObjectiveDefinition(int tierTarget)
    {
        // This would ideally be a ScriptableObject, but for now create programmatically
        var definition = ScriptableObject.CreateInstance<ObjectiveDefinition>();
        definition.id = $"weekly_daily_tier_{tierTarget}";
        definition.period = ObjectivePeriod.Weekly;
        definition.type = ObjectiveType.CompleteDailyObjectives;
        definition.targetAmount = tierTarget;
        definition.description = $"Complete {tierTarget} daily objectives this week";
        definition.rewardType = CurrencyType.Cores;
        definition.rewardAmount = CalculateTierReward(tierTarget);
        definition.manualClaim = true; // Require manual claiming
        
        return definition;
    }
    
    private int CalculateTierReward(int tierTarget)
    {
        // Scale rewards based on tier difficulty
        return tierTarget switch
        {
            3 => 50,
            6 => 120,
            10 => 250,
            15 => 450,
            20 => 700,
            30 => 1200,
            40 => 1800,
            55 => 2800,
            _ => tierTarget * 10
        };
    }
    
    private void ResetWeeklyObjectives()
    {
        // Clear all weekly objectives
        PlayerData.weeklyObjectives.Clear();
        activeWeekly.Clear();
        
        // Reset daily completion counter
        dailyCompletionsThisWeek = 0;
        PlayerData.weeklyDailyCompletions = 0;
        
        // Initialize new weekly tiers
        InitializeWeeklyTiers();
        
        Debug.Log("Weekly objectives reset for new week");
    }

    public IReadOnlyList<ObjectiveRuntime> GetActiveWeeklyObjectives() => activeWeekly;

    public IReadOnlyList<ObjectiveRuntime> GetOrderedWeeklyObjectives()
    {
        // Return objectives ordered by completion status (incomplete first)
        return activeWeekly.OrderBy(obj => obj.progressData.completed ? 1 : 0).ToList();
    }

    public void Claim(ObjectiveRuntime rt)
    {
        if (rt == null || !rt.Completed || rt.Claimed) return;

        GrantReward(rt.definition);
        rt.progressData.claimed = true;

        SaveManager.main.QueueSave();
        SaveManager.main.QueueImmediateSave();
        CloudSyncService.main?.ScheduleUpload();
        OnProgress?.Invoke(rt);
    }

    private void Progress(ObjectiveRuntime rt, float amount)
    {
        if (rt.progressData.completed) return;
        rt.progressData.currentProgress += amount;
        if (rt.progressData.currentProgress >= rt.definition.targetAmount)
        {
            rt.progressData.currentProgress = rt.definition.targetAmount;
            rt.progressData.completed = true;
            if (!rt.definition.manualClaim)
            {
                GrantReward(rt.definition);
                rt.progressData.claimed = true;
            }
            SaveManager.main.QueueSave();
        }
        OnProgress?.Invoke(rt);
    }

    private void GrantReward(ObjectiveDefinition def)
    {
        switch (def.rewardType)
        {
            case CurrencyType.Cores:
                PlayerManager.main?.AddCurrency(cores: def.rewardAmount);
                break;
            case CurrencyType.Prisms:
                PlayerManager.main?.AddCurrency(prisms: def.rewardAmount);
                break;
            case CurrencyType.Loops:
                PlayerManager.main?.AddCurrency(loops: def.rewardAmount);
                break;
            default:
                PlayerManager.main?.AddCurrency(fragments: def.rewardAmount);
                break;
        }
        CloudSyncService.main?.ScheduleUpload();
    }

    // --- Event Handlers ---

    private void OnEnemyDestroyedDefinition(object data)
    {
        EnsureInitIfNeeded();
        if (data is not EnemyDestroyedDefinitionEvent e) return;

        foreach (var rt in activeWeekly)
        {
            var def = rt.definition;
            if (def.type != ObjectiveType.KillEnemies) continue;

            string reason;
            bool match = def.MatchesDetailed(e, out reason);

            if (match)
            {
                Progress(rt, 1f);
            }
        }
    }

    private void OnWaveCompleted(object data)
    {
        EnsureInitIfNeeded();
        foreach (var rt in activeWeekly)
        {
            var def = rt.definition;
            if (def.type == ObjectiveType.CompleteWaves)
                Progress(rt, 1f);
        }
    }

    private void OnRoundCompleted(object data)
    {
        EnsureInitIfNeeded();
        foreach (var rt in activeWeekly)
            if (rt.definition.type == ObjectiveType.CompleteRounds)
                Progress(rt, 1f);
    }

    private void OnCurrencyEarned(object data)
    {
        EnsureInitIfNeeded();
        if (data is not CurrencyEarnedEvent ce) return;

        foreach (var rt in activeWeekly)
        {
            var def = rt.definition;
            if (def.type != ObjectiveType.EarnCurrency) continue;
            switch (def.currencyType)
            {
                case CurrencyType.Fragments: if (ce.fragments > 0) Progress(rt, ce.fragments); break;
                case CurrencyType.Cores:     if (ce.cores > 0)     Progress(rt, ce.cores); break;
                case CurrencyType.Prisms:    if (ce.prisms > 0)    Progress(rt, ce.prisms); break;
                case CurrencyType.Loops:     if (ce.loops > 0)     Progress(rt, ce.loops); break;
            }
        }
    }

    private void OnCurrencySpent(object data)
    {
        EnsureInitIfNeeded();
        if (data is SpendCurrencyEvent se)
        {
            if (se.fragments + se.cores + se.prisms + se.loops <= 0f) return;

            foreach (var rt in activeWeekly)
            {
                var def = rt.definition;
                if (def.type != ObjectiveType.SpendCurrency) continue;
                switch (def.currencyType)
                {
                    case CurrencyType.Fragments: if (se.fragments > 0f) Progress(rt, se.fragments); break;
                    case CurrencyType.Cores: if (se.cores > 0f) Progress(rt, se.cores); break;
                    case CurrencyType.Prisms: if (se.prisms > 0f) Progress(rt, se.prisms); break;
                    case CurrencyType.Loops: if (se.loops > 0f) Progress(rt, se.loops); break;
                }
            }
        }
    }

    private void OnSkillUnlocked(object data) 
    {
        EnsureInitIfNeeded();
        if (data is SkillUnlockedEvent su)
        {
            foreach (var rt in activeWeekly)
            {
                var def = rt.definition;
                if (def.type != ObjectiveType.UnlockSkill) continue;
                if (!string.IsNullOrEmpty(def.skillId) && !string.Equals(def.skillId, su.skillId, StringComparison.OrdinalIgnoreCase))
                    continue;
                Progress(rt, 1f);
            }
        }
    }

    public static event System.Action<ObjectiveRuntime> OnProgress;
    public static event Action<string> OnSlotRollover;

    // --- DEBUG / TESTING METHODS ---
    
    [Header("Debug (Testing Only)")]
    [SerializeField] private bool enableDebugControls = false;
    
    // Manual trigger for testing - can be called from Inspector button or code
    [ContextMenu("Force New Week (Debug)")]
    public void ForceNewWeek()
    {
        if (!Application.isPlaying) return;
        
        Debug.Log("[WeeklyObjectiveManager] Manually triggering new week...");
        
        // Clear current weekly objectives
        activeWeekly.Clear();
        if (PlayerData != null)
        {
            PlayerData.weeklyObjectives.Clear();
        }
        
        // Reset daily completion counter
        if (PlayerData != null)
        {
            PlayerData.weeklyDailyCompletions = 0;
        }
        
        // Set new week start time to now
        string newWeekKey = CurrentSlotKey();
        if (PlayerData != null)
        {
            PlayerData.lastWeeklyObjectiveSlotKey = newWeekKey;
        }
        
        // Create fresh weekly objectives
        InitializeWeeklyTiers();
        
        // Save changes
        SaveManager.main?.QueueImmediateSave();
        CloudSyncService.main?.ScheduleUpload();
        
        // Trigger event if it exists
        OnSlotRollover?.Invoke(newWeekKey);
        
        Debug.Log($"[WeeklyObjectiveManager] New week started! Week key: {newWeekKey}, Active objectives: {activeWeekly.Count}");
    }
    
    // Get current state for debugging
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        if (!Application.isPlaying) return;
        
        var sb = new StringBuilder();
        sb.AppendLine("=== Weekly Objectives Debug ===");
        sb.AppendLine($"Initialized: {initialized}");
        sb.AppendLine($"Current Week Key: {PlayerData?.lastWeeklyObjectiveSlotKey}");
        sb.AppendLine($"Daily Completions This Week: {PlayerData?.weeklyDailyCompletions ?? 0}");
        sb.AppendLine($"Active Weekly Objectives: {activeWeekly.Count}");
        
        for (int i = 0; i < activeWeekly.Count; i++)
        {
            var rt = activeWeekly[i];
            sb.AppendLine($"  Tier {i+1}: {rt.Current}/{rt.definition.targetAmount} ({(rt.Completed ? "COMPLETE" : "INCOMPLETE")})");
        }
        
        Debug.Log(sb.ToString());
    }
    
    // Alternative: force trigger via public method (can be called from other scripts)
    public void TriggerNewWeekForTesting()
    {
        if (!enableDebugControls && !Application.isEditor)
        {
            Debug.LogWarning("[WeeklyObjectiveManager] Debug controls disabled in build!");
            return;
        }
        
        ForceNewWeek();
    }

    // ---- PUBLIC SLOT TIME HELPERS ----

    public DateTime GetCurrentSlotStartUtc()
    {
        return GetWeekStart(DateTime.UtcNow, slotLengthDays);
    }

    public DateTime GetNextSlotStartUtc()
    {
        var currentStart = GetCurrentSlotStartUtc();
        return currentStart.AddDays(slotLengthDays);
    }

    public TimeSpan GetTimeUntilNextSlot()
    {
        var nextStart = GetNextSlotStartUtc();
        return nextStart - DateTime.UtcNow;
    }

    public double GetSecondsUntilNextSlot()
    {
        return Math.Max(0, GetTimeUntilNextSlot().TotalSeconds);
    }

    public string GetNextSlotCountdownString()
    {
        var remaining = GetTimeUntilNextSlot();
        if (remaining.TotalSeconds < 0) 
            remaining = TimeSpan.Zero;
        
        int days = (int)remaining.TotalDays;
        int hours = remaining.Hours;
        int minutes = remaining.Minutes;
        
        if (days > 0)
            return $"{days}d {hours:00}h {minutes:00}m";
        else
            return $"{hours:00}h {minutes:00}m";
    }

    public string GetCurrentSlotKey() => CurrentSlotKey();
    
    // Debug helper to check current state
    public void LogCurrentState()
    {
        Debug.Log($"[WeeklyObjectives] Current state:");
        Debug.Log($"  - Daily completions this week: {dailyCompletionsThisWeek}");
        Debug.Log($"  - Active weekly objectives: {activeWeekly.Count}");
        Debug.Log($"  - Current slot key: {CurrentSlotKey()}");
        
        foreach (var obj in activeWeekly)
        {
            Debug.Log($"  - {obj.definition.id}: {obj.progressData.currentProgress}/{obj.definition.targetAmount} (Completed: {obj.progressData.completed}, Claimed: {obj.progressData.claimed})");
        }
    }
}
