using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Round Data")]
    [SerializeField] private RoundType roundType;
    [SerializeField] private EnemyTypeDefinition[] enemyTypes;
    [SerializeField] private TierShareConfig tierShareConfig;

    [Header("Random Seed")]
    [SerializeField] private int baseSeed = 12345;

    [Header("Runtime State")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private bool isWaveActive = false;
    [SerializeField] private bool inBreak = false;
    [SerializeField] private float waveEndTime;
    [SerializeField] private float breakEndTime;
    [SerializeField] private float currentWaveTotalDuration;
    [SerializeField] private float currentBreakTotalDuration;

    private readonly List<(float t, EnemyTypeDefinition enemy)> schedule = new(256);
    private int scheduleIndex = 0;
    private float waveStartTime;
    private WaveContext currentWaveContext;

    private Coroutine loopRoutine;
    private Tower subscribedTower;

    // Working pools
    private readonly List<EnemyTypeDefinition> poolBasic = new();
    private readonly List<EnemyTypeDefinition> poolAdv = new();
    private readonly List<EnemyTypeDefinition> poolElite = new();

    // API
    public int GetCurrentWave() => currentWave;
    public bool IsWaveActive() => isWaveActive;
    public bool IsBetweenWaves() => !isWaveActive && inBreak;
    public float GetWaveTimeRemaining() => isWaveActive ? Mathf.Max(0, waveEndTime - Time.time) : 0f;
    public float GetBreakTimeRemaining() => inBreak ? Mathf.Max(0, breakEndTime - Time.time) : 0f;
    public float GetWaveDuration() => currentWaveTotalDuration;
    public float GetBreakTotalDuration() => currentBreakTotalDuration;
    public int SafeCurrentWave() => currentWave;
    public float SpawnProgress => schedule.Count == 0 ? 0f : Mathf.Clamp01((float)scheduleIndex / schedule.Count);
    public float TimeProgress => isWaveActive && currentWaveTotalDuration > 0f
        ? 1f - (GetWaveTimeRemaining() / currentWaveTotalDuration) : 0f;

    private float currentWaveBudget = 0f; // total budget for the current wave

    public void StartWaveSystem(EnemySpawner spawner, Tower coreTower)
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        if (!roundType)
        {
            Debug.LogError("[WaveManager] RoundType missing.");
            return;
        }
        subscribedTower = coreTower;
        if (subscribedTower) subscribedTower.TowerDestroyed += EndAllWaves;
        loopRoutine = StartCoroutine(WaveLoop(spawner, coreTower));
    }

    public void ForceStartNextWave()
    {
        if (inBreak) breakEndTime = Time.time;
    }

    private IEnumerator WaveLoop(EnemySpawner spawner, Tower tower)
    {
        while (true)
        {
            currentWave++;
            PrepareWaveSchedule();

            isWaveActive = true;
            inBreak = false;
            waveStartTime = Time.time;
            currentWaveTotalDuration = roundType.GetWaveDuration(currentWave);
            waveEndTime = waveStartTime + currentWaveTotalDuration;

            EventManager.TriggerEvent(EventNames.NewWaveStarted, currentWave);

            if (roundType.bossEvery > 0 && currentWave % roundType.bossEvery == 0)
                SpawnBossNow(spawner, tower);

            scheduleIndex = 0;
            while (Time.time < waveEndTime && isWaveActive && tower != null)
            {
                float elapsed = Time.time - waveStartTime;
                while (scheduleIndex < schedule.Count && schedule[scheduleIndex].t <= elapsed)
                {
                    SpawnEnemy(spawner, tower, schedule[scheduleIndex].enemy);
                    scheduleIndex++;
                }
                yield return null;
            }

            if (isWaveActive)
            {
                var playerManager = PlayerManager.main;
                int difficulty = playerManager ? playerManager.GetDifficulty() : 1;

                EventManager.TriggerEvent(EventNames.WaveCompleted, new WaveCompletedEvent(currentWave, difficulty));
            }

            isWaveActive = false;
            if (tower == null) yield break;

            currentBreakTotalDuration = roundType.breakDuration;
            inBreak = true;
            breakEndTime = Time.time + currentBreakTotalDuration;
            while (Time.time < breakEndTime && inBreak) yield return null;
            inBreak = false;
        }
    }

    private void PrepareWaveSchedule()
    {
        schedule.Clear();

        currentWaveContext = new WaveContext
        {
            wave = currentWave,
            healthMult = roundType.GetHealthMultiplier(currentWave),
            speedMult = roundType.GetSpeedMultiplier(currentWave),
            damageMult = roundType.GetDamageMultiplier(currentWave),
            rewardMult = roundType.GetRewardMultiplier(currentWave),
            rng = new System.Random(baseSeed + currentWave),
            eliteChance = roundType.eliteChanceCurve.Evaluate(currentWave)
        };

        float duration = roundType.GetWaveDuration(currentWave);
        float totalBudget = roundType.GetBudget(currentWave);

        // store total budget for threat calculations / UI
        currentWaveBudget = Mathf.Max(0.0001f, totalBudget);

        EnemyRosterBuilder.BuildTierPools(currentWave, enemyTypes, poolBasic, poolAdv, poolElite);

        if (poolBasic.Count == 0 && poolAdv.Count == 0 && poolElite.Count == 0)
        {
            Debug.LogWarning("[WaveManager] No eligible enemies this wave.");
            return;
        }

        var (basicShare, advShare, eliteShare) = tierShareConfig
            ? tierShareConfig.GetShares(currentWave)
            : (basic:0.6f, advanced:0.3f, elite:0.1f);

        float basicBudget = totalBudget * basicShare;
        float advBudget   = totalBudget * advShare;
        float eliteBudget = totalBudget * eliteShare;

        BuildTierSchedule(poolBasic, basicBudget, duration, promoteBasics: true);
        BuildTierSchedule(poolAdv, advBudget, duration);
        BuildTierSchedule(poolElite, eliteBudget, duration);

        schedule.Sort((a, b) => a.t.CompareTo(b.t));
    }

    private void BuildTierSchedule(
        List<EnemyTypeDefinition> pool,
        float tierBudget,
        float duration,
        bool promoteBasics = false)
    {
        if (tierBudget <= 0 || pool.Count == 0) return;

        float totalWeight = 0f;
        foreach (var d in pool) totalWeight += Mathf.Max(0.0001f, d.GetWeight(currentWaveContext.wave));

        if (totalWeight <= 0f) return;

        float spent = 0f;
        float cursor = 0f;
        float avgCost = 0f;
        foreach (var d in pool) avgCost += d.budgetCost;
        avgCost = Mathf.Max(1f, avgCost / pool.Count);
        int estCount = Mathf.CeilToInt(tierBudget / avgCost);
        float baseInterval = duration / Mathf.Max(1, estCount);

        while (spent < tierBudget && cursor < duration)
        {
            double roll = currentWaveContext.rng.NextDouble() * totalWeight;
            EnemyTypeDefinition chosen = null;
            foreach (var d in pool)
            {
                roll -= Mathf.Max(0.0001f, d.GetWeight(currentWaveContext.wave));
                if (roll <= 0) { chosen = d; break; }
            }
            chosen ??= pool[pool.Count - 1];

            EnemyTypeDefinition final = chosen;
            if (promoteBasics && chosen.tier == EnemyTier.Basic)
                final = EnemyRosterBuilder.MaybePromote(currentWaveContext.wave, chosen, poolAdv, poolElite, currentWaveContext.rng);

            if (spent + final.budgetCost > tierBudget && (tierBudget - spent) < 1f)
                break;

            schedule.Add((cursor, final));
            spent += final.budgetCost;

            float jitter = Mathf.Lerp(0.8f, 1.2f, (float)currentWaveContext.rng.NextDouble());
            cursor += baseInterval * jitter;
        }
    }

    private void SpawnEnemy(EnemySpawner spawner, Tower tower, EnemyTypeDefinition def)
    {
        if (!spawner || !tower || !def || !def.prefab) return;
        var go = spawner.SpawnEnemy(def.prefab, tower);
        if (!go) return;

        var runtime = go.GetComponent<IEnemyRuntime>();
        if (runtime == null)
        {
            Debug.LogWarning($"[WaveManager] Prefab '{def.prefab.name}' missing IEnemyRuntime.");
            return;
        }

        def.ApplyToRuntime(currentWaveContext, runtime);
        runtime.SetTarget(tower);
        var enemyCmp = go.GetComponent<Enemy>();
        if (enemyCmp) enemyCmp.SetDefinitionId(def.id);

        // Register spawned enemy with EnemyManager using budgetCost scaled by current health multiplier
        float healthMult = 1f;
        if (!currentWaveContext.Equals(default(WaveContext)))
            healthMult = currentWaveContext.healthMult;
        float threatValue = def.budgetCost * healthMult;
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(go, threatValue);
    }

    private void SpawnBossNow(EnemySpawner spawner, Tower tower)
    {
        // find boss definitions in the configured list
        var bosses = EnemyRosterBuilder.GetEligibleBosses(currentWave, enemyTypes);

        if (bosses == null || bosses.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No eligible Boss-type EnemyTypeDefinition found for this wave.");
            return;
        }

        var def = bosses[Random.Range(0, bosses.Length)];
        if (!def || def.prefab == null)
        {
            Debug.LogWarning("[WaveManager] Selected boss definition missing prefab.");
            return;
        }

        var go = spawner.SpawnEnemy(def.prefab, tower);
        if (!go) return;

        var runtime = go.GetComponent<IEnemyRuntime>();
        if (runtime == null)
        {
            Debug.LogWarning($"[WaveManager] Boss prefab '{def.prefab.name}' missing IEnemyRuntime.");
            return;
        }

        // Use the same WaveContext as normal enemies so bosses scale identically
        def.ApplyToRuntime(currentWaveContext, runtime);
        runtime.SetTarget(tower);

        var enemyCmp = go.GetComponent<Enemy>();
        if (enemyCmp) enemyCmp.SetDefinitionId(def.id);
    }

    public WaveContext GetCurrentWaveContext()
    {
        int wave = Mathf.Max(1, currentWave);
        var context = new WaveContext
        {
            wave = wave,
            healthMult = roundType ? roundType.GetHealthMultiplier(wave) : 1f,
            speedMult = roundType ? roundType.GetSpeedMultiplier(wave) : 1f,
            damageMult = roundType ? roundType.GetDamageMultiplier(wave) : 1f,
            rewardMult = roundType ? roundType.GetRewardMultiplier(wave) : 1f,
            eliteChance = roundType ? roundType.eliteChanceCurve.Evaluate(wave) : 0f,
            rng = new System.Random(baseSeed + wave)
        };

        return context;
    }

    public void EndAllWaves()
    {
        isWaveActive = false;
        inBreak = false;
        if (loopRoutine != null) { StopCoroutine(loopRoutine); loopRoutine = null; }
        if (subscribedTower != null)
        {
            subscribedTower.TowerDestroyed -= EndAllWaves;
            subscribedTower = null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDisable()
    {
        EndAllWaves();
        if (Instance == this) Instance = null;
    }

    public EnemyTypeDefinition[] EnemyDefinitions => enemyTypes;

    // Add or update helper APIs for threat to include active enemies
    public float GetRemainingScheduledBudget()
    {
        if (schedule == null || schedule.Count == 0) return 0f;
        float remaining = 0f;
        for (int i = scheduleIndex; i < schedule.Count; i++)
            remaining += Mathf.Max(0f, schedule[i].enemy?.budgetCost ?? 0f);
        return remaining;
    }

    public float GetCurrentWaveBudget() => currentWaveBudget;

    /// <summary>
    /// Returns the current active threat, scheduled (incoming) threat, and total budget baseline.
    /// </summary>
    public void GetThreatSnapshot(out float activeThreat, out float scheduledThreat, out float totalBudget)
    {
        activeThreat = EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveThreat() : 0f;
        scheduledThreat = GetRemainingScheduledBudget();
        totalBudget = Mathf.Max(0.0001f, currentWaveBudget);

        // If budget is smaller than the sum (e.g. promotions), clamp up so UI stays 0..100%
        float combined = activeThreat + scheduledThreat;
        if (combined > totalBudget) totalBudget = combined;
    }

    public float GetThreatNormalized() => GetThreatRemainingNormalized();

    public float GetThreatRemainingNormalized()
    {
        float total = currentWaveBudget;
        if (total <= 0f) return 0f;
        float active = EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveThreat() : 0f;
        float scheduled = GetRemainingScheduledBudget();
        return Mathf.Clamp01((active + scheduled) / total);
    }

    public float GetActiveThreatNormalized()
    {
        float total = currentWaveBudget;
        if (total <= 0f) return 0f;
        float active = EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveThreat() : 0f;
        return Mathf.Clamp01(active / total);
    }

    public float GetActiveThreatAbsolute() =>
        EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveThreat() : 0f;

    public float GetRemainingThreatAbsolute() =>
        GetRemainingScheduledBudget() + GetActiveThreatAbsolute();
}
