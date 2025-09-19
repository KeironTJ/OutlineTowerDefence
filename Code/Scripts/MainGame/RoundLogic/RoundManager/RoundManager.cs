using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private TowerSpawner towerSpawner;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private UIManager uiManager;

    private PlayerManager playerManager;
    private Tower tower;

    [Header("Services")]
    [SerializeField] private SkillService skillService; 

    [Header("Round Wallet")]
    private RoundCurrencyWallet roundWallet;
    public ICurrencyWallet RoundWallet => roundWallet;
    public ICurrencyWallet GetRoundWallet() => roundWallet;      // (kept for existing callers)

    // --- Skill Id Constants (match your SkillDefinition ids / names) ---
    private const string StartFragmentsSkillId   = "Start Fragments";
    private const string FragmentsModifierSkillId = "Fragments Modifier";

    [Header("Fragment Gain Settings")]
    [SerializeField] private bool applyModifierToStartingFragments = true;

    // Event UI can subscribe to after wallet & round skill layer ready
    public event Action RoundInitialized;

    // Stats Tracking
    [Header("Stats Tracking")]
    [SerializeField] private int roundDifficulty;
    [SerializeField] private float roundStartTime;
    [SerializeField] private float roundEndTime;
    [SerializeField] private int bulletsFiredThisRound;
    [SerializeField] private int enemiesKilledThisRound;
    private Dictionary<EnemyType, Dictionary<EnemySubtype, int>> enemiesKilledByTypeAndSubtype = new Dictionary<EnemyType, Dictionary<EnemySubtype, int>>();
    private Dictionary<CurrencyType, float> currencyEarnedThisRound = new Dictionary<CurrencyType, float>
    {
        { CurrencyType.Fragments, 0f },
        { CurrencyType.Cores, 0f },
        { CurrencyType.Prisms, 0f },
        { CurrencyType.Loops, 0f }
    };

    [SerializeField] private RoundSummary lastRoundSummary;

    //Round Summary Accessor
    public struct RoundSummary
    {
        public float durationSeconds;
        public int bulletsFired;
        public Dictionary<CurrencyType, float> currencyEarned;
    }

    public RoundSummary GetCurrentRoundSummary()
    {
        return new RoundSummary
        {
            durationSeconds = Time.time - roundStartTime,
            bulletsFired = bulletsFiredThisRound,
            currencyEarned = new Dictionary<CurrencyType, float>(currencyEarnedThisRound)
        };
    }

    public RoundSummary GetLastRoundSummary() => lastRoundSummary;

    public RoundRecord GetLiveRoundRecord()
    {
        return new RoundRecord
        {
            id = string.Empty,
            startedAtIsoUtc = string.Empty,
            endedAtIsoUtc = string.Empty,
            durationSeconds = GetRoundLengthInSeconds(),
            difficulty = roundDifficulty,
            highestWave = waveManager != null ? waveManager.GetCurrentWave() : 0,
            bulletsFired = bulletsFiredThisRound,
            enemiesKilled = enemiesKilledThisRound,
            currencyEarned = RoundDataConverters.ToCurrencyList(currencyEarnedThisRound),
            enemyBreakdown = RoundDataConverters.ToEnemyBreakdown(enemiesKilledByTypeAndSubtype)
        };
    }

    private RoundRecord BuildRoundRecord()
    {
        float duration = GetRoundLengthInSeconds();
        var nowUtc = System.DateTime.UtcNow;
        return new RoundRecord
        {
            id = System.Guid.NewGuid().ToString("N"),
            startedAtIsoUtc = nowUtc.AddSeconds(-duration).ToString("o"),
            endedAtIsoUtc = nowUtc.ToString("o"),
            durationSeconds = duration,
            difficulty = roundDifficulty,
            highestWave = waveManager != null ? waveManager.GetCurrentWave() : 0,
            bulletsFired = bulletsFiredThisRound,
            enemiesKilled = enemiesKilledThisRound,
            currencyEarned = RoundDataConverters.ToCurrencyList(currencyEarnedThisRound),
            enemyBreakdown = RoundDataConverters.ToEnemyBreakdown(enemiesKilledByTypeAndSubtype)
        };
    }

    private void Start()
    {
        if (!skillService) skillService = SkillService.Instance;
        SkillService.Instance.BuildRoundStates();
        
        if (tower == null)
        {
            StartNewRound();
            SpawnTower();
        }
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StartListening(EventNames.BulletFired, OnBulletFired);
        EventManager.StartListening(EventNames.EnemyDestroyed, OnEnemyDestroyed);
        EventManager.StartListening(EventNames.NewWaveStarted, OnNewWaveStarted);

    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.CurrencyEarned, OnCurrencyEarned);
        EventManager.StopListening(EventNames.BulletFired, OnBulletFired);
        EventManager.StopListening(EventNames.EnemyDestroyed, OnEnemyDestroyed);
        EventManager.StopListening(EventNames.NewWaveStarted, OnNewWaveStarted);

        if (tower != null)
        {
            tower.TowerDestroyed -= EndRound; // Unsubscribe from the TowerDestroyed event
        }
    }

    // TOWER MANAGEMENT
    private void SpawnTower()
    {
        if (!towerSpawner)
        {
            Debug.LogError("TowerSpawner is not assigned to RoundManager.");
            return;
        }

        tower = towerSpawner.SpawnTower();
        if (tower != null)
        {
            // Update Tower.Initialize signature to drop skillManager param.
            tower.Initialize(this, enemySpawner, uiManager); 
            tower.TowerDestroyed += EndRound;
            waveManager.StartWave(enemySpawner, tower);
            uiManager.Initialize(this, waveManager, tower, playerManager); // drop skillManager param
        }
        else
        {
            Debug.LogError("Failed to spawn Tower.");
        }
    }

    public void DestroyAllBullets()
    {
        var bullets = UnityEngine.Object.FindObjectsByType<Bullet>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (var b in bullets)
            Destroy(b.gameObject);
    }

    // ROUND MANAGEMENT
    private void StartNewRound()
    {
        playerManager = PlayerManager.main;
        if (!playerManager)
        {
            Debug.LogError("PlayerManager not found!");
            return;
        }

        if (!skillService) skillService = SkillService.Instance;

        ResetRoundStats();
        roundStartTime = Time.time;
        roundEndTime = 0f;

        // Build round skill layer (copies persistent into round state)
        playerManager.OnRoundStarted(); // calls skillService.BuildRoundStates()

        // Create round wallet wrapping persistent wallet
        roundWallet = new RoundCurrencyWallet(
            playerManager.Wallet,
            spent => playerManager.RecordFragmentsSpent(spent)
        );

        roundDifficulty = playerManager.GetDifficulty();

        // Apply starting fragments / bonuses
        ApplyRoundStartBonuses();

        EventManager.TriggerEvent(EventNames.RoundStarted);
        RoundInitialized?.Invoke(); // UI hook point
    }

    private void ApplyRoundStartBonuses()
    {
        SetStartFragments(); // starting fragments
    }

    public void EndRound()
    {
        SetHighestDifficulties();
        SetHighestWaves();
        DestroyAllEnemies();
        DestroyAllBullets();

        // Final Round Stats:
        roundEndTime = Time.time;
        lastRoundSummary = new RoundSummary
        {
            durationSeconds = GetRoundLengthInSeconds(),
            bulletsFired = bulletsFiredThisRound,
            currencyEarned = new Dictionary<CurrencyType, float>(currencyEarnedThisRound)
        };

        var roundRecord = BuildRoundRecord();

        roundWallet?.ClearRound();

        EventManager.TriggerEvent(EventNames.RoundEnded, lastRoundSummary);
        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
        EventManager.TriggerEvent(EventNames.RoundRecordCreated, roundRecord);
    }

    // Stat Management
    private void ResetRoundStats()
    {
        bulletsFiredThisRound = 0;
        enemiesKilledThisRound = 0;
        currencyEarnedThisRound = new Dictionary<CurrencyType, float>
        {
            [CurrencyType.Fragments] = 0f,
            [CurrencyType.Cores] = 0f,
            [CurrencyType.Prisms] = 0f,
            [CurrencyType.Loops] = 0f
        };

        enemiesKilledByTypeAndSubtype.Clear();
        foreach (EnemyType t in System.Enum.GetValues(typeof(EnemyType)))
        {
            enemiesKilledByTypeAndSubtype[t] = new Dictionary<EnemySubtype, int>();
            foreach (EnemySubtype s in System.Enum.GetValues(typeof(EnemySubtype)))
            {
                enemiesKilledByTypeAndSubtype[t][s] = 0;
            }
        }

        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }

    private void OnBulletFired(object eventData)
    {
        if (eventData is Bullet bullet)
        {
            bulletsFiredThisRound++;
            EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
        }
        else
        {
            Debug.LogWarning("BulletFired received unexpected event payload.");
        }
    }

    public float GetRoundLengthInSeconds()
    {
        if (roundStartTime == 0f) return 0f;
        return (roundEndTime > 0f) ? (roundEndTime - roundStartTime) : (Time.time - roundStartTime);
    }

    public float GetCurrencyPerMinute(CurrencyType type)
    {
        float secs = GetRoundLengthInSeconds();
        if (secs <= 0.01f) return 0f;
        return (currencyEarnedThisRound.TryGetValue(type, out var amt) ? amt : 0f) * (60f / secs);
    }

    private void OnEnemyDestroyed(object eventData)
    {
        if (eventData is EnemyDestroyedEvent ede)
        {
            enemiesKilledThisRound++;
            if (!enemiesKilledByTypeAndSubtype.TryGetValue(ede.type, out var subtypeDict))
            {
                subtypeDict = new Dictionary<EnemySubtype, int>();
                enemiesKilledByTypeAndSubtype[ede.type] = subtypeDict;
            }
            subtypeDict[ede.subtype] = subtypeDict.TryGetValue(ede.subtype, out var count) ? count + 1 : 1;
        }

        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }

    public int GetBulletsFiredThisRound() => bulletsFiredThisRound;
    public float GetCurrencyEarnedThisRound(CurrencyType type) => currencyEarnedThisRound.TryGetValue(type, out var amt) ? amt : 0f;

    public int GetEnemiesKilledByType(EnemyType type)
    {
        if (enemiesKilledByTypeAndSubtype.TryGetValue(type, out var subDict))
        {
            int sum = 0;
            foreach (var v in subDict.Values) sum += v;
            return sum;
        }
        return 0;
    }

    public IReadOnlyDictionary<EnemySubtype, int> GetSubtypeCountsForType(EnemyType type)
    {
        if (enemiesKilledByTypeAndSubtype.TryGetValue(type, out var subDict))
            return subDict;
        return new Dictionary<EnemySubtype, int>();
    }

    public List<string> GetTypeSubtypeSummaryLines()
    {
        var lines = new List<string>();
        foreach (EnemyType t in Enum.GetValues(typeof(EnemyType)))
        {
            int tcount = GetEnemiesKilledByType(t);
            // decide: skip types with zero kills or include them — current code skips zero
            if (tcount == 0) continue;

            lines.Add($"Enemy Type: {t}  ({tcount})");
            var subDict = GetSubtypeCountsForType(t);
            foreach (var kv in subDict)
            {
                if (kv.Value > 0)
                    lines.Add($"  SubType: {kv.Key}  ({kv.Value})");
            }
        }
        return lines;
    }

    private void OnNewWaveStarted(object eventData)
    {
        //Placeholder for future implementation
        if (eventData is int waveNumber)
        {
            Debug.Log($"New wave started: {waveNumber}");
            // You can add additional logic here if needed
        }
        else
        {
            Debug.LogWarning("NewWaveStarted received unexpected event payload.");
        }
    }

    // PLAYER MANAGEMENT
    private void SetHighestWaves()
    {
        if (playerManager != null)
        {
            int difficulty = playerManager.GetDifficulty();
            int highestWave = playerManager.GetHighestWave(difficulty);

            playerManager.SetMaxWaveAchieved(difficulty, waveManager.GetCurrentWave());

        }
    }

    private void SetHighestDifficulties()
    {
        if (playerManager != null)
        {
            int maxDifficulty = playerManager.GetMaxDifficultyAchieved();
            if (roundDifficulty > maxDifficulty)
            {
                playerManager.SetMaxDifficultyAchieved(roundDifficulty);
            }
        }
    }

    // Enemies
    private void DestroyAllEnemies()
    {
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    // ROUND DIFFICULTY
    public void SetRoundDifficulty(int difficulty)
    {
        roundDifficulty = difficulty;
    }

    public int GetRoundDifficulty()
    {
        return roundDifficulty;
    }

    // CURRENCY
    private void OnCurrencyEarned(object eventData)
    {
        if (eventData is CurrencyEarnedEvent currencyEarned)
        {
            AddFragmentsWithModifiers(currencyEarned.fragments);
            playerManager.Wallet.Add(CurrencyType.Cores, currencyEarned.cores);
            playerManager.Wallet.Add(CurrencyType.Prisms, currencyEarned.prisms);
            playerManager.Wallet.Add(CurrencyType.Loops, currencyEarned.loops);

            currencyEarnedThisRound[CurrencyType.Fragments] += currencyEarned.fragments;
            currencyEarnedThisRound[CurrencyType.Cores] += currencyEarned.cores;
            currencyEarnedThisRound[CurrencyType.Prisms] += currencyEarned.prisms;
            currencyEarnedThisRound[CurrencyType.Loops] += currencyEarned.loops;
            EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
        }
    }

    private void SetStartFragments()
    {
        float startingFragmentsBase = GetSkillValueSafe(StartFragmentsSkillId);
        if (startingFragmentsBase > 0f)
        {
            if (applyModifierToStartingFragments)
                AddFragmentsWithModifiers(startingFragmentsBase);
            else
                AddRawFragments(startingFragmentsBase);
        }
    }

    public void IncreaseFragments(float amount)
    {
        // KEEP for backward compatibility; assume caller passes BASE amount
        AddFragmentsWithModifiers(amount);
    }

    // Central point: apply all fragment gain modifiers here (skills, difficulty, temporary buffs)
    private void AddFragmentsWithModifiers(float baseAmount)
    {
        if (baseAmount <= 0f) return;

        float final = ComputeFinalFragments(baseAmount);

        AddRawFragments(final);

        // Track earned (final credited amount)
        currencyEarnedThisRound[CurrencyType.Fragments] += final;
    }

    private void AddRawFragments(float amount)
    {
        if (amount <= 0f) return;
        roundWallet?.Add(CurrencyType.Fragments, amount);
    }

    private float ComputeFinalFragments(float baseAmount)
    {
        // Skill: additive percent (e.g. 0.25 => +25%)
        float additivePercent = Mathf.Max(0f, GetSkillValueSafe(FragmentsModifierSkillId));

        // Difficulty multiplier (1 = no change)
        float difficultyMult = 1f * roundDifficulty; 

        // Temporary round buff multiplier
        float tempBuffMult = 1f;   // replace if you add timed boosts

        // Order: base * (1 + additive) * difficulty * temp
        return baseAmount * (1f + additivePercent) * difficultyMult * tempBuffMult;
    }

    public bool SpendFragments(float amount)
    {
        return roundWallet?.TrySpend(CurrencyType.Fragments, amount) ?? false;
    }

    public float GetFragments()
    {
        return roundWallet?.Get(CurrencyType.Fragments) ?? 0f;
    }


    private float GetSkillValueSafe(string skillId)
    {
        if (skillService == null) return 0f;
        return skillService.GetValue(skillId);
    }
}
