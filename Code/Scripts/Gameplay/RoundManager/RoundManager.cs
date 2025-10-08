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
    // New tracking (by definition id / tier / family)
    private string roundStartTimeIsoUtc;
    private readonly Dictionary<string, int> enemyKillsByDefinition = new();
    private readonly Dictionary<EnemyTier, int> enemyKillsByTier = new();
    private readonly Dictionary<string, int> enemyKillsByFamily = new(StringComparer.Ordinal);
    
    // Projectile & Turret tracking
    private readonly Dictionary<string, int> shotsByProjectile = new();
    private readonly Dictionary<string, int> shotsByTurret = new();
    private readonly Dictionary<string, float> damageByProjectile = new();
    
    // Damage tracking
    private float totalDamageDealt;
    private int criticalHitsThisRound;
    
    // Tower base tracking
    private string currentTowerBaseId;

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
        var defLookup = BuildDefinitionLookup();
        var killSummaries = RoundDataConverters.ToEnemyKillSummaries(enemyKillsByDefinition, defLookup);
        return new RoundRecord
        {
            id = string.Empty,
            startedAtIsoUtc = string.Empty,
            endedAtIsoUtc = string.Empty,
            durationSeconds = GetRoundLengthInSeconds(),
            difficulty = roundDifficulty,
            highestWave = waveManager ? waveManager.GetCurrentWave() : 0,
            bulletsFired = bulletsFiredThisRound,
            enemiesKilled = enemiesKilledThisRound,
            currencyEarned = RoundDataConverters.ToCurrencyList(currencyEarnedThisRound),
            enemyKills = killSummaries,
            tierKills = RoundDataConverters.AggregateTierKills(killSummaries),
            familyKills = RoundDataConverters.AggregateFamilyKills(killSummaries),
            towerBaseId = currentTowerBaseId,
            turretUsage = RoundDataConverters.ToTurretUsageSummaries(shotsByTurret),
            projectileUsage = RoundDataConverters.ToProjectileUsageSummaries(shotsByProjectile, damageByProjectile),
            totalDamageDealt = totalDamageDealt,
            criticalHits = criticalHitsThisRound
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
        EventManager.StartListening(EventNames.RawEnemyRewardEvent, OnRawEnemyReward);
        EventManager.StartListening(EventNames.BulletFired, OnBulletFired);
        EventManager.StartListening(EventNames.DamageDealt, OnDamageDealt);
        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StartListening(EventNames.NewWaveStarted, OnNewWaveStarted);

    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.RawEnemyRewardEvent, OnRawEnemyReward);
        EventManager.StopListening(EventNames.BulletFired, OnBulletFired);
        EventManager.StopListening(EventNames.DamageDealt, OnDamageDealt);
        EventManager.StopListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
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
            waveManager.StartWaveSystem(enemySpawner, tower);
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
        
        // Capture tower base ID
        currentTowerBaseId = playerManager.playerData.selectedTowerBaseId ?? "UNKNOWN";

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

        // Track round start time (ISO 8601 format)
        roundStartTimeIsoUtc = DateTime.UtcNow.ToString("o");
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

        var defLookup = BuildDefinitionLookup();

        var killSummaries = RoundDataConverters.ToEnemyKillSummaries(enemyKillsByDefinition, defLookup);

        var record = new RoundRecord
        {
            id = Guid.NewGuid().ToString(),
            startedAtIsoUtc = roundStartTimeIsoUtc,
            endedAtIsoUtc = DateTime.UtcNow.ToString("o"),
            durationSeconds = Time.time - roundStartTime,
            difficulty = roundDifficulty,
            highestWave = waveManager.GetCurrentWave(),
            bulletsFired = bulletsFiredThisRound,
            enemiesKilled = enemiesKilledThisRound,
            currencyEarned = RoundDataConverters.ToCurrencyList(currencyEarnedThisRound),
            enemyKills = killSummaries,
            tierKills = RoundDataConverters.AggregateTierKills(killSummaries),
            familyKills = RoundDataConverters.AggregateFamilyKills(killSummaries),
            towerBaseId = currentTowerBaseId,
            turretUsage = RoundDataConverters.ToTurretUsageSummaries(shotsByTurret),
            projectileUsage = RoundDataConverters.ToProjectileUsageSummaries(shotsByProjectile, damageByProjectile),
            totalDamageDealt = totalDamageDealt,
            criticalHits = criticalHitsThisRound
        };

        roundWallet?.ClearRound();

        EventManager.TriggerEvent(EventNames.RoundEnded, lastRoundSummary);
        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
        EventManager.TriggerEvent(EventNames.RoundRecordCreated, record);
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

        enemyKillsByDefinition.Clear();
        enemyKillsByTier.Clear();
        enemyKillsByFamily.Clear();
        
        shotsByProjectile.Clear();
        shotsByTurret.Clear();
        damageByProjectile.Clear();
        
        totalDamageDealt = 0f;
        criticalHitsThisRound = 0;

        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }

    private void OnBulletFired(object eventData)
    {
        bulletsFiredThisRound++;
        
        // Track by projectile type and turret type
        if (eventData is BulletFiredEvent bfe)
        {
            // Track shots by projectile
            if (!string.IsNullOrEmpty(bfe.projectileId))
            {
                shotsByProjectile[bfe.projectileId] =
                    shotsByProjectile.TryGetValue(bfe.projectileId, out var count) ? count + 1 : 1;
            }

            // Track shots by turret
            if (!string.IsNullOrEmpty(bfe.turretId))
            {
                shotsByTurret[bfe.turretId] =
                    shotsByTurret.TryGetValue(bfe.turretId, out var tcount) ? tcount + 1 : 1;
            }
        }
        
        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }
    
    private void OnDamageDealt(object eventData)
    {
        if (eventData is DamageDealtEvent dde)
        {
            totalDamageDealt += dde.damageAmount;
            
            if (dde.wasCritical)
                criticalHitsThisRound++;
            
            // Track damage by projectile type
            if (!string.IsNullOrEmpty(dde.projectileId))
            {
                damageByProjectile[dde.projectileId] = 
                    damageByProjectile.TryGetValue(dde.projectileId, out var dmg) ? dmg + dde.damageAmount : dde.damageAmount;
            }
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

    private void OnNewWaveStarted(object _)
    {
        // Optionally update highestWave or UI
        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }

    private void OnEnemyDestroyedDefinition(object payload)
    {
        if (payload is not EnemyDestroyedDefinitionEvent e) return;
        enemiesKilledThisRound++;

        // per definition
        enemyKillsByDefinition[e.definitionId] =
            enemyKillsByDefinition.TryGetValue(e.definitionId, out var c) ? c + 1 : 1;

        // per tier
        enemyKillsByTier[e.tier] =
            enemyKillsByTier.TryGetValue(e.tier, out var tcount) ? tcount + 1 : 1;

        // per family
        var famKey = e.family ?? "Unknown";
        enemyKillsByFamily[famKey] =
            enemyKillsByFamily.TryGetValue(famKey, out var fcount) ? fcount + 1 : 1;

        EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
    }

    // Helper to build lookup for summaries (add anywhere inside class):
    private Dictionary<string, EnemyTypeDefinition> BuildDefinitionLookup()
    {
        var dict = new Dictionary<string, EnemyTypeDefinition>();
        if (waveManager)
        {
            foreach (var d in waveManager.EnemyDefinitions)
                if (d && !string.IsNullOrEmpty(d.id) && !dict.ContainsKey(d.id))
                    dict.Add(d.id, d);
        }
        return dict;
    }

    public IReadOnlyDictionary<string, int> GetEnemyKillsByDefinition() => enemyKillsByDefinition;

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
    private void OnRawEnemyReward(object eventData)
    {
        if (eventData is RawEnemyRewardEvent rawEnemyReward)
        {
            float finalFragments = rawEnemyReward.fragments; // apply modifiers below
            float finalCores = rawEnemyReward.cores;   // no modifiers for now
            float finalPrisms = rawEnemyReward.prisms; // no modifiers for now
            float finalLoops = rawEnemyReward.loops;   // no modifiers for now

            AddFragmentsWithModifiers(finalFragments);
            playerManager.Wallet.Add(CurrencyType.Cores, finalCores);
            playerManager.Wallet.Add(CurrencyType.Prisms, finalPrisms);
            playerManager.Wallet.Add(CurrencyType.Loops, finalLoops);

            currencyEarnedThisRound[CurrencyType.Cores] += finalCores;
            currencyEarnedThisRound[CurrencyType.Prisms] += finalPrisms;
            currencyEarnedThisRound[CurrencyType.Loops] += finalLoops;
            
            EventManager.TriggerEvent(EventNames.RoundStatsUpdated, GetCurrentRoundSummary());
            EventManager.TriggerEvent(EventNames.CurrencyEarned, new CurrencyEarnedEvent
            {
                fragments = finalFragments,
                cores = finalCores,
                prisms = finalPrisms,
                loops = finalLoops
            });
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
        float fragmentsSkillMult = Mathf.Max(0f, GetSkillValueSafe(FragmentsModifierSkillId));

        // Difficulty multiplier (1 = no change)
        float difficultyMult = 1f * roundDifficulty;

        // Order: base * (1 + additive) * difficulty * temp
        //Debug.Log($"Fragment Gain Calc: base {baseAmount} * (additive {fragmentsSkillMult}) * diff {difficultyMult} * temp {tempBuffMult}");
        return baseAmount * fragmentsSkillMult * difficultyMult;
        
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
