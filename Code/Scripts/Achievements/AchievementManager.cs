using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    public static event System.Action<AchievementRuntime> OnProgress;
    public static event System.Action<AchievementRuntime, AchievementTier> OnTierCompletedEvent;
    public static event System.Action<AchievementRuntime, AchievementTier> OnTierClaimedEvent;

    [Header("Configuration")]
    [SerializeField] private List<AchievementDefinition> allAchievements;

    private readonly List<AchievementRuntime> activeAchievements = new List<AchievementRuntime>();
    private readonly Dictionary<string, AchievementDefinition> achievementById = new Dictionary<string, AchievementDefinition>();

    private PlayerManager PlayerMgr => PlayerManager.main;
    private PlayerData PlayerData => PlayerMgr?.playerData;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyed);
        EventManager.StartListening(EventNames.WaveCompleted, OnWaveCompleted);
        EventManager.StartListening(EventNames.RoundCompleted, OnRoundCompleted);
        EventManager.StartListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StartListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StartListening(EventNames.BulletFired, OnProjectileFired);
        EventManager.StartListening(EventNames.DifficultyAchieved, OnDifficultyAchieved);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyed);
        EventManager.StopListening(EventNames.WaveCompleted, OnWaveCompleted);
        EventManager.StopListening(EventNames.RoundCompleted, OnRoundCompleted);
        EventManager.StopListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StopListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StopListening(EventNames.BulletFired, OnProjectileFired);
        EventManager.StopListening(EventNames.DifficultyAchieved, OnDifficultyAchieved);
    }

    private void Start()
    {
        InitializeIfNeeded();
    }

    public void EnsureInitialized()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        
        // Auto-load from Resources if not assigned
        if (allAchievements == null || allAchievements.Count == 0)
        {
            var loaded = Resources.LoadAll<AchievementDefinition>("Data/Achievements");
            if (loaded != null && loaded.Length > 0)
                allAchievements = new List<AchievementDefinition>(loaded);
        }

        RebuildMap();
        LoadAchievementProgress();
        initialized = true;
    }

    private void RebuildMap()
    {
        achievementById.Clear();
        foreach (var def in allAchievements)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            achievementById[def.id] = def;
        }
    }

    private void LoadAchievementProgress()
    {
        activeAchievements.Clear();
        var pm = PlayerMgr;
        if (pm == null || pm.playerData == null)
        {
            Debug.LogWarning("[AchievementManager] PlayerManager not ready; delaying achievement progress load.");
            return;
        }

        var progressList = pm.GetAchievementProgressList();
        if (progressList == null) return;

        bool progressChanged = false;
        var addedIds = new HashSet<string>();

        foreach (var progressData in progressList)
        {
            if (progressData == null) continue;

            if (achievementById.TryGetValue(progressData.achievementId, out var def))
            {
                if (progressData.claimedTierIndices == null)
                {
                    progressData.claimedTierIndices = new List<int>();
                    if (progressData.highestTierCompleted >= 0 && def.tiers != null)
                    {
                        int maxIndex = Mathf.Min(progressData.highestTierCompleted, def.tiers.Length - 1);
                        for (int i = 0; i <= maxIndex; i++)
                            progressData.claimedTierIndices.Add(i);
                    }
                    progressChanged = true;
                }

                activeAchievements.Add(new AchievementRuntime
                {
                    definition = def,
                    progressData = progressData
                });
                addedIds.Add(def.id);
            }
        }

        foreach (var def in allAchievements)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            if (addedIds.Contains(def.id)) continue;

            var newProgress = pm.GetOrCreateAchievementProgress(def.id);
            if (newProgress == null) continue;

            activeAchievements.Add(new AchievementRuntime
            {
                definition = def,
                progressData = newProgress
            });
            addedIds.Add(def.id);
            progressChanged = true;
        }

        progressChanged |= SyncReachDifficultyProgress();

        if (progressChanged)
        {
            pm.NotifyAchievementProgressChanged();
            SaveManager.main?.QueueImmediateSave();
        }
    }

    private bool SyncReachDifficultyProgress()
    {
        var pm = PlayerMgr;
        if (pm == null) return false;

        int highestDifficulty = pm.GetMaxDifficultyAchieved();
        if (highestDifficulty <= 0) return false;

        bool changed = false;

        foreach (var rt in activeAchievements)
        {
            if (rt.definition?.type != AchievementType.ReachDifficulty) continue;
            if (rt.progressData == null) continue;

            if (rt.progressData.currentProgress >= highestDifficulty)
                continue;

            rt.progressData.currentProgress = highestDifficulty;
            rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");

            if (rt.definition.tiers != null && rt.definition.tiers.Length > 0)
            {
                int newHighestTier = rt.progressData.highestTierCompleted;
                for (int i = 0; i < rt.definition.tiers.Length; i++)
                {
                    if (highestDifficulty >= rt.definition.tiers[i].targetAmount)
                    {
                        newHighestTier = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (newHighestTier != rt.progressData.highestTierCompleted)
                    rt.progressData.highestTierCompleted = newHighestTier;
            }

            changed = true;
        }

        return changed;
    }

    public IReadOnlyList<AchievementRuntime> GetAllAchievements()
    {
        InitializeIfNeeded();
        return activeAchievements;
    }

    public AchievementRuntime GetAchievement(string achievementId)
    {
        InitializeIfNeeded();
        return activeAchievements.FirstOrDefault(a => a.definition.id == achievementId);
    }

    private void Progress(AchievementRuntime rt, float amount)
    {
        if (rt == null || rt.definition == null || rt.definition.tiers == null) return;

        rt.progressData.currentProgress += amount;
        rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");

        // Check for tier completions
        for (int i = rt.HighestTierCompleted + 1; i < rt.definition.tiers.Length; i++)
        {
            var tier = rt.definition.tiers[i];
            if (rt.Current >= tier.targetAmount)
            {
                rt.progressData.highestTierCompleted = i;
                OnTierCompleted(rt, tier, i);
            }
            else
            {
                break; // Stop at first uncompleted tier
            }
        }

        SaveManager.main?.QueueImmediateSave();
        OnProgress?.Invoke(rt);
    }

    private void OnTierCompleted(AchievementRuntime rt, AchievementTier tier, int tierIndex)
    {
        Debug.Log($"Achievement Tier Completed: {rt.definition.displayName} - {tier.tierName} (Tier {tierIndex + 1}) — awaiting manual claim");

        // Fire event for UI/other systems
        EventManager.TriggerEvent(EventNames.AchievementTierCompleted, new AchievementTierCompletedEvent
        {
            achievementId = rt.definition.id,
            tierIndex = tierIndex,
            tierName = tier.tierName
        });

        OnTierCompletedEvent?.Invoke(rt, tier);
    }

    private void GrantReward(AchievementReward reward)
    {
        var pm = PlayerManager.main;
        if (pm == null) return;

        switch (reward.rewardType)
        {
            case AchievementRewardType.Currency:
                pm.Wallet.Add(reward.currencyType, reward.amount);
                Debug.Log($"Granted {reward.amount} {reward.currencyType}");
                break;

            case AchievementRewardType.UnlockTurret:
                if (!string.IsNullOrEmpty(reward.rewardId))
                {
                    pm.UnlockTurret(reward.rewardId);
                    Debug.Log($"Unlocked turret: {reward.rewardId}");
                }
                break;

            case AchievementRewardType.UnlockProjectile:
                if (!string.IsNullOrEmpty(reward.rewardId))
                {
                    pm.UnlockProjectile(reward.rewardId);
                    Debug.Log($"Unlocked projectile: {reward.rewardId}");
                }
                break;

            case AchievementRewardType.UnlockTowerBase:
                if (!string.IsNullOrEmpty(reward.rewardId))
                {
                    pm.UnlockTowerBase(reward.rewardId);
                    Debug.Log($"Unlocked tower base: {reward.rewardId}");
                }
                break;
        }

        SaveManager.main?.QueueImmediateSave();
    }

    private void GrantRewards(AchievementReward[] rewards)
    {
        if (rewards == null) return;
        foreach (var reward in rewards)
        {
            GrantReward(reward);
        }
    }

    public bool ClaimNextTier(AchievementRuntime rt) => ClaimPendingTiers(rt, false) > 0;

    public int ClaimAllPendingTiers(AchievementRuntime rt) => ClaimPendingTiers(rt, true);

    private int ClaimPendingTiers(AchievementRuntime rt, bool claimAll)
    {
        InitializeIfNeeded();
        if (rt == null || rt.definition?.tiers == null || rt.definition.tiers.Length == 0)
            return 0;

        if (rt.progressData.claimedTierIndices == null)
            rt.progressData.claimedTierIndices = new List<int>();

        var claimed = rt.progressData.claimedTierIndices;
        int claimedCount = 0;

        for (int index = 0; index <= rt.HighestTierCompleted && index < rt.definition.tiers.Length; index++)
        {
            if (claimed.Contains(index)) continue;

            var tier = rt.definition.tiers[index];
            GrantRewards(tier?.rewards);
            claimed.Add(index);
            claimedCount++;
            OnTierClaimedEvent?.Invoke(rt, tier);

            if (!claimAll)
                break;
        }

        if (claimedCount > 0)
        {
            claimed.Sort();
            SaveManager.main?.QueueImmediateSave();
            CloudSyncService.main?.ScheduleUpload();
            OnProgress?.Invoke(rt);
        }

        return claimedCount;
    }

    // --- Event Handlers ---

    private void OnEnemyDestroyed(object data)
    {
        InitializeIfNeeded();
        if (data is not EnemyDestroyedDefinitionEvent e) return;

        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.KillEnemies) continue;
            if (rt.IsComplete) continue;

            if (rt.definition.Matches(e))
            {
                Progress(rt, 1f);
            }
        }
    }

    private void OnWaveCompleted(object data)
    {
        InitializeIfNeeded();

        foreach (var rt in activeAchievements)
        {
            if (rt.IsComplete) continue;

            if (rt.definition.type == AchievementType.CompleteWaves)
            {
                Progress(rt, 1f);
            }
        }
    }

    private void OnRoundCompleted(object data)
    {
        InitializeIfNeeded();
        
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type == AchievementType.CompleteRounds && !rt.IsComplete)
            {
                Progress(rt, 1f);
            }
        }
    }

    private void OnDifficultyAchieved(object data)
    {
        InitializeIfNeeded();

        int difficulty = data switch
        {
            DifficultyAchievedEvent dae => dae.difficultyLevel,
            int i => i,
            float f => Mathf.RoundToInt(f),
            double d => (int)System.Math.Round(d),
            _ => 0
        };

        if (difficulty <= 0) return;

        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.ReachDifficulty) continue;
            if (rt.IsComplete && rt.Current >= difficulty) continue;

            float delta = difficulty - rt.Current;
            if (delta > 0f)
            {
                Progress(rt, delta);
            }
        }
    }

    private void OnCurrencyEarned(object data)
    {
        InitializeIfNeeded();
        if (data is not CurrencyEarnedEvent ce) return;

        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.EarnCurrency) continue;
            if (rt.IsComplete) continue;

            switch (rt.definition.currencyType)
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
        InitializeIfNeeded();
        if (data is not SpendCurrencyEvent sce) return;

        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.SpendCurrency) continue;
            if (rt.IsComplete) continue;

            switch (rt.definition.currencyType)
            {
                case CurrencyType.Fragments: if (sce.fragments > 0) Progress(rt, sce.fragments); break;
                case CurrencyType.Cores:     if (sce.cores > 0)     Progress(rt, sce.cores); break;
                case CurrencyType.Prisms:    if (sce.prisms > 0)    Progress(rt, sce.prisms); break;
                case CurrencyType.Loops:     if (sce.loops > 0)     Progress(rt, sce.loops); break;
            }
        }
    }

    private void OnProjectileFired(object data)
    {
        InitializeIfNeeded();
        
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type == AchievementType.ShootProjectiles && !rt.IsComplete)
            {
                Progress(rt, 1f);
            }
        }
    }
}
