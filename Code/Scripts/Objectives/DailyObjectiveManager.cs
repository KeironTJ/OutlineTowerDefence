using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;

public class DailyObjectiveManager : MonoBehaviour
{
    public static DailyObjectiveManager main;

    [Header("Config")]
    [SerializeField] private int maxDailyObjectives = 4;
    [SerializeField] private int slotLengthHours = 6;
    [SerializeField] private int objectivesAddedPerCycle = 1;
    [SerializeField] private List<ObjectiveDefinition> allObjectives; // Populate in Inspector (both daily & weekly).

    private readonly List<ObjectiveRuntime> activeDaily = new List<ObjectiveRuntime>();

    private PlayerData PlayerData => SaveManager.main?.Current?.player;

    private bool initialized;

    [SerializeField] private bool removeClaimedOnNextCycle = true; // (kept) prune at slot rollover
    [SerializeField] private bool grantInitialFill = false; // OPTIONAL: set true if you ever decide to give objectives immediately on first load.

    private double nextSlotCheckTime; // throttle Update slot checks

    private void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);   // ensure persists across scenes
    }

    private void EnsureDefinitionsLoaded()
    {
        if (allObjectives != null && allObjectives.Count > 0) return;
        var loaded = Resources.LoadAll<ObjectiveDefinition>("Data/Objectives/Daily");
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

        // Load definitions before any init that depends on them
        EnsureDefinitionsLoaded();

        // Subscribe to SaveManager.OnAfterLoad if available; otherwise fallback to polling
        if (SaveManager.main != null)
        {
            SaveManager.main.OnAfterLoad += OnSaveLoaded; // typed subscription
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

    // Called when SaveManager finishes loading or when ReplaceCurrent is invoked
    private void OnSaveLoaded(PlayerSavePayload payload)
    {
        EnsureInitialized();
        EvaluateSlots();
    }

    private System.Collections.IEnumerator WaitForSaveAndInitialize()
    {
        // short polling until SaveManager.current/player data exists
        while (SaveManager.main == null || SaveManager.main.Current == null || PlayerData == null)
            yield return new WaitForSeconds(0.1f);

        EnsureInitialized();
        EvaluateSlots();
    }

    // When returning from background, re-evaluate missed slot rollovers
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (!initialized) EnsureInitialized();
            EvaluateSlots();
        }
    }

    // Safety: ensure initialization before any event-driven progress is applied
    // Call this at the start of each public/event handler.
    private void EnsureInitIfNeeded()
    {
        if (!initialized) EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        if (PlayerData == null) return; // Bootstrap waits; safety guard.
        EnsureDefinitionsLoaded();

        RebuildFromSave();          // Does NOT add or prune.
        EstablishBaselineIfNeeded(); // Sets baseline only if missing.
        initialized = true;

        //Debug.Log($"[DailyObjectiveManager] Init complete. Slot={PlayerData.lastDailyObjectiveSlotKey} Active={activeDaily.Count}");
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.unscaledTime >= nextSlotCheckTime)
        {
            EvaluateSlots(); // CHANGED (was EvaluateSlot)
            nextSlotCheckTime = Time.unscaledTime + 10f;
        }
    }

    // --- SLOT LOGIC ---

    private void RebuildFromSave()
    {
        activeDaily.Clear();
        if (PlayerData == null) return;
        if (allObjectives == null) return;

        // Build lookup of definitions
        var map = new Dictionary<string, ObjectiveDefinition>();
        foreach (var def in allObjectives)
        {
            if (def == null) continue;
            if (def.period == ObjectivePeriod.Daily && !string.IsNullOrEmpty(def.id))
                map[def.id] = def;
        }

        // Rebuild runtime list from saved progress
        foreach (var pd in PlayerData.dailyObjectives)
            if (map.TryGetValue(pd.objectiveId, out var def))
                activeDaily.Add(new ObjectiveRuntime { definition = def, progressData = pd });
    }

    private void EstablishBaselineIfNeeded()
    {
        if (string.IsNullOrEmpty(PlayerData.lastDailyObjectiveSlotKey))
        {
            var key = CurrentSlotKey();
            PlayerData.lastDailyObjectiveSlotKey = key;
            if (grantInitialFill)
            {
                AddObjectivesForSlot(DateTime.UtcNow);
                //Debug.Log("[DailyObjectiveManager] First baseline + granted initial objectives.");
            }
            else
            {
                //Debug.Log("[DailyObjectiveManager] First baseline set (no initial objectives).");
            }
            SaveManager.main.QueueImmediateSave();
        }
    }

    private string CurrentSlotKey()
    {
        return SlotTimeUtility.CurrentSlotKey(slotLengthHours);
    }

    // REPLACE old EvaluateSlot with multi-slot catch-up
    private void EvaluateSlots()
    {
        if (PlayerData == null) return;

        // Parse stored key -> last slot start
        string stored = PlayerData.lastDailyObjectiveSlotKey;
        if (string.IsNullOrEmpty(stored))
        {
            PlayerData.lastDailyObjectiveSlotKey = CurrentSlotKey();
            SaveManager.main.QueueImmediateSave();
            //Debug.Log("[DailyObjectiveManager] Missing slot key repaired.");
            return;
        }

        if (!SlotTimeUtility.TryParseSlotKey(stored, slotLengthHours, out DateTime lastSlotStartUtc))
        {
            // Fallback: reset baseline to current
            PlayerData.lastDailyObjectiveSlotKey = CurrentSlotKey();
            SaveManager.main.QueueImmediateSave();
            //Debug.LogWarning("[DailyObjectiveManager] Failed to parse slot key. Reset baseline.");
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime currentSlotStart = SlotTimeUtility.GetSlotStartForTime(now, slotLengthHours);

        // If still inside the stored slot: nothing to do
        if (lastSlotStartUtc == currentSlotStart) return;

        bool anyChange = false;
        int rollovers = 0;

        // Process each missed slot boundary
        DateTime iter = lastSlotStartUtc;
        while (iter + TimeSpan.FromHours(slotLengthHours) <= now)
        {
            iter = iter.AddHours(slotLengthHours);
            rollovers++;

            if (removeClaimedOnNextCycle)
            {
                int before = activeDaily.Count;
                PruneClaimedCompleted();
                if (activeDaily.Count != before) anyChange = true;
            }

            int beforeAdd = activeDaily.Count;
            AddObjectivesForSlot(iter); // add per slot
            if (activeDaily.Count != beforeAdd) anyChange = true;

            // If already full, we still continue loop to advance slot pointer (so key reflects latest slot)
        }

        // Update stored key to current slot
        string newKey = SlotTimeUtility.SlotKeyFromStart(currentSlotStart);
        if (PlayerData.lastDailyObjectiveSlotKey != newKey)
        {
            PlayerData.lastDailyObjectiveSlotKey = newKey;
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
            //Debug.Log($"[DailyObjectiveManager] Processed {rollovers} slot rollover(s). Active={activeDaily.Count}");
        }
    }

    private void AddObjectivesForSlot(DateTime slotStartUtc)
    {
        if (activeDaily.Count >= maxDailyObjectives) return;

        int free = maxDailyObjectives - activeDaily.Count;
        int toAdd = Mathf.Clamp(objectivesAddedPerCycle, 0, free);
        if (toAdd <= 0) return;

        var activeIds = new HashSet<string>(activeDaily.Select(a => a.definition.id));
        var candidates = allObjectives.Where(o =>
            o.period == ObjectivePeriod.Daily &&
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
                assignedAtIsoUtc = slotStartUtc.ToString("o") // stamp with slot time
            };
            PlayerData.dailyObjectives.Add(data);
            activeDaily.Add(new ObjectiveRuntime { definition = chosen, progressData = data });
            //Debug.Log($"[DailyObjectiveManager] Added objective {chosen.id} (slot {SlotTimeUtility.SlotKeyFromStart(slotStartUtc)})");
        }
    }

    private void PruneClaimedCompleted()
    {
        bool removed = false;

        for (int i = activeDaily.Count - 1; i >= 0; i--)
        {
            var rt = activeDaily[i];
            if (rt.progressData.completed && rt.progressData.claimed)
            {
                activeDaily.RemoveAt(i);
                removed = true;
                //Debug.Log($"[DailyObjectiveManager] Pruned claimed {rt.definition.id}");
            }
        }

        if (removed)
        {
            // Clean PlayerData list
            for (int i = PlayerData.dailyObjectives.Count - 1; i >= 0; i--)
            {
                var pd = PlayerData.dailyObjectives[i];
                bool stillActive = activeDaily.Any(r => r.progressData == pd);
                if (!stillActive && pd.completed && pd.claimed)
                    PlayerData.dailyObjectives.RemoveAt(i);
            }
        }
    }

    // Public access for UI
    public IReadOnlyList<ObjectiveRuntime> GetActiveDailyObjectives() => activeDaily;

    public IReadOnlyList<ObjectiveRuntime> GetOrderedDailyObjectives()
    {
        // Return objectives ordered by completion status (incomplete first)
        return activeDaily.OrderBy(obj => obj.progressData.completed ? 1 : 0).ToList();
    }
    
    // Get objectives ordered by completion status (incomplete first, then completed)
    public IReadOnlyList<ObjectiveRuntime> GetActiveDailyObjectivesOrdered()
    {
        return activeDaily
            .OrderBy(rt => rt.Completed ? 1 : 0) // incomplete first (0), completed last (1)
            .ThenBy(rt => rt.definition.targetAmount > 0 ? rt.Current / rt.definition.targetAmount : 0) // within each group, sort by progress
            .ToList();
    }

    public void Claim(ObjectiveRuntime rt)
    {
        if (rt == null || !rt.Completed || rt.Claimed) return;

        GrantReward(rt.definition);
        rt.progressData.claimed = true;

        // If you want IMMEDIATE slot freeing instead of waiting for next cycle:
        // if (!removeClaimedOnNextCycle) { PruneImmediate(rt); }

        SaveManager.main.QueueSave();
        SaveManager.main.QueueImmediateSave();
        CloudSyncService.main?.ScheduleUpload();
        OnProgress?.Invoke(rt);
    }

    // Optional immediate prune helper (only used if you uncomment above)
    private void PruneImmediate(ObjectiveRuntime rt)
    {
        activeDaily.Remove(rt);
        PlayerData.dailyObjectives.Remove(rt.progressData);
        //Debug.Log($"[DailyObjectiveManager] Immediately removed claimed objective {rt.definition.id}");
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
            OnDailyObjectiveCompleted?.Invoke(); // Notify weekly objectives
        }
        //Debug.Log($"[DailyObjectiveManager] {rt.definition.id} progress {rt.progressData.currentProgress:0.##}/{rt.definition.targetAmount:0.##}");
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

        foreach (var rt in activeDaily)
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
        //Debug.Log($"Event Heard: Wave Completed");
        foreach (var rt in activeDaily)
        {
            var def = rt.definition;
            if (def.type == ObjectiveType.CompleteWaves)
                Progress(rt, 1f);
        }
    }

    private void OnRoundCompleted(object data)
    {
        EnsureInitIfNeeded();
        foreach (var rt in activeDaily)
            if (rt.definition.type == ObjectiveType.CompleteRounds)
                Progress(rt, 1f);
    }

    private void OnCurrencyEarned(object data)
    {
        EnsureInitIfNeeded();
        if (data is not CurrencyEarnedEvent ce) return;

        foreach (var rt in activeDaily)
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

            foreach (var rt in activeDaily)
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
            foreach (var rt in activeDaily)
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
    public static event Action<string> OnSlotRollover; // slotKey of new slot
    public static event System.Action OnDailyObjectiveCompleted; // fired when any daily objective completes

    // ---- PUBLIC SLOT TIME HELPERS ----

    // Start (UTC) of the current slot
    public DateTime GetCurrentSlotStartUtc()
    {
        return SlotTimeUtility.GetSlotStartForTime(DateTime.UtcNow, slotLengthHours);
    }

    // Start (UTC) of the next slot
    public DateTime GetNextSlotStartUtc()
    {
        var currentStart = GetCurrentSlotStartUtc();
        return SlotTimeUtility.GetNextSlotStart(currentStart, slotLengthHours);
    }

    // Time remaining until next slot
    public TimeSpan GetTimeUntilNextSlot()
    {
        return SlotTimeUtility.GetTimeUntilNextSlot(DateTime.UtcNow, slotLengthHours);
    }

    // Seconds remaining (clamped >= 0)
    public double GetSecondsUntilNextSlot()
    {
        return Math.Max(0, GetTimeUntilNextSlot().TotalSeconds);
    }

    // Formatted countdown "HH:MM:SS" (UTC based)
    public string GetNextSlotCountdownString()
    {
        return SlotTimeUtility.FormatTimeRemaining(GetTimeUntilNextSlot());
    }

    // Current slot key (public if UI wants it)
    public string GetCurrentSlotKey() => CurrentSlotKey();
}

