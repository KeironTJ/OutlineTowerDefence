using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentUnlockService : SingletonMonoBehaviour<ContentUnlockService>
{
    private readonly HashSet<string> defaultUnlocksApplied = new HashSet<string>(StringComparer.Ordinal);

    protected override void OnAwakeAfterInit()
    {
        // nothing yet; defaults handled in Start coroutine
    }

    private IEnumerator Start()
    {
        while (PlayerManager.main == null || !PlayerManager.main.IsInitialized)
            yield return null;

        float timeout = Time.realtimeSinceStartup + 5f;
        while (!AreDefinitionManagersLikelyReady() && Time.realtimeSinceStartup < timeout)
            yield return null;

        EnsureDefaultUnlocks();
    }

    private bool AreDefinitionManagersLikelyReady()
    {
        return TowerBaseManager.Instance != null &&
               TurretDefinitionManager.Instance != null &&
               ProjectileDefinitionManager.Instance != null;
    }

    public void RefreshDefaultUnlocks() => EnsureDefaultUnlocks();

    public bool IsUnlocked(UnlockableContentType type, string id)
    {
        return IsUnlockedInternal(type, id, PlayerManager.main);
    }

    public bool CanUnlock(UnlockableContentType type, string id, out UnlockPathInfo bestPath, out string reason)
    {
        bestPath = default;
        reason = string.Empty;

        var pm = PlayerManager.main;
        if (pm?.playerData == null)
        {
            reason = "Player not ready";
            return false;
        }

        if (IsUnlockedInternal(type, id, pm))
        {
            reason = "Already unlocked";
            return false;
        }

        if (!(ResolveDefinition(type, id) is IUnlockableDefinition def))
        {
            reason = "Definition missing";
            return false;
        }

        var snapshot = new PlayerProgressSnapshot(pm);
        var evaluation = EvaluateUnlock(def, pm, snapshot);
        if (evaluation.canUnlock)
        {
            bestPath = evaluation.path;
            reason = evaluation.path.description;
            return true;
        }

        reason = string.IsNullOrEmpty(evaluation.reason)
            ? (def.UnlockProfile?.lockedHint ?? "Locked")
            : evaluation.reason;
        return false;
    }

    public bool TryUnlock(UnlockableContentType type, string id, out string failReason)
    {
        failReason = string.Empty;
        var pm = PlayerManager.main;
        if (pm?.playerData == null)
        {
            failReason = "Player not ready";
            return false;
        }

        if (IsUnlockedInternal(type, id, pm))
            return true;

        if (!(ResolveDefinition(type, id) is IUnlockableDefinition def))
        {
            failReason = "Definition missing";
            return false;
        }

        var snapshot = new PlayerProgressSnapshot(pm);
        var evaluation = EvaluateUnlock(def, pm, snapshot);
        if (!evaluation.canUnlock)
        {
            failReason = string.IsNullOrEmpty(evaluation.reason)
                ? (def.UnlockProfile?.lockedHint ?? "Locked")
                : evaluation.reason;
            return false;
        }

        if (!SpendCosts(evaluation.path, pm))
        {
            failReason = "Insufficient currency";
            return false;
        }

        GrantUnlock(type, id, pm);
        return true;
    }

    private bool SpendCosts(UnlockPathInfo path, PlayerManager pm)
    {
        return path.totalCost.TrySpend(pm);
    }

    private void EnsureDefaultUnlocks()
    {
        var pm = PlayerManager.main;
        if (pm?.playerData == null)
            return;

        foreach (UnlockableContentType type in Enum.GetValues(typeof(UnlockableContentType)))
        {
            foreach (var def in EnumerateDefinitions(type))
            {
                if (def == null) continue;
                var profile = def.UnlockProfile;
                if (profile == null || !profile.grantByDefault) continue;
                if (IsUnlockedInternal(type, def.DefinitionId, pm)) continue;

                string key = BuildDefaultKey(type, def.DefinitionId);
                if (defaultUnlocksApplied.Contains(key))
                    continue;

                GrantUnlock(type, def.DefinitionId, pm);
                defaultUnlocksApplied.Add(key);
            }
        }
    }

    private static string BuildDefaultKey(UnlockableContentType type, string id) => $"{type}:{id}";

    private IUnlockableDefinition ResolveDefinition(UnlockableContentType type, string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        switch (type)
        {
            case UnlockableContentType.TowerBase:
                return TowerBaseManager.Instance?.GetBaseById(id) as IUnlockableDefinition;
            case UnlockableContentType.Turret:
                return TurretDefinitionManager.Instance?.GetById(id) as IUnlockableDefinition;
            case UnlockableContentType.Projectile:
                return ProjectileDefinitionManager.Instance?.GetById(id) as IUnlockableDefinition;
            default:
                return null;
        }
    }

    private IEnumerable<IUnlockableDefinition> EnumerateDefinitions(UnlockableContentType type)
    {
        switch (type)
        {
            case UnlockableContentType.TowerBase:
                {
                var manager = TowerBaseManager.Instance;
                var bases = manager?.allBases;
                if (bases == null) break;
                foreach (var def in bases)
                        if (def is IUnlockableDefinition unlockable)
                            yield return unlockable;
                break;
                }
            case UnlockableContentType.Turret:
                {
                var manager = TurretDefinitionManager.Instance;
                var defs = manager?.GetAll();
                if (defs == null) break;
                foreach (var def in defs)
                        if (def is IUnlockableDefinition unlockable)
                            yield return unlockable;
                break;
                }
            case UnlockableContentType.Projectile:
                {
                var manager = ProjectileDefinitionManager.Instance;
                var defs = manager?.GetAll();
                if (defs == null) break;
                foreach (var def in defs)
                        if (def is IUnlockableDefinition unlockable)
                            yield return unlockable;
                break;
                }
        }

        yield break;
    }

    private bool IsUnlockedInternal(UnlockableContentType type, string id, PlayerManager pm)
    {
        if (pm?.playerData == null || string.IsNullOrEmpty(id))
            return false;

        switch (type)
        {
            case UnlockableContentType.TowerBase:
                return pm.playerData.unlockedTowerBases != null && pm.playerData.unlockedTowerBases.Contains(id);
            case UnlockableContentType.Turret:
                return pm.IsTurretUnlocked(id);
            case UnlockableContentType.Projectile:
                return pm.IsProjectileUnlocked(id);
            default:
                return false;
        }
    }

    private void GrantUnlock(UnlockableContentType type, string id, PlayerManager pm)
    {
        if (pm == null || string.IsNullOrEmpty(id))
            return;

        switch (type)
        {
            case UnlockableContentType.TowerBase:
                pm.UnlockTowerBase(id);
                break;
            case UnlockableContentType.Turret:
                pm.UnlockTurret(id);
                GrantDefaultProjectileFromTurret(id, pm);
                break;
            case UnlockableContentType.Projectile:
                pm.UnlockProjectile(id);
                break;
        }
    }

    private void GrantDefaultProjectileFromTurret(string turretId, PlayerManager pm)
    {
        var turret = TurretDefinitionManager.Instance?.GetById(turretId);
        if (turret == null || string.IsNullOrEmpty(turret.defaultProjectileId))
            return;
        pm.UnlockProjectile(turret.defaultProjectileId);
    }

    private UnlockEvaluation EvaluateUnlock(IUnlockableDefinition def, PlayerManager pm, PlayerProgressSnapshot snapshot)
    {
        var profile = def.UnlockProfile ?? new UnlockProfile();
        if (profile.grantByDefault)
        {
            var defaultPath = new UnlockPathInfo(null, default, "Granted", profile.lockedHint);
            return UnlockEvaluation.Success(defaultPath, "Granted");
        }

        if (profile.requirementGroups == null || profile.requirementGroups.Length == 0)
            return UnlockEvaluation.Failure(profile.lockedHint);

        string lastReason = profile.lockedHint;
        foreach (var group in profile.requirementGroups)
        {
            var groupEvaluation = EvaluateGroup(group, pm, snapshot);
            if (groupEvaluation.passed)
                return UnlockEvaluation.Success(groupEvaluation.path, group?.description);

            if (!string.IsNullOrEmpty(groupEvaluation.failureReason))
                lastReason = groupEvaluation.failureReason;
        }

        return UnlockEvaluation.Failure(lastReason);
    }

    private GroupEvaluationResult EvaluateGroup(UnlockRequirementGroup group, PlayerManager pm, PlayerProgressSnapshot snapshot)
    {
        if (group == null || group.requirements == null || group.requirements.Length == 0)
        {
            return GroupEvaluationResult.Success(new UnlockPathInfo(group, default, group?.label, group?.description));
        }

        var totalCost = default(UnlockCurrencyCost);
        foreach (var req in group.requirements)
        {
            if (req == null) continue;
            var check = EvaluateRequirement(req, pm, snapshot);
            if (!check.passed)
            {
                string reason = !string.IsNullOrEmpty(req.failureHint) ? req.failureHint : check.reason;
                if (string.IsNullOrEmpty(reason))
                    reason = group.description;
                return GroupEvaluationResult.Failure(reason);
            }

            if (req.requirementType == UnlockRequirementType.SpendCurrency)
                totalCost += req.currencyCost;
        }

        return GroupEvaluationResult.Success(new UnlockPathInfo(group, totalCost, group.label, group.description));
    }

    private RequirementCheckResult EvaluateRequirement(UnlockRequirement req, PlayerManager pm, PlayerProgressSnapshot snapshot)
    {
        switch (req.requirementType)
        {
            case UnlockRequirementType.HighestWave:
                return CheckHighestWave(req, snapshot);
            case UnlockRequirementType.MaxDifficulty:
                return CheckMaxDifficulty(req, snapshot);
            case UnlockRequirementType.AchievementCompleted:
                return CheckAchievementRequirement(req);
            case UnlockRequirementType.DefinitionUnlocked:
                return CheckDefinitionRequirement(req, pm);
            case UnlockRequirementType.SpendCurrency:
                return CheckCurrencyRequirement(req, pm);
            default:
                return RequirementCheckResult.Success();
        }
    }

    private RequirementCheckResult CheckHighestWave(UnlockRequirement req, PlayerProgressSnapshot snapshot)
    {
        int requiredWave = Mathf.Max(1, req.thresholdValue);
        int bestWave = req.difficultyLevel > 0
            ? snapshot.GetHighestWave(req.difficultyLevel)
            : snapshot.HighestWaveAny;

        if (bestWave >= requiredWave)
            return RequirementCheckResult.Success();

        string detail = req.difficultyLevel > 0
            ? $"Reach wave {requiredWave} on difficulty {req.difficultyLevel}"
            : $"Reach wave {requiredWave}";
        return RequirementCheckResult.Failure(detail);
    }

    private RequirementCheckResult CheckMaxDifficulty(UnlockRequirement req, PlayerProgressSnapshot snapshot)
    {
        int required = Mathf.Max(1, req.thresholdValue);
        if (snapshot.HighestDifficulty >= required)
            return RequirementCheckResult.Success();
        return RequirementCheckResult.Failure($"Reach difficulty {required}");
    }

    private RequirementCheckResult CheckAchievementRequirement(UnlockRequirement req)
    {
        var manager = AchievementManager.Instance;
        if (manager == null)
            return RequirementCheckResult.Failure("Achievements unavailable");

        manager.EnsureInitialized();
        if (req.referencedIds == null || req.referencedIds.Length == 0)
            return RequirementCheckResult.Success();

        foreach (var id in req.referencedIds)
        {
            if (string.IsNullOrEmpty(id))
                continue;

            var runtime = manager.GetAchievement(id);
            if (!IsAchievementSatisfied(runtime, req.minimumAchievementTier))
            {
                string friendly = runtime?.definition?.displayName ?? id;
                return RequirementCheckResult.Failure($"Complete achievement {friendly}");
            }
        }

        return RequirementCheckResult.Success();
    }

    private RequirementCheckResult CheckDefinitionRequirement(UnlockRequirement req, PlayerManager pm)
    {
        if (req.referencedIds == null || req.referencedIds.Length == 0)
            return RequirementCheckResult.Success();

        bool all = req.matchMode == RequirementMatchMode.All;
        bool anyMet = false;
        foreach (var id in req.referencedIds)
        {
            if (string.IsNullOrEmpty(id))
                continue;
            bool unlocked = IsUnlockedInternal(req.prerequisiteType, id, pm);
            if (all && !unlocked)
            {
                string friendly = GetDefinitionName(req.prerequisiteType, id);
                return RequirementCheckResult.Failure($"Unlock {friendly}");
            }

            if (!all && unlocked)
            {
                anyMet = true;
                break;
            }
        }

        if (all)
            return RequirementCheckResult.Success();

        if (!anyMet)
        {
            string friendly = req.referencedIds.Length == 1
                ? GetDefinitionName(req.prerequisiteType, req.referencedIds[0])
                : "required content";
            return RequirementCheckResult.Failure($"Unlock {friendly}");
        }

        return RequirementCheckResult.Success();
    }

    private RequirementCheckResult CheckCurrencyRequirement(UnlockRequirement req, PlayerManager pm)
    {
        if (req.currencyCost.IsFree)
            return RequirementCheckResult.Success();

        if (req.currencyCost.CanAfford(pm))
            return RequirementCheckResult.Success();

        string label = req.currencyCost.ToLabel();
        return RequirementCheckResult.Failure(string.IsNullOrEmpty(label) ? "Insufficient funds" : $"Need {label}");
    }

    private bool IsAchievementSatisfied(AchievementRuntime runtime, int requiredTier)
    {
        if (runtime == null)
            return false;

        int targetTier = requiredTier >= 0
            ? requiredTier
            : (runtime.definition?.tiers?.Length ?? 1) - 1;
        targetTier = Mathf.Max(0, targetTier);
        return runtime.HighestTierCompleted >= targetTier;
    }

    private string GetDefinitionName(UnlockableContentType type, string id)
    {
        return type switch
        {
            UnlockableContentType.Projectile => DefinitionDisplayNameUtility.GetProjectileName(id),
            UnlockableContentType.Turret => DefinitionDisplayNameUtility.GetTurretName(id),
            UnlockableContentType.TowerBase => DefinitionDisplayNameUtility.GetTowerBaseName(id),
            _ => id
        };
    }

    private readonly struct PlayerProgressSnapshot
    {
        public readonly int HighestDifficulty;
        public readonly int HighestWaveAny;
        private readonly int[] wavesByDifficulty;

        public PlayerProgressSnapshot(PlayerManager pm)
        {
            HighestDifficulty = pm?.GetMaxDifficultyAchieved() ?? 0;
            var source = pm?.playerData?.difficultyMaxWaveAchieved;
            if (source != null && source.Length > 0)
            {
                wavesByDifficulty = new int[source.Length];
                Array.Copy(source, wavesByDifficulty, source.Length);
            }
            else
            {
                wavesByDifficulty = Array.Empty<int>();
            }

            HighestWaveAny = 0;
            if (wavesByDifficulty.Length > 0)
            {
                for (int i = 0; i < wavesByDifficulty.Length; i++)
                    if (wavesByDifficulty[i] > HighestWaveAny)
                        HighestWaveAny = wavesByDifficulty[i];
            }
        }

        public int GetHighestWave(int difficulty)
        {
            if (wavesByDifficulty.Length == 0)
                return 0;
            if (difficulty <= 0)
                return HighestWaveAny;

            int index = Mathf.Clamp(difficulty - 1, 0, wavesByDifficulty.Length - 1);
            return wavesByDifficulty[index];
        }
    }

    private readonly struct RequirementCheckResult
    {
        public readonly bool passed;
        public readonly string reason;

        private RequirementCheckResult(bool passed, string reason)
        {
            this.passed = passed;
            this.reason = reason;
        }

        public static RequirementCheckResult Success() => new RequirementCheckResult(true, string.Empty);
        public static RequirementCheckResult Failure(string reason) => new RequirementCheckResult(false, reason);
    }

    private readonly struct GroupEvaluationResult
    {
        public readonly bool passed;
        public readonly UnlockPathInfo path;
        public readonly string failureReason;

        private GroupEvaluationResult(bool passed, UnlockPathInfo path, string failureReason)
        {
            this.passed = passed;
            this.path = path;
            this.failureReason = failureReason;
        }

        public static GroupEvaluationResult Success(UnlockPathInfo path) => new GroupEvaluationResult(true, path, string.Empty);
        public static GroupEvaluationResult Failure(string reason) => new GroupEvaluationResult(false, default, reason);
    }

    private readonly struct UnlockEvaluation
    {
        public readonly bool canUnlock;
        public readonly UnlockPathInfo path;
        public readonly string reason;

        private UnlockEvaluation(bool canUnlock, UnlockPathInfo path, string reason)
        {
            this.canUnlock = canUnlock;
            this.path = path;
            this.reason = reason;
        }

        public static UnlockEvaluation Success(UnlockPathInfo path, string reason) => new UnlockEvaluation(true, path, reason);
        public static UnlockEvaluation Failure(string reason) => new UnlockEvaluation(false, default, reason);
    }
}
