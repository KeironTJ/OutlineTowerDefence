using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillService : MonoBehaviour, IStatContributor
{
    public static SkillService Instance { get; private set; }

    [SerializeField] private SkillDefinition[] loadedDefinitions;

    private readonly Dictionary<string, SkillDefinition> defs = new();
    private readonly Dictionary<string, PersistentSkillState> persistent = new();
    private readonly Dictionary<string, RoundSkillState> round = new();

    public event Action<string> SkillUpgraded;
    public event Action<string> SkillValueChanged;

    private bool roundActive = false; 
    public bool RoundActive => roundActive;

    public void Contribute(StatCollector collector)
    {
        if (collector == null) return;

        foreach (var definition in defs.Values)
        {
            if (definition == null || !definition.HasStatMapping)
                continue;

            if (definition.requiresUnlockForContribution && !IsUnlocked(definition.id, persistentOnly: false))
                continue;

            if (definition.statContributionKind == SkillContributionKind.None)
                continue;

            float rawValue = definition.useSafeValue
                ? GetValueSafe(definition.id)
                : GetValue(definition.id);

            float pipelineValue = definition.ToPipelineValue(rawValue);

            switch (definition.statContributionKind)
            {
                case SkillContributionKind.Base:
                    collector.AddBase(definition.primaryStat, pipelineValue);
                    break;
                case SkillContributionKind.FlatBonus:
                    collector.AddFlatBonus(definition.primaryStat, pipelineValue);
                    break;
                case SkillContributionKind.Multiplier:
                    collector.AddMultiplier(definition.primaryStat, pipelineValue);
                    break;
                case SkillContributionKind.Percentage:
                    collector.AddPercentage(definition.primaryStat, pipelineValue);
                    break;
            }
        }
    }


    // --- PUBLIC DEFINITION ACCESS ---
    public SkillDefinition GetDefinition(string id) => defs.TryGetValue(id, out var d) ? d : null;
    public bool TryGetDefinition(string id, out SkillDefinition def) => defs.TryGetValue(id, out def);

    public int GetMaxLevel(string id) => defs.TryGetValue(id, out var d) ? d.maxLevel : 0;
    public IEnumerable<PersistentSkillState> GetPersistentStates() => persistent.Values;

    // --- PREVIEW / VALUE HELPERS ---
     public float GetValueAtLevel(string id, int level)
    {
        if (!defs.TryGetValue(id, out var def)) return 0f;
        level = Mathf.Max(1, level);
        float baseVal = SkillMath.EvaluateCurve(def.valueCurve, def.baseValue, def.valueGrowth, level, def.customValueCurve);
        if (roundActive && round.TryGetValue(id, out var r)) // gate by roundActive
            baseVal = (baseVal + r.additiveBonus) * r.multiplicativeBonus;
        return baseVal;
    }

    public float GetNextValue(string id)
    {
        int next = GetLevel(id) + 1;
        return GetValueAtLevel(id, next);
    }

    public UpgradePreview GetUpgradePreview(string id, CurrencyType currency)
    {
        var level = GetLevel(id);
        var max = GetMaxLevel(id);
        if (level >= max) return UpgradePreview.Maxed;

        float current = GetValue(id);
        float nextVal;
        float cost;

        if (currency == CurrencyType.Fragments && roundActive && round.ContainsKey(id)) // gate by roundActive
        {
            cost = GetRoundCost(id);
            nextVal = GetValueAtLevel(id, level + 1);
        }
        else
        {
            if (!persistent.TryGetValue(id, out var p)) return UpgradePreview.Maxed;
            cost = GetCostPersistent(id, currency, p.baseLevel + 1);
            nextVal = GetValueAtLevel(id, level + 1);
        }

        return new UpgradePreview(id, level, level + 1, current, nextVal, cost, currency);
    }

    public UpgradePreview GetUpgradePreview(string id, CurrencyType currency, int startLevel, int upgrades)
    {
        if (!defs.TryGetValue(id, out var def)) return UpgradePreview.Maxed;

        int effectiveLevel = startLevel;
        int max = def.maxLevel;

        int remainingByEffective = Mathf.Max(0, max - effectiveLevel);
        int stepsTarget = Mathf.Min(upgrades, remainingByEffective);
        if (stepsTarget <= 0) return UpgradePreview.Maxed;

        float totalCost = 0f;
        float currentValue = GetValueAtLevel(id, effectiveLevel);
        float nextValue = currentValue;

        if (currency == CurrencyType.Fragments)
        {
            if (!roundActive || !IsUpgradableInRound(id)) return UpgradePreview.Maxed;

            if (round.TryGetValue(id, out var r)) // gate by roundActive
            {
                int startStep = r.roundLevels + 1;
                for (int i = 0; i < stepsTarget; i++)
                {
                    int step = startStep + i;
                    float c = Mathf.Ceil(
                        SkillMath.EvaluateCurve(def.fragmentsCostCurve, def.baseFragmentsCost, def.fragmentsCostGrowth,
                            step, def.customCostCurveFragments)
                    );
                    totalCost += c;
                    effectiveLevel++;
                    nextValue = GetValueAtLevel(id, effectiveLevel);
                }
            }
        }
        else
        {
            if (!persistent.TryGetValue(id, out var p)) return UpgradePreview.Maxed;

            int effectiveRoundLevel = (roundActive && round.TryGetValue(id, out var rState))
                ? rState.baseLevel + rState.roundLevels
                : p.baseLevel;
            int remainingByMax = Mathf.Max(0, def.maxLevel - effectiveRoundLevel);
            stepsTarget = Mathf.Min(stepsTarget, remainingByMax);

            int baseLevelCursor = p.baseLevel;
            for (int i = 0; i < stepsTarget; i++)
            {
                int nextBase = baseLevelCursor + 1;
                float c = GetCostPersistent(id, currency, nextBase);
                totalCost += c;
                baseLevelCursor = nextBase;

                effectiveLevel++;
                nextValue = GetValueAtLevel(id, effectiveLevel);
            }
        }

        int endLevel = startLevel + stepsTarget;
        return new UpgradePreview(id, startLevel, endLevel, currentValue, nextValue, totalCost, currency);
    }

    // --- ROUND UPGRADE (fragments) ---
    public bool TryUpgradeRound(string id, CurrencyType currency, ICurrencyWallet wallet)
    {
        if (!roundActive) return false;
        if (currency != CurrencyType.Fragments) return false;
        if (!IsUpgradableInRound(id)) return false; 
        if (!CanUpgradeRound(id, currency, wallet)) return false;

        float cost = GetRoundCost(id);
        if (!wallet.TrySpend(CurrencyType.Fragments, cost)) return false;

        var r = round[id];
        r.roundLevels++;
        SkillUpgraded?.Invoke(id);
        SkillValueChanged?.Invoke(id);
        return true;
    }

    // --- PERSISTENT UPGRADE (cores etc.) ---
    public bool TryUpgradePersistent(string id, CurrencyType currency, ICurrencyWallet wallet)
    {
        if (!defs.TryGetValue(id, out var def)) return false;
        if (!persistent.TryGetValue(id, out var p)) return false;

        int effectiveRoundLevel = round.TryGetValue(id, out var rState)
            ? rState.baseLevel + rState.roundLevels
            : p.baseLevel;

        if (effectiveRoundLevel >= def.maxLevel) return false;

        float cost = GetCostPersistent(id, currency, p.baseLevel + 1);
        if (!wallet.TrySpend(currency, cost)) return false;

        p.baseLevel++;

        // Sync active round baseLevel (do not reset roundLevels)
        if (round.TryGetValue(id, out var r))
        {
            if (r.baseLevel < p.baseLevel)
                r.baseLevel = p.baseLevel;
        }

        SkillUpgraded?.Invoke(id);
        SkillValueChanged?.Invoke(id);
        return true;
    }

    // ======== ROUND STATE BUILD ========
    public void BuildRoundStates()
    {
        round.Clear();
        foreach (var kv in persistent)
        {
            var p = kv.Value;
            round[kv.Key] = new RoundSkillState
            {
                id = kv.Key,
                baseLevel = p.baseLevel,
                roundLevels = 0,
                unlocked = p.unlocked,
                additiveBonus = 0f,
                multiplicativeBonus = 1f
            };
        }
        roundActive = true; // mark active
    }

    public void ClearRoundStates() 
    {
        round.Clear();
        roundActive = false;
    }


    // ======== LEVEL / VALUE ========
    public int GetLevel(string id)
    {
        if (roundActive && round.TryGetValue(id, out var r)) // gate by roundActive
            return r.baseLevel + r.roundLevels;
        if (persistent.TryGetValue(id, out var p))
            return p.baseLevel;
        return 0;
    }

    private int GetEffectiveLevel(string id) => GetLevel(id);

    public float GetValue(string id)
    {
        if (!defs.TryGetValue(id, out var def)) return 0f;
        int effectiveLevel = Mathf.Max(1, GetLevel(id));
        float baseVal = SkillMath.EvaluateCurve(def.valueCurve, def.baseValue, def.valueGrowth, effectiveLevel, def.customValueCurve);

        if (roundActive && round.TryGetValue(id, out var r)) // gate by roundActive
            baseVal = (baseVal + r.additiveBonus) * r.multiplicativeBonus;

        return baseVal;
    }

    // --- New safe accessor: returns 1f when the skill id is not defined (useful for multipliers) ---
    public float GetValueSafe(string id)
    {
        if (string.IsNullOrEmpty(id)) return 1f;
        if (!defs.TryGetValue(id, out var def)) return 1f; // default multiplier
        return GetValue(id);
    }

    // ======== COSTS ========
    private float GetRoundCost(string id)
    {
        if (!defs.TryGetValue(id, out var def)) return 0f;
        if (!round.TryGetValue(id, out var r)) return 0f;

        int nextRoundStep = r.roundLevels + 1;
        return Mathf.Ceil(
            SkillMath.EvaluateCurve(def.fragmentsCostCurve, def.baseFragmentsCost, def.fragmentsCostGrowth,
                nextRoundStep, def.customCostCurveFragments)
        );
    }

    private float GetCostPersistent(string id, CurrencyType currency, int targetLevel)
    {
        if (!defs.TryGetValue(id, out var def)) return 0f;
        bool fragments = currency == CurrencyType.Fragments;
        float baseCost = fragments ? def.baseFragmentsCost : def.baseCoresCost;
        var curve = fragments ? def.fragmentsCostCurve : def.coresCostCurve;
        float growth = fragments ? def.fragmentsCostGrowth : def.coresCostGrowth;
        return Mathf.Ceil(
            SkillMath.EvaluateCurve(curve, baseCost, growth, targetLevel,
                fragments ? def.customCostCurveFragments : def.customCostCurveCores)
        );
    }

    public float GetCost(string id, CurrencyType currency)
    {
        if (currency == CurrencyType.Fragments && roundActive && round.ContainsKey(id))
            return GetRoundCost(id);
        if (persistent.TryGetValue(id, out var p))
            return GetCostPersistent(id, currency, p.baseLevel + 1);
        return 0f;
    }

    // ======== UPGRADE CHECKS ========
    public bool CanUpgradeRound(string id, CurrencyType currency, ICurrencyWallet wallet)
    {
        if (!roundActive) return false;
        if (currency != CurrencyType.Fragments) return false;
        if (!IsUpgradableInRound(id)) return false; 
        if (!defs.TryGetValue(id, out var def)) return false;
        if (!round.TryGetValue(id, out var r)) return false;

        int totalLevel = r.baseLevel + r.roundLevels;
        if (totalLevel >= def.maxLevel) return false;

        float cost = GetRoundCost(id);
        return wallet.Get(CurrencyType.Fragments) >= cost;
    }

    public bool CanUpgradePersistent(string id, CurrencyType currency, ICurrencyWallet wallet)
    {
        if (!defs.TryGetValue(id, out var def)) return false;
        if (!persistent.TryGetValue(id, out var p)) return false;
        if (p.baseLevel >= def.maxLevel) return false;
        float cost = GetCostPersistent(id, currency, p.baseLevel + 1);
        return wallet.Get(currency) >= cost;
    }

    public bool CanUnlockPersistent(string id, ICurrencyWallet wallet)
    {
        if (string.IsNullOrEmpty(id) || wallet == null) return false;
        if (!defs.TryGetValue(id, out var def)) return false;
        if (!persistent.TryGetValue(id, out var ps)) return false;
        if (ps.unlocked) return false;

        // prerequisite: either none or unlocked (persistent)
        if (!string.IsNullOrEmpty(def.prerequisiteSkillId))
        {
            if (!persistent.TryGetValue(def.prerequisiteSkillId, out var pre) || !pre.unlocked)
                return false;
        }

        float cost = Mathf.Max(0f, def.coresToUnlock);
        if (cost == 0f) return true;
        return wallet.Get(CurrencyType.Cores) >= cost;
    }

    public bool TryUnlockPersistent(string id, ICurrencyWallet wallet)
    {
        if (string.IsNullOrEmpty(id) || wallet == null) return false;
        if (!defs.TryGetValue(id, out var def)) return false;
        if (!persistent.TryGetValue(id, out var ps)) return false;
        if (ps.unlocked) return true;

        float cost = Mathf.Max(0f, def.coresToUnlock);
        if (cost > 0f && !wallet.TrySpend(CurrencyType.Cores, cost))
            return false;

        ps.unlocked = true;
        if (roundActive && round.TryGetValue(id, out var roundState))
            roundState.unlocked = true;

        SkillValueChanged?.Invoke(id);
        EventManager.TriggerEvent(EventNames.SkillUnlocked, new SkillUnlockedEvent(id));
        return true;
    }

    public bool IsSkillAvailable(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return true;
        if (!defs.TryGetValue(skillId, out var def)) return true; // missing definition -> treat as available (no prereq)
        if (string.IsNullOrEmpty(def.requiredTurretId)) return true;

        // require the turret to be unlocked via PlayerManager
        var pm = PlayerManager.main;
        if (pm == null) return false; // conservative: if no player manager, skill not available
        return pm.IsTurretUnlocked(def.requiredTurretId);
    }

    // ======== INIT / LOAD ========
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        IndexDefinitions();
    }

    private void IndexDefinitions()
    {
        defs.Clear();
        foreach (var d in loadedDefinitions)
        {
            if (d == null || string.IsNullOrEmpty(d.id)) continue;
            if (!defs.ContainsKey(d.id))
                defs.Add(d.id, d);
            else
                Debug.LogWarning($"Duplicate skill id {d.id}");
        }
    }

    public void LoadPersistentStates(List<PersistentSkillState> saved)
    {
        persistent.Clear();
        foreach (var def in defs.Values)
        {
            var s = saved.Find(x => x.id == def.id);
            if (s == null)
            {
                s = new PersistentSkillState
                {
                    id = def.id,
                    baseLevel = 1,
                    researchLevel = 0,
                    unlocked = def.startsUnlocked
                };
                saved.Add(s);
            }
            persistent[def.id] = s;
        }
    }

    // ======== CATEGORY ENUMERATION ========
    public IEnumerable<SkillDefinition> GetByCategory(SkillCategory cat)
    {
        foreach (var d in defs.Values)
            if (d.category == cat) yield return d;
    }

    public bool IsUnlocked(string id, bool persistentOnly = true)
    {
        if (!defs.TryGetValue(id, out var def)) return false;

        if (persistentOnly)
        {
            if (persistent != null && persistent.TryGetValue(id, out var p)) return p.unlocked;
            return def.startsUnlocked;
        }

        // Include round state if active
        if (roundActive && round != null && round.TryGetValue(id, out var r)) return r.unlocked;
        if (persistent != null && persistent.TryGetValue(id, out var p2)) return p2.unlocked;
        return def.startsUnlocked;
    }

    public bool IsUpgradableInRound(string id)
    {
        return defs.TryGetValue(id, out var def) && def.upgradableInRound;
    }
}