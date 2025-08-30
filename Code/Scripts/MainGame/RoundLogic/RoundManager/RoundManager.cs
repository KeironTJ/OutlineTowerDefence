using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner; // Serialized field for EnemySpawner
    [SerializeField] private TowerSpawner towerSpawner; // Serialized field for TowerSpawner
    [SerializeField] private WaveManager waveManager; // Serialized field for WaveManager
    [SerializeField] private UIManager uiManager; // Serialized field for UIManager
    PlayerManager playerManager;
    private Tower tower;

    [Header("Managers")]
    [SerializeField] private SkillManager skillManager; // Reference to SkillManager

    public ICurrencyWallet GetRoundWallet() => roundWallet;

    [Header("Round Wallet")]
    private RoundCurrencyWallet roundWallet;

    // Stats Tracking
    [Header("Stats Tracking")]
    [SerializeField] private int roundDifficulty;
    [SerializeField] private float roundStartTime;
    [SerializeField] private float roundEndTime;
    [SerializeField] private int bulletsFiredThisRound;
    [SerializeField] private int enemiesKilledThisRound;
    private Dictionary<EnemyType, Dictionary<EnemySubtype, int>> enemiesKilledByTypeAndSubtype = new Dictionary<EnemyType, Dictionary<EnemySubtype, int>>();
    private Dictionary<CurrencyType, float> creditsEarnedThisRound = new Dictionary<CurrencyType, float>
    {
        { CurrencyType.Basic, 0f },
        { CurrencyType.Premium, 0f },
        { CurrencyType.Luxury, 0f },
        { CurrencyType.Special, 0f }
    };

    [SerializeField] private RoundSummary lastRoundSummary;

    //Round Summary Accessor
    public struct RoundSummary
    {
        public float durationSeconds;
        public int bulletsFired;
        public Dictionary<CurrencyType, float> creditsEarned;
    }

    public RoundSummary GetCurrentRoundSummary()
    {
        return new RoundSummary
        {
            durationSeconds = Time.time - roundStartTime,
            bulletsFired = bulletsFiredThisRound,
            creditsEarned = new Dictionary<CurrencyType, float>(creditsEarnedThisRound)
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
            creditsEarned = RoundDataConverters.ToCurrencyList(creditsEarnedThisRound),
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
            creditsEarned = RoundDataConverters.ToCurrencyList(creditsEarnedThisRound),
            enemyBreakdown = RoundDataConverters.ToEnemyBreakdown(enemiesKilledByTypeAndSubtype)
        };
    }

    private void Start()
    {
        if (tower == null)
        {
            StartNewRound();
            SpawnTower(); // Use TowerSpawner to spawn the Tower
        }
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.CreditsEarned, OnCreditsEarned);
        EventManager.StartListening(EventNames.BulletFired, OnBulletFired);
        EventManager.StartListening(EventNames.EnemyDestroyed, OnEnemyDestroyed);
        EventManager.StartListening(EventNames.NewWaveStarted, OnNewWaveStarted);

    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.CreditsEarned, OnCreditsEarned);
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
        if (towerSpawner == null)
        {
            Debug.LogError("TowerSpawner is not assigned to RoundManager.");
            return;
        }

        tower = towerSpawner.SpawnTower(); // Spawn the Tower and store the reference
        if (tower != null)
        {
            tower.Initialize(this, enemySpawner, skillManager, uiManager); // Initialize the Tower with SkillManager
            tower.TowerDestroyed += EndRound; // Subscribe to the TowerDestroyed event
            waveManager.StartWave(enemySpawner, tower); // Start the wave using WaveManager
            uiManager.Initialize(this, waveManager, tower, skillManager, playerManager); // Initialize UIManager with necessary references
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

        ResetRoundStats();
        roundStartTime = Time.time;
        roundEndTime = 0f;

        if (playerManager != null)
        {
            roundWallet = new RoundCurrencyWallet(
                playerManager.Wallet,
                amount =>
                {
                    playerManager.RecordBasicSpent(amount);
                }
            );

            InitializeRound(playerManager);
            roundDifficulty = playerManager.GetDifficulty();

        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }

        EventManager.TriggerEvent(EventNames.RoundStarted);
    }

    public void InitializeRound(PlayerManager playerManager)
    {
        InitializeRoundSkills(playerManager);
    }

    private void InitializeRoundSkills(PlayerManager playerManager)
    {
        if (skillManager == null)
        {
            Debug.LogError("SkillManager is not assigned to RoundManager.");
            return;
        }

        skillManager.InitializeSkills(playerManager.attackSkills, playerManager.defenceSkills, playerManager.supportSkills, playerManager.specialSkills);

        SetStartBasicCredits();
    }

    public void EndRound()
    {
        SetHighestWaves();
        DestroyAllEnemies();
        DestroyAllBullets();

        // Final Round Stats:
        roundEndTime = Time.time;
        lastRoundSummary = new RoundSummary
        {
            durationSeconds = GetRoundLengthInSeconds(),
            bulletsFired = bulletsFiredThisRound,
            creditsEarned = new Dictionary<CurrencyType, float>(creditsEarnedThisRound)
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
        creditsEarnedThisRound = new Dictionary<CurrencyType, float>
        {
            [CurrencyType.Basic] = 0f,
            [CurrencyType.Premium] = 0f,
            [CurrencyType.Luxury] = 0f,
            [CurrencyType.Special] = 0f
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

    public float GetCreditsPerMinute(CurrencyType type)
    {
        float secs = GetRoundLengthInSeconds();
        if (secs <= 0.01f) return 0f;
        return (creditsEarnedThisRound.TryGetValue(type, out var amt) ? amt : 0f) * (60f / secs);
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
    public float GetCreditsEarnedThisRound(CurrencyType type) => creditsEarnedThisRound.TryGetValue(type, out var amt) ? amt : 0f;

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

    // CREDITS
    private void OnCreditsEarned(object eventData)
    {
        if (eventData is CreditsEarnedEvent creditsEvent)
        {
            //Debug.Log($"Credits Earned - Basic: {creditsEvent.basic}, Premium: {creditsEvent.premium}, Luxury: {creditsEvent.luxury}, Special: {creditsEvent.special}");
            IncreaseBasicCredits(creditsEvent.basic);
            playerManager.Wallet.Add(CurrencyType.Premium, creditsEvent.premium);
            playerManager.Wallet.Add(CurrencyType.Luxury, creditsEvent.luxury);
            playerManager.Wallet.Add(CurrencyType.Special, creditsEvent.special);

            creditsEarnedThisRound[CurrencyType.Basic] += creditsEvent.basic;
            creditsEarnedThisRound[CurrencyType.Premium] += creditsEvent.premium;
            creditsEarnedThisRound[CurrencyType.Luxury] += creditsEvent.luxury;
            creditsEarnedThisRound[CurrencyType.Special] += creditsEvent.special;
            EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
        }
    }

    private void SetStartBasicCredits()
    {
        float startingBasicCredits = skillManager.GetSkillValue(skillManager.GetSkill("Start Basic Credit"));
        IncreaseBasicCredits(startingBasicCredits);
    }

    public void IncreaseBasicCredits(float amount)
    {
        if (amount == 0f) return;

        // Treat skill as a bonus (0 => x1.0, 0.25 => x1.25)
        float bonus = skillManager.GetSkillValue(skillManager.GetSkill("Basic Credit Modifier"));
        float multiplier = Mathf.Max(0f, 1f + bonus);
        roundWallet?.Add(CurrencyType.Basic, amount * multiplier);
    }

    public bool SpendBasicCredits(float amount)
    {
        return roundWallet?.TrySpend(CurrencyType.Basic, amount) ?? false; 
    }

    public float GetBasicCredits()
    {
        return roundWallet?.Get(CurrencyType.Basic) ?? 0f;
    }


}
