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

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.EnemyDestroyed, OnEnemyDestroyed);
        EventManager.StartListening(EventNames.RoundEnded, OnRoundCompleted);
        EventManager.StartListening(EventNames.CreditsEarned, OnCreditsEarned);
        EventManager.StartListening(EventNames.CreditsSpent, OnCreditsSpent);        
        EventManager.StartListening(EventNames.SkillUnlocked, OnSkillUnlocked); 
        EventManager.StartListening(EventNames.WaveCompleted, OnWaveCompleted);      
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.EnemyDestroyed, OnEnemyDestroyed);
        EventManager.StopListening(EventNames.RoundEnded, OnRoundCompleted);
        EventManager.StopListening(EventNames.CreditsEarned, OnCreditsEarned);
        EventManager.StopListening(EventNames.CreditsSpent, OnCreditsSpent);   
        EventManager.StopListening(EventNames.SkillUnlocked, OnSkillUnlocked);
        EventManager.StopListening(EventNames.WaveCompleted, OnWaveCompleted);
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        if (PlayerData == null) return; // Bootstrap waits; safety guard.

        RebuildFromSave();          // Does NOT add or prune.
        EstablishBaselineIfNeeded(); // Sets baseline only if missing.
        initialized = true;

        Debug.Log($"[DailyObjectiveManager] Init complete. Slot={PlayerData.lastDailyObjectiveSlotKey} Active={activeDaily.Count}");
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

        // Build lookup of definitions
        var map = new Dictionary<string, ObjectiveDefinition>();
        foreach (var def in allObjectives)
            if (def.period == ObjectivePeriod.Daily && !string.IsNullOrEmpty(def.id))
                map[def.id] = def;

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
                Debug.Log("[DailyObjectiveManager] First baseline + granted initial objectives.");
            }
            else
            {
                Debug.Log("[DailyObjectiveManager] First baseline set (no initial objectives).");
            }
            SaveManager.main.QueueImmediateSave();
        }
    }

    private string CurrentSlotKey()
    {
        DateTime now = DateTime.UtcNow;
        int slotIndex = now.Hour / slotLengthHours;
        int slotStartHour = slotIndex * slotLengthHours;
        return now.ToString("yyyyMMdd") + "-" + slotStartHour.ToString("00");
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
            Debug.Log("[DailyObjectiveManager] Missing slot key repaired.");
            return;
        }

        if (!TryParseSlotKey(stored, out DateTime lastSlotStartUtc))
        {
            // Fallback: reset baseline to current
            PlayerData.lastDailyObjectiveSlotKey = CurrentSlotKey();
            SaveManager.main.QueueImmediateSave();
            Debug.LogWarning("[DailyObjectiveManager] Failed to parse slot key. Reset baseline.");
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime currentSlotStart = GetSlotStartForTime(now);

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
        string newKey = SlotKeyFromStart(currentSlotStart);
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
            Debug.Log($"[DailyObjectiveManager] Processed {rollovers} slot rollover(s). Active={activeDaily.Count}");
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
            Debug.Log($"[DailyObjectiveManager] Added objective {chosen.id} (slot {SlotKeyFromStart(slotStartUtc)})");
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
                Debug.Log($"[DailyObjectiveManager] Pruned claimed {rt.definition.id}");
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
        Debug.Log($"[DailyObjectiveManager] Immediately removed claimed objective {rt.definition.id}");
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
        Debug.Log($"[DailyObjectiveManager] {rt.definition.id} progress {rt.progressData.currentProgress:0.##}/{rt.definition.targetAmount:0.##}");
        OnProgress?.Invoke(rt);
    }

    private void GrantReward(ObjectiveDefinition def)
    {
        switch (def.rewardType)
        {
            case CurrencyType.Premium:
                PlayerManager.main?.AddCredits(premium: def.rewardAmount);
                break;
            case CurrencyType.Special:
                PlayerManager.main?.AddCredits(special: def.rewardAmount);
                break;
            case CurrencyType.Luxury:
                PlayerManager.main?.AddCredits(luxury: def.rewardAmount);
                break;
            default:
                // If you have a basic currency reward enum add it here; otherwise ignore.
                PlayerManager.main?.AddCredits(basic: def.rewardAmount);
                break;
        }
        CloudSyncService.main?.ScheduleUpload();
    }

    // OPTIONAL helper (improves readability for enemy filtering):
    private bool EnemyMatches(ObjectiveDefinition def, EnemyDestroyedEvent e)
    {
        if (def.type != ObjectiveType.KillEnemies) return false;
        if (!def.anyEnemyType && e.type != def.enemyType) return false;
        if (!def.anyEnemySubtype && e.subtype != def.enemySubtype) return false;
        Debug.Log($"[DailyObjectiveManager] EnemyMatches: {def.enemyType}:{def.enemySubtype} vs {e.type}:{e.subtype}");
        return true;
    }

    // --- Event Handlers ---

    private void OnEnemyDestroyed(object data)
    {
        if (data is EnemyDestroyedEvent e)
        {
            foreach (var rt in activeDaily)
            {
                var def = rt.definition;
                if (EnemyMatches(def, e))
                    Progress(rt, 1f);
            }
        }
    }

    private void OnWaveCompleted(object data)
    {
        foreach (var rt in activeDaily)
        {
            var def = rt.definition;
            if (def.type == ObjectiveType.CompleteWaves)
                Progress(rt, 1f);
        }
    }

    private void OnRoundCompleted(object data)
    {
        foreach (var rt in activeDaily)
            if (rt.definition.type == ObjectiveType.CompleteRounds)
                Progress(rt, 1f);
    }

    private void OnCreditsEarned(object data)
    {
        if (data is CreditsEarnedEvent ce)
        {
            if (ce.basic + ce.premium + ce.luxury + ce.special <= 0f) return;
            Debug.Log($"[DailyObjectiveManager] OnCreditsEarned: B{ce.basic} P{ce.premium} L{ce.luxury} S{ce.special}");

            foreach (var rt in activeDaily)
            {
                var def = rt.definition;
                if (def.type != ObjectiveType.EarnCredits) continue;
                switch (def.creditCurrencyType)
                {
                    case CurrencyType.Basic: if (ce.basic > 0f) Progress(rt, ce.basic); break;
                    case CurrencyType.Premium: if (ce.premium > 0f) Progress(rt, ce.premium); break;
                    case CurrencyType.Luxury: if (ce.luxury > 0f) Progress(rt, ce.luxury); break;
                    case CurrencyType.Special: if (ce.special > 0f) Progress(rt, ce.special); break;
                }
            }
        }
    }

    private void OnCreditsSpent(object data)
    {
        if (data is SpendCreditsEvent se)
        {
            if (se.basic + se.premium + se.luxury + se.special <= 0f) return;

            foreach (var rt in activeDaily)
            {
                var def = rt.definition;
                if (def.type != ObjectiveType.SpendCredits) continue;
                switch (def.creditCurrencyType)
                {
                    case CurrencyType.Basic:   if (se.basic   > 0f) Progress(rt, se.basic);   break;
                    case CurrencyType.Premium: if (se.premium > 0f) Progress(rt, se.premium); break;
                    case CurrencyType.Luxury:  if (se.luxury  > 0f) Progress(rt, se.luxury);  break;
                    case CurrencyType.Special: if (se.special > 0f) Progress(rt, se.special); break;
                }
            }
        }
    }

    private void OnSkillUnlocked(object data) 
    {
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

    // ---- PUBLIC SLOT TIME HELPERS ----

    // Start (UTC) of the current slot
    public DateTime GetCurrentSlotStartUtc()
    {
        var now = DateTime.UtcNow;
        int slotStartHour = (now.Hour / slotLengthHours) * slotLengthHours;
        return new DateTime(now.Year, now.Month, now.Day, slotStartHour, 0, 0, DateTimeKind.Utc);
    }

    // Start (UTC) of the next slot
    public DateTime GetNextSlotStartUtc()
    {
        var start = GetCurrentSlotStartUtc();
        var next = start.AddHours(slotLengthHours);
        if (slotLengthHours <= 0) return start; // safety
        return next;
    }

    // Time remaining until next slot
    public TimeSpan GetTimeUntilNextSlot()
    {
        return GetNextSlotStartUtc() - DateTime.UtcNow;
    }

    // Seconds remaining (clamped >= 0)
    public double GetSecondsUntilNextSlot()
    {
        var ts = GetTimeUntilNextSlot();
        return Math.Max(0, ts.TotalSeconds);
    }

    // Formatted countdown "HH:MM:SS" (UTC based)
    public string GetNextSlotCountdownString()
    {
        var remain = GetTimeUntilNextSlot();
        if (remain.TotalSeconds < 0) remain = TimeSpan.Zero;
        return $"{(int)remain.TotalHours:00}:{remain.Minutes:00}:{remain.Seconds:00}";
    }

    // Current slot key (public if UI wants it)
    public string GetCurrentSlotKey() => CurrentSlotKey();

    // Helper: compute slot start for arbitrary UTC time
    private DateTime GetSlotStartForTime(DateTime utc)
    {
        int slotStartHour = (utc.Hour / slotLengthHours) * slotLengthHours;
        return new DateTime(utc.Year, utc.Month, utc.Day, slotStartHour, 0, 0, DateTimeKind.Utc);
    }

    private string SlotKeyFromStart(DateTime slotStartUtc)
    {
        return slotStartUtc.ToString("yyyyMMdd") + "-" + slotStartUtc.Hour.ToString("00");
    }

    private bool TryParseSlotKey(string key, out DateTime slotStartUtc)
    {
        slotStartUtc = DateTime.MinValue;
        if (string.IsNullOrEmpty(key)) return false;

        // Expected format: yyyyMMdd-HH
        var parts = key.Split('-');
        if (parts.Length != 2) return false;

        if (!DateTime.TryParseExact(parts[0],
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime day))
            return false;

        if (!int.TryParse(parts[1], out int hour)) return false;
        if (hour < 0 || hour >= 24) return false;

        // Snap hour to slot boundary just in case (defensive)
        int slotStartHour = (hour / slotLengthHours) * slotLengthHours;

        slotStartUtc = new DateTime(day.Year, day.Month, day.Day, slotStartHour, 0, 0, DateTimeKind.Utc);
        return true;
    }
}

