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
    [SerializeField] private int maxWeeklyObjectives = 3;
    [SerializeField] private int slotLengthDays = 7; // 7 or 14 days
    [SerializeField] private int objectivesAddedPerCycle = 1;
    [SerializeField] private List<ObjectiveDefinition> allObjectives; // Populate in Inspector (weekly objectives)

    private readonly List<ObjectiveRuntime> activeWeekly = new List<ObjectiveRuntime>();

    private PlayerData PlayerData => SaveManager.main?.Current?.player;

    private bool initialized;

    [SerializeField] private bool removeClaimedOnNextCycle = true;
    [SerializeField] private bool grantInitialFill = false;

    private double nextSlotCheckTime;

    private void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StartListening(EventNames.RoundEnded, OnRoundCompleted);
        EventManager.StartListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StartListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StartListening(EventNames.SkillUnlocked, OnSkillUnlocked);
        EventManager.StartListening(EventNames.WaveCompleted, OnWaveCompleted);

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

        var map = new Dictionary<string, ObjectiveDefinition>();
        foreach (var def in allObjectives)
            if (def.period == ObjectivePeriod.Weekly && !string.IsNullOrEmpty(def.id))
                map[def.id] = def;

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
            if (grantInitialFill)
            {
                AddObjectivesForSlot(DateTime.UtcNow);
            }
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

            if (removeClaimedOnNextCycle)
            {
                int before = activeWeekly.Count;
                PruneClaimedCompleted();
                if (activeWeekly.Count != before) anyChange = true;
            }

            int beforeAdd = activeWeekly.Count;
            AddObjectivesForSlot(iter);
            if (activeWeekly.Count != beforeAdd) anyChange = true;
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
        }
    }

    private void AddObjectivesForSlot(DateTime slotStartUtc)
    {
        if (activeWeekly.Count >= maxWeeklyObjectives) return;

        int free = maxWeeklyObjectives - activeWeekly.Count;
        int toAdd = Mathf.Clamp(objectivesAddedPerCycle, 0, free);
        if (toAdd <= 0) return;

        var activeIds = new HashSet<string>(activeWeekly.Select(a => a.definition.id));
        var candidates = allObjectives.Where(o =>
            o.period == ObjectivePeriod.Weekly &&
            !string.IsNullOrEmpty(o.id) &&
            !activeIds.Contains(o.id)).ToList();

        if (candidates.Count == 0) return;

        System.Random rng = new System.Random();
        for (int i = 0; i < toAdd && candidates.Count > 0; i++)
        {
            int pick = rng.Next(candidates.Count);
            var chosen = candidates[pick];
            candidates.RemoveAt(pick);

            var data = new ObjectiveProgressData
            {
                objectiveId = chosen.id,
                currentProgress = 0f,
                completed = false,
                claimed = false,
                assignedAtIsoUtc = slotStartUtc.ToString("o")
            };
            PlayerData.weeklyObjectives.Add(data);
            activeWeekly.Add(new ObjectiveRuntime { definition = chosen, progressData = data });
        }
    }

    private void PruneClaimedCompleted()
    {
        bool removed = false;

        for (int i = activeWeekly.Count - 1; i >= 0; i--)
        {
            var rt = activeWeekly[i];
            if (rt.progressData.completed && rt.progressData.claimed)
            {
                activeWeekly.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            for (int i = PlayerData.weeklyObjectives.Count - 1; i >= 0; i--)
            {
                var pd = PlayerData.weeklyObjectives[i];
                bool stillActive = activeWeekly.Any(r => r.progressData == pd);
                if (!stillActive && pd.completed && pd.claimed)
                    PlayerData.weeklyObjectives.RemoveAt(i);
            }
        }
    }

    public IReadOnlyList<ObjectiveRuntime> GetActiveWeeklyObjectives() => activeWeekly;

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
}
