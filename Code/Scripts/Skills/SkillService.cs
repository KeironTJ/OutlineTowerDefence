using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillService : MonoBehaviour
{
    public static SkillService Instance { get; private set; }

    [SerializeField] private SkillDefinition[] loadedDefinitions;

    private readonly Dictionary<string, SkillDefinition> defs = new();
    private readonly Dictionary<string, PersistentSkillState> persistent = new();
    private readonly Dictionary<string, RoundSkillState> round = new();

    public event Action<string> SkillUpgraded;
    public event Action<string> SkillValueChanged;
    public event Action<string> SkillModifiersChanged;

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
        if (round.TryGetValue(id, out var r))
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

        if (currency == CurrencyType.Fragments && round.ContainsKey(id))
        {
            cost = GetRoundCost(id);
            nextVal = GetValueAtLevel(id, level + 1);
        }
        else
        {
            // Persistent preview: cost of next base level, value = effective +1
            if (!persistent.TryGetValue(id, out var p)) return UpgradePreview.Maxed;
            cost = GetCostPersistent(id, currency, p.baseLevel + 1);
            nextVal = GetValueAtLevel(id, level + 1);
        }

        return new UpgradePreview(id, level, level + 1, current, nextVal, cost, currency);
    }


    public UpgradePreview GetUpgradePreview(string id, CurrencyType currency, int startLevel, int upgrades)
    {
        if (!defs.TryGetValue(id, out var def)) return UpgradePreview.Maxed;

        int effectiveLevel = startLevel;                // current effective level (base + round)
        int max = def.maxLevel;

        // cap desired upgrades by remaining levels from effective
        int remainingByEffective = Mathf.Max(0, max - effectiveLevel);
        int stepsTarget = Mathf.Min(upgrades, remainingByEffective);
        if (stepsTarget <= 0) return UpgradePreview.Maxed;

        float totalCost = 0f;
        float currentValue = GetValueAtLevel(id, effectiveLevel);
        float nextValue = currentValue;

        if (currency == CurrencyType.Fragments && round.TryGetValue(id, out var r))
        {
            // Round costs depend on roundLevels only
            int startStep = r.roundLevels + 1; // next round step to buy
            for (int i = 0; i < stepsTarget; i++)
            {
                int step = startStep + i;
                float c = Mathf.Ceil(
                    SkillMath.EvaluateCurve(def.fragmentsCostCurve, def.baseFragmentsCost, def.fragmentsCostGrowth,
                        step, def.customCostCurveFragments)
                );
                totalCost += c;

                // value preview increments effective level
                effectiveLevel++;
                nextValue = GetValueAtLevel(id, effectiveLevel);
            }
        }
        else
        {
            // Persistent costs depend on persistent baseLevel, not effective level
            if (!persistent.TryGetValue(id, out var p)) return UpgradePreview.Maxed;

            // Also ensure we don't cross the global max considering current round state
            int effectiveRoundLevel = round.TryGetValue(id, out var rState)
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

                // value preview increments effective level
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
        if (currency != CurrencyType.Fragments) return false;
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
    }

    // ======== LEVEL / VALUE ========
    public int GetLevel(string id)
    {
        if (round.TryGetValue(id, out var r))
            return r.baseLevel + r.roundLevels;
        if (persistent.TryGetValue(id, out var p))
            return p.baseLevel;
        return 0;
    }

    private int GetEffectiveLevel(string id) => GetLevel(id);

    public float GetValue(string id)
    {
        if (!defs.TryGetValue(id, out var def)) return 0f;
        int effectiveLevel = Mathf.Max(1, GetEffectiveLevel(id));
        float baseVal = SkillMath.EvaluateCurve(def.valueCurve, def.baseValue, def.valueGrowth, effectiveLevel, def.customValueCurve);

        if (round.TryGetValue(id, out var r))
            baseVal = (baseVal + r.additiveBonus) * r.multiplicativeBonus;

        return baseVal;
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
        if (currency == CurrencyType.Fragments && round.ContainsKey(id))
            return GetRoundCost(id);
        if (persistent.TryGetValue(id, out var p))
            return GetCostPersistent(id, currency, p.baseLevel + 1);
        return 0f;
    }

    // ======== UPGRADE CHECKS ========
    public bool CanUpgradeRound(string id, CurrencyType currency, ICurrencyWallet wallet)
    {
        if (currency != CurrencyType.Fragments) return false;
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

    public bool CanUpgrade(string id, CurrencyType currency, ICurrencyWallet wallet) =>
        currency == CurrencyType.Fragments
            ? CanUpgradeRound(id, currency, wallet)
            : CanUpgradePersistent(id, currency, wallet);

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
}