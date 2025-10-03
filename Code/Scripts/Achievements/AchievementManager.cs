using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    [Header("Configuration")]
    [SerializeField] private List<AchievementDefinition> allAchievements;

    private readonly List<AchievementRuntime> activeAchievements = new List<AchievementRuntime>();
    private readonly Dictionary<string, AchievementDefinition> achievementById = new Dictionary<string, AchievementDefinition>();

    private PlayerData PlayerData => SaveManager.main?.Current?.player;
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
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyed);
        EventManager.StopListening(EventNames.WaveCompleted, OnWaveCompleted);
        EventManager.StopListening(EventNames.RoundCompleted, OnRoundCompleted);
        EventManager.StopListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StopListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StopListening(EventNames.BulletFired, OnProjectileFired);
    }

    private void Start()
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
        
        if (PlayerData?.achievementProgress == null)
        {
            if (PlayerData != null)
                PlayerData.achievementProgress = new List<AchievementProgressData>();
            return;
        }

        // Create runtime objects from saved data
        foreach (var progressData in PlayerData.achievementProgress)
        {
            if (achievementById.TryGetValue(progressData.achievementId, out var def))
            {
                activeAchievements.Add(new AchievementRuntime
                {
                    definition = def,
                    progressData = progressData
                });
            }
        }

        // Add any new achievements that don't have progress yet
        foreach (var def in allAchievements)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            
            bool exists = activeAchievements.Any(a => a.definition.id == def.id);
            if (!exists)
            {
                var newProgress = new AchievementProgressData(def.id);
                PlayerData.achievementProgress.Add(newProgress);
                activeAchievements.Add(new AchievementRuntime
                {
                    definition = def,
                    progressData = newProgress
                });
            }
        }

        SaveManager.main?.QueueImmediateSave();
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
    }

    private void OnTierCompleted(AchievementRuntime rt, AchievementTier tier, int tierIndex)
    {
        Debug.Log($"Achievement Tier Completed: {rt.definition.displayName} - {tier.tierName} (Tier {tierIndex + 1})");

        // Grant rewards
        if (tier.rewards != null)
        {
            foreach (var reward in tier.rewards)
            {
                GrantReward(reward);
            }
        }

        // Fire event for UI/other systems
        EventManager.TriggerEvent(EventNames.AchievementTierCompleted, new AchievementTierCompletedEvent
        {
            achievementId = rt.definition.id,
            tierIndex = tierIndex,
            tierName = tier.tierName
        });
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
                    if (!pm.playerData.unlockedTowerBases.Contains(reward.rewardId))
                    {
                        pm.playerData.unlockedTowerBases.Add(reward.rewardId);
                        Debug.Log($"Unlocked tower base: {reward.rewardId}");
                    }
                }
                break;
        }

        SaveManager.main?.QueueImmediateSave();
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
        if (data is not WaveCompletedEvent wce) return;

        foreach (var rt in activeAchievements)
        {
            if (rt.IsComplete) continue;

            if (rt.definition.type == AchievementType.CompleteWaves)
            {
                Progress(rt, 1f);
            }
            else if (rt.definition.type == AchievementType.ReachDifficulty)
            {
                // Track highest wave per difficulty
                if (wce.waveNumber > rt.Current)
                {
                    rt.progressData.currentProgress = wce.waveNumber;
                    rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");
                    SaveManager.main?.QueueImmediateSave();
                }
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
