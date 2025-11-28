using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementManager : SingletonMonoBehaviour<AchievementManager>
{
    public static event System.Action<AchievementRuntime> OnProgress;
    public static event System.Action<AchievementRuntime, AchievementTier> OnTierCompletedEvent;
    public static event System.Action<AchievementRuntime, AchievementTier> OnTierClaimedEvent;
    public static event System.Action OnListChanged; // NEW

    [Header("Configuration")]
    [SerializeField] private List<AchievementDefinition> allAchievements;

    private readonly List<AchievementRuntime> activeAchievements = new List<AchievementRuntime>();
    private readonly Dictionary<string, AchievementDefinition> achievementById = new Dictionary<string, AchievementDefinition>();

    private PlayerManager PlayerMgr => PlayerManager.main;
    private PlayerData PlayerData => PlayerMgr?.playerData;
    private bool initialized;

    protected override void OnAwakeAfterInit() { }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyed);
        EventManager.StartListening(EventNames.WaveCompleted, OnWaveCompleted);
        EventManager.StartListening(EventNames.RoundCompleted, OnRoundCompleted);
        EventManager.StartListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StartListening(EventNames.CurrencySpent, OnCurrencySpent);
        EventManager.StartListening(EventNames.BulletFired, OnProjectileFired);
        EventManager.StartListening(EventNames.DifficultyAchieved, OnDifficultyAchieved);
        EventManager.StartListening(EventNames.ResearchCompleted, OnResearchCompleted);
        EventManager.StartListening(EventNames.ChipUnlocked, OnChipUnlocked);
        EventManager.StartListening(EventNames.ChipUpgraded, OnChipUpgraded);
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
        EventManager.StopListening(EventNames.ResearchCompleted, OnResearchCompleted);
        EventManager.StopListening(EventNames.ChipUnlocked, OnChipUnlocked);
        EventManager.StopListening(EventNames.ChipUpgraded, OnChipUpgraded);
    }

    private void Start() => InitializeIfNeeded();
    public void EnsureInitialized() => InitializeIfNeeded();

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        if (allAchievements == null || allAchievements.Count == 0)
            allAchievements = DefinitionLoader.LoadAll<AchievementDefinition>("Data/Achievements");
        achievementById.Clear();
        if (allAchievements != null)
        {
            foreach (var def in allAchievements)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                achievementById[def.id] = def;
            }
        }
        LoadAchievementProgress();
        initialized = true;
    }

    private void RaiseListChanged() => OnListChanged?.Invoke();

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
                if (progressData.difficultyWaveProgress == null)
                {
                    progressData.difficultyWaveProgress = new List<DifficultyWaveProgressEntry>();
                    progressChanged = true;
                }
                activeAchievements.Add(new AchievementRuntime { definition = def, progressData = progressData });
                addedIds.Add(def.id);
            }
        }
        foreach (var def in allAchievements)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            if (addedIds.Contains(def.id)) continue;
            var newProgress = pm.GetOrCreateAchievementProgress(def.id);
            if (newProgress == null) continue;
            activeAchievements.Add(new AchievementRuntime { definition = def, progressData = newProgress });
            addedIds.Add(def.id);
            progressChanged = true;
        }
        progressChanged |= SyncReachDifficultyProgress();
        progressChanged |= SyncDifficultyWaveProgress();
        if (progressChanged)
        {
            pm.NotifyAchievementProgressChanged();
            SaveManager.main?.QueueImmediateSave();
            RaiseListChanged();
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
            if (rt.progressData.currentProgress >= highestDifficulty) continue;
            rt.progressData.currentProgress = highestDifficulty;
            rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");
            if (rt.definition.tiers != null && rt.definition.tiers.Length > 0)
            {
                int newHighestTier = rt.progressData.highestTierCompleted;
                for (int i = 0; i < rt.definition.tiers.Length; i++)
                {
                    if (highestDifficulty >= rt.definition.tiers[i].targetAmount) newHighestTier = i; else break;
                }
                if (newHighestTier != rt.progressData.highestTierCompleted) rt.progressData.highestTierCompleted = newHighestTier;
            }
            changed = true;
        }
        return changed;
    }

    private bool SyncDifficultyWaveProgress()
    {
        bool changed = false;
        foreach (var rt in activeAchievements)
        {
            if (rt.definition?.type != AchievementType.CompleteDifficultyWaves) continue;
            if (rt.progressData == null) continue;
            if (rt.progressData.difficultyWaveProgress == null)
            {
                rt.progressData.difficultyWaveProgress = new List<DifficultyWaveProgressEntry>();
                changed = true;
            }
            if (rt.definition.tiers == null || rt.definition.tiers.Length == 0) continue;
            int recalculatedHighest = rt.progressData.highestTierCompleted;
            for (int i = 0; i < rt.definition.tiers.Length; i++)
            {
                var tier = rt.definition.tiers[i];
                int requiredDifficulty = tier.requiredDifficultyLevel;
                int bestWave = GetHighestWaveForDifficulty(rt.progressData, requiredDifficulty);
                if (bestWave >= tier.targetAmount) { recalculatedHighest = i; continue; }
                break;
            }
            if (recalculatedHighest != rt.progressData.highestTierCompleted)
            {
                rt.progressData.highestTierCompleted = recalculatedHighest;
                changed = true;
            }
            if (UpdateDifficultyCurrentProgress(rt))
            {
                rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");
                changed = true;
            }
        }
        return changed;
    }

    public IReadOnlyList<AchievementRuntime> GetAllAchievements() { InitializeIfNeeded(); return activeAchievements; }
    public AchievementRuntime GetAchievement(string achievementId) { InitializeIfNeeded(); return activeAchievements.FirstOrDefault(a => a.definition.id == achievementId); }

    public List<AchievementRuntime> GetAchievementsSortedForUI()
    {
        InitializeIfNeeded();
        return activeAchievements
            // Completed last (false before true)
            .OrderBy(a => a.IsComplete)
            // Among incomplete, those with unclaimed rewards first
            .ThenByDescending(a => a.HasUnclaimedRewards)
            // More tiers completed before fewer
            .ThenByDescending(a => a.HighestTierCompleted)
            // Then in‑tier progress ratio
            .ThenByDescending(a => GetCurrentTierCompletionRatio(a))
            // Then overall completion percent
            .ThenByDescending(a => GetOverallCompletionPercent(a))
            // Stable fallback alphabetical
            .ThenBy(a => a.definition.displayName)
            .ToList();
    }

    public float GetCurrentTierCompletionRatio(AchievementRuntime rt)
    {
        if (rt?.definition?.tiers == null || rt.definition.tiers.Length == 0) return Mathf.Clamp01(rt?.Current ?? 0f);
        int nextIndex = rt.HighestTierCompleted + 1;
        if (nextIndex >= rt.definition.tiers.Length) return 1f;
        var tier = rt.definition.tiers[nextIndex];
        float target = tier.targetAmount <= 0 ? 1f : tier.targetAmount;
        float current = rt.Current;
        float spanStart = nextIndex > 0 ? rt.definition.tiers[nextIndex - 1].targetAmount : 0f;
        float spanSize = Mathf.Max(1f, target - spanStart);
        float inTier = Mathf.Clamp(current - spanStart, 0f, spanSize);
        return Mathf.Clamp01(inTier / spanSize);
    }

    public float GetOverallCompletionPercent(AchievementRuntime rt)
    {
        if (rt?.definition?.tiers == null || rt.definition.tiers.Length == 0) return Mathf.Clamp01(rt?.Current ?? 0f);
        int totalTiers = rt.definition.tiers.Length;
        int completed = rt.HighestTierCompleted + 1;
        if (completed >= totalTiers) return 1f;
        float currentTierRatio = GetCurrentTierCompletionRatio(rt);
        return Mathf.Clamp01((completed + currentTierRatio) / totalTiers);
    }

    private static int GetHighestWaveForDifficulty(AchievementProgressData data, int difficulty)
    {
        if (data?.difficultyWaveProgress == null || data.difficultyWaveProgress.Count == 0) return 0;
        if (difficulty <= 0)
        {
            int best = 0; for (int i = 0; i < data.difficultyWaveProgress.Count; i++) best = Mathf.Max(best, data.difficultyWaveProgress[i].highestWave); return best;
        }
        for (int i = 0; i < data.difficultyWaveProgress.Count; i++) if (data.difficultyWaveProgress[i].difficulty == difficulty) return data.difficultyWaveProgress[i].highestWave;
        return 0;
    }

    private static bool TryRecordDifficultyWave(AchievementProgressData data, int difficulty, int waveNumber)
    {
        if (data == null || difficulty <= 0) return false;
        data.difficultyWaveProgress ??= new List<DifficultyWaveProgressEntry>();
        for (int i = 0; i < data.difficultyWaveProgress.Count; i++)
        {
            var entry = data.difficultyWaveProgress[i];
            if (entry.difficulty != difficulty) continue;
            if (waveNumber > entry.highestWave)
            {
                entry.highestWave = waveNumber; data.difficultyWaveProgress[i] = entry; return true;
            }
            return false;
        }
        data.difficultyWaveProgress.Add(new DifficultyWaveProgressEntry(difficulty, waveNumber));
        return true;
    }

    private bool UpdateDifficultyCurrentProgress(AchievementRuntime rt)
    {
        if (rt.definition?.tiers == null || rt.definition.tiers.Length == 0) return false;
        if (rt.progressData == null) return false;
        int nextIndex = rt.progressData.highestTierCompleted + 1;
        float newProgress = (nextIndex >= rt.definition.tiers.Length)
            ? rt.definition.tiers[rt.definition.tiers.Length - 1].targetAmount
            : GetHighestWaveForDifficulty(rt.progressData, rt.definition.tiers[nextIndex].requiredDifficultyLevel);
        if (!Mathf.Approximately(rt.progressData.currentProgress, newProgress)) { rt.progressData.currentProgress = newProgress; return true; }
        return false;
    }

    private void Progress(AchievementRuntime rt, float amount)
    {
        if (rt == null || rt.definition == null || rt.definition.tiers == null) return;
        rt.progressData.currentProgress += amount;
        rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o");
        for (int i = rt.HighestTierCompleted + 1; i < rt.definition.tiers.Length; i++)
        {
            var tier = rt.definition.tiers[i];
            if (rt.Current >= tier.targetAmount) { rt.progressData.highestTierCompleted = i; OnTierCompleted(rt, tier, i); }
            else break;
        }
        SaveManager.main?.QueueImmediateSave();
        OnProgress?.Invoke(rt);
        RaiseListChanged();
    }

    private void OnTierCompleted(AchievementRuntime rt, AchievementTier tier, int tierIndex)
    {
        Debug.Log($"Achievement Tier Completed: {rt.definition.displayName} - {tier.tierName} (Tier {tierIndex + 1}) — awaiting manual claim");
        EventManager.TriggerEvent(EventNames.AchievementTierCompleted, new AchievementTierCompletedEvent { achievementId = rt.definition.id, tierIndex = tierIndex, tierName = tier.tierName });
        OnTierCompletedEvent?.Invoke(rt, tier);
    }

    private void GrantReward(AchievementReward reward)
    {
        var pm = PlayerManager.main; if (pm == null) return;
        switch (reward.rewardType)
        {
            case AchievementRewardType.Currency: pm.Wallet.Add(reward.currencyType, reward.amount); break;
            case AchievementRewardType.UnlockTurret: if (!string.IsNullOrEmpty(reward.rewardId)) pm.UnlockTurret(reward.rewardId); break;
            case AchievementRewardType.UnlockProjectile: if (!string.IsNullOrEmpty(reward.rewardId)) pm.UnlockProjectile(reward.rewardId); break;
            case AchievementRewardType.UnlockTowerBase: if (!string.IsNullOrEmpty(reward.rewardId)) pm.UnlockTowerBase(reward.rewardId); break;
        }
        SaveManager.main?.QueueImmediateSave();
    }

    private void GrantRewards(AchievementReward[] rewards)
    { if (rewards == null) return; foreach (var r in rewards) GrantReward(r); }

    public bool ClaimNextTier(AchievementRuntime rt) => ClaimPendingTiers(rt, false) > 0;
    public int ClaimAllPendingTiers(AchievementRuntime rt) => ClaimPendingTiers(rt, true);

    private int ClaimPendingTiers(AchievementRuntime rt, bool claimAll)
    {
        InitializeIfNeeded();
        if (rt == null || rt.definition?.tiers == null || rt.definition.tiers.Length == 0) return 0;
        if (rt.progressData.claimedTierIndices == null) rt.progressData.claimedTierIndices = new List<int>();
        var claimed = rt.progressData.claimedTierIndices; int claimedCount = 0;
        for (int index = 0; index <= rt.HighestTierCompleted && index < rt.definition.tiers.Length; index++)
        {
            if (claimed.Contains(index)) continue;
            var tier = rt.definition.tiers[index];
            GrantRewards(tier?.rewards);
            claimed.Add(index); claimedCount++; OnTierClaimedEvent?.Invoke(rt, tier);
            if (NotificationManager.Instance != null && tier != null)
                NotificationManager.Instance.ShowQuickNotification("Achievement Unlocked!", $"{rt.definition.displayName} - {tier.tierName} claimed!", NotificationSource.Achievement, 3f);
            if (!claimAll) break;
        }
        if (claimedCount > 0)
        {
            claimed.Sort(); SaveManager.main?.QueueImmediateSave(); CloudSyncService.main?.ScheduleUpload(); OnProgress?.Invoke(rt); RaiseListChanged();
        }
        return claimedCount;
    }

    private void OnEnemyDestroyed(object data)
    {
        InitializeIfNeeded(); if (data is not EnemyDestroyedDefinitionEvent e) return;
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.KillEnemies || rt.IsComplete) continue;
            if (rt.definition.Matches(e)) Progress(rt, 1f);
        }
    }

    private void OnWaveCompleted(object data)
    {
        InitializeIfNeeded(); int waveNumber = 0; int difficulty = 0;
        switch (data)
        {
            case WaveCompletedEvent wce: waveNumber = wce.waveNumber; difficulty = wce.difficulty; break;
            case int wave: waveNumber = wave; break;
            case float waveF: waveNumber = Mathf.RoundToInt(waveF); break;
            case double waveD: waveNumber = (int)System.Math.Round(waveD); break;
        }
        foreach (var rt in activeAchievements)
        {
            if (rt.IsComplete) continue;
            switch (rt.definition.type)
            {
                case AchievementType.CompleteWaves: if (waveNumber > 0) Progress(rt, 1f); break;
                case AchievementType.CompleteDifficultyWaves: HandleDifficultyWaveAchievement(rt, waveNumber, difficulty); break;
            }
        }
    }

    private void HandleDifficultyWaveAchievement(AchievementRuntime rt, int waveNumber, int difficulty)
    {
        if (rt?.definition?.tiers == null || rt.progressData == null) return; if (waveNumber <= 0 || difficulty <= 0) return;
        bool changed = TryRecordDifficultyWave(rt.progressData, difficulty, waveNumber); int previousTier = rt.progressData.highestTierCompleted;
        for (int i = previousTier + 1; i < rt.definition.tiers.Length; i++)
        {
            var tier = rt.definition.tiers[i]; int requiredDifficulty = tier.requiredDifficultyLevel > 0 ? tier.requiredDifficultyLevel : difficulty; int bestWave = GetHighestWaveForDifficulty(rt.progressData, requiredDifficulty);
            if (bestWave >= tier.targetAmount) { rt.progressData.highestTierCompleted = i; OnTierCompleted(rt, tier, i); changed = true; continue; }
            if (requiredDifficulty == difficulty || tier.requiredDifficultyLevel > 0) break;
        }
        if (UpdateDifficultyCurrentProgress(rt)) changed = true;
        if (changed) { rt.progressData.lastUpdatedIsoUtc = System.DateTime.UtcNow.ToString("o"); SaveManager.main?.QueueImmediateSave(); OnProgress?.Invoke(rt); RaiseListChanged(); }
    }

    private void OnRoundCompleted(object data)
    {
        InitializeIfNeeded(); foreach (var rt in activeAchievements) if (rt.definition.type == AchievementType.CompleteRounds && !rt.IsComplete) Progress(rt, 1f);
    }

    private void OnDifficultyAchieved(object data)
    {
        InitializeIfNeeded(); int difficulty = data switch { DifficultyAchievedEvent dae => dae.difficultyLevel, int i => i, float f => Mathf.RoundToInt(f), double d => (int)System.Math.Round(d), _ => 0 }; if (difficulty <= 0) return;
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.ReachDifficulty) continue; if (rt.IsComplete && rt.Current >= difficulty) continue; float delta = difficulty - rt.Current; if (delta > 0f) Progress(rt, delta);
        }
    }

    private void OnCurrencyEarned(object data)
    {
        InitializeIfNeeded(); if (data is not CurrencyEarnedEvent ce) return;
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.EarnCurrency || rt.IsComplete) continue;
            switch (rt.definition.currencyType)
            {
                case CurrencyType.Fragments: if (ce.fragments > 0) Progress(rt, ce.fragments); break;
                case CurrencyType.Cores: if (ce.cores > 0) Progress(rt, ce.cores); break;
                case CurrencyType.Prisms: if (ce.prisms > 0) Progress(rt, ce.prisms); break;
                case CurrencyType.Loops: if (ce.loops > 0) Progress(rt, ce.loops); break;
            }
        }
    }

    private void OnCurrencySpent(object data)
    {
        InitializeIfNeeded(); if (data is not SpendCurrencyEvent sce) return;
        foreach (var rt in activeAchievements)
        {
            if (rt.definition.type != AchievementType.SpendCurrency || rt.IsComplete) continue;
            switch (rt.definition.currencyType)
            {
                case CurrencyType.Fragments: if (sce.fragments > 0) Progress(rt, sce.fragments); break;
                case CurrencyType.Cores: if (sce.cores > 0) Progress(rt, sce.cores); break;
                case CurrencyType.Prisms: if (sce.prisms > 0) Progress(rt, sce.prisms); break;
                case CurrencyType.Loops: if (sce.loops > 0) Progress(rt, sce.loops); break;
            }
        }
    }

    private void OnResearchCompleted(object data) { InitializeIfNeeded(); if (data is not ResearchCompletedEvent) return; IncrementAchievementProgress(AchievementType.ResearchProgress, 1f); }
    private void OnChipUnlocked(object data) { InitializeIfNeeded(); if (data is not ChipUnlockedEvent) return; IncrementAchievementProgress(AchievementType.ChipProgress, 1f); }
    private void OnChipUpgraded(object data) { InitializeIfNeeded(); if (data is not ChipUpgradedEvent) return; IncrementAchievementProgress(AchievementType.ChipProgress, 1f); }

    private void IncrementAchievementProgress(AchievementType targetType, float amount)
    {
        if (amount <= 0f) return; foreach (var rt in activeAchievements) { if (rt.definition.type != targetType || rt.IsComplete) continue; Progress(rt, amount); }
    }

    private void OnProjectileFired(object data)
    {
        InitializeIfNeeded(); foreach (var rt in activeAchievements) if (rt.definition.type == AchievementType.ShootProjectiles && !rt.IsComplete) Progress(rt, 1f);
    }
}