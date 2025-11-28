using System;
using System.Collections.Generic;
using System.Collections; // added for IEnumerator
using System.Globalization;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;

    [Header("Services")]
    [SerializeField] private SkillService skillService;      // Assign in scene (or auto‑find)
    [SerializeField] private SaveManager saveManager;        // Optional explicit reference

    [Header("Difficulty")]
    [SerializeField] private DifficultyProgression difficultyProgression;
    [SerializeField] private string difficultyProgressionResourcePath = "Data/DifficultyProgression";

    [Header("Runtime State")]
    public PlayerData playerData;

    [Header("Defaults")]
    [Tooltip("Default turret id to auto-assign for new players / empty slots")]
    [SerializeField] private string defaultTurretId = "MSB";

    public ICurrencyWallet Wallet { get; private set; }      // Persistent wallet (cores / prisms / loops / total fragments KPI)
    public int difficultySelected;

    private bool initialized;
    private readonly Dictionary<string, int> enemyDestructionCounts = new();

    private Coroutine cloudWaitCoroutine; // added

    public bool IsInitialized => initialized;

    // ---------------- LIFECYCLE ----------------
    private void Awake()
    {
        if (main != this && main != null) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);

        if (!skillService) skillService = SkillService.Instance;
        if (!saveManager) saveManager = SaveManager.main;
        if (!difficultyProgression && !string.IsNullOrEmpty(difficultyProgressionResourcePath))
            difficultyProgression = Resources.Load<DifficultyProgression>(difficultyProgressionResourcePath);

        if (saveManager) saveManager.OnAfterLoad += OnSaveLoaded;
    }

    private void OnDestroy()
    {
        if (saveManager) saveManager.OnAfterLoad -= OnSaveLoaded;
    }

    private void Start()
    {
        // In case save already loaded before Start.
        if (!initialized && saveManager && saveManager.Current != null)
            OnSaveLoaded(saveManager.Current);
    }

    // ---------------- SAVE / LOAD ----------------
    private void OnSaveLoaded(PlayerSavePayload payload)
    {
        if (initialized) return;

        playerData = payload.player ?? (payload.player = new PlayerData());
        ValidatePlayerData();

        if (playerData.unlockedTowerBases == null)
            playerData.unlockedTowerBases = new List<string>();

        // Ensure turret lists exist (migrate older saves)
        if (playerData.unlockedTurretIds == null) playerData.unlockedTurretIds = new List<string>();
        if (playerData.selectedTurretIds == null) playerData.selectedTurretIds = new List<string> { "", "", "", "" };

        // Ensure selected slot count (4)
        while (playerData.selectedTurretIds.Count < 4)
            playerData.selectedTurretIds.Add("");

        // Only auto-assign the default turret to slot 0 (main) if it's empty
        if (playerData.selectedTurretIds.Count > 0 && string.IsNullOrEmpty(playerData.selectedTurretIds[0]))
        {
            playerData.selectedTurretIds[0] = defaultTurretId;
            // ensure default is unlocked so selection is valid
            if (playerData.unlockedTurretIds == null) playerData.unlockedTurretIds = new List<string>();
            if (!playerData.unlockedTurretIds.Contains(defaultTurretId))
                playerData.unlockedTurretIds.Add(defaultTurretId);
        }

        // Ensure list exists (first session)
        if (playerData.skillStates == null)
            playerData.skillStates = new List<PersistentSkillState>();

        // Initialize SkillService persistent layer
        if (!skillService) skillService = SkillService.Instance;
        if (!skillService)
        {
            Debug.LogError("SkillService missing in scene.");
            return;
        }
        skillService.LoadPersistentStates(playerData.skillStates);
        // (Round layer is built by RoundManager at round start; can pre-build if needed)
        // skillService.BuildRoundStates();

        // Init wallet AFTER we have playerData (implement PlayerCurrencyWallet yourself)
        Wallet = new PlayerCurrencyWallet(playerData, QueueSave);
        initialized = true;

        // Persist the populated lists (was empty at first load)
        SavePlayerData();
    }

    private void OnEnable()
    {
        if (SaveManager.main != null)
            SaveManager.main.OnAfterLoad += OnSaveLoaded;

        // start coroutine to wait for cloud adoption completion on the Unity thread
        if (cloudWaitCoroutine == null)
            cloudWaitCoroutine = StartCoroutine(WaitForCloudAdoptionAndResync());

        EventManager.StartListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StartListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));
        EventManager.StartListening(EventNames.WaveCompleted, new Action<object>(OnWaveCompleted));
        EventManager.StartListening(EventNames.RoundEnded, new Action<object>(OnRoundEnded));
    }

    private void OnDisable()
    {
        if (SaveManager.main != null)
            SaveManager.main.OnAfterLoad -= OnSaveLoaded;

        // stop the cloud wait coroutine if running
        if (cloudWaitCoroutine != null)
        {
            StopCoroutine(cloudWaitCoroutine);
            cloudWaitCoroutine = null;
        }

        // Unsubscribe from the EnemyDestroyed event via EventManager
        EventManager.StopListening(EventNames.EnemyDestroyedDefinition, OnEnemyDestroyedDefinition);
        EventManager.StopListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));
        EventManager.StopListening(EventNames.WaveCompleted, new Action<object>(OnWaveCompleted));
        EventManager.StopListening(EventNames.RoundEnded, new Action<object>(OnRoundEnded));
    }

    private IEnumerator WaitForCloudAdoptionAndResync()
    {
        // wait for CloudSyncService to exist
        while (CloudSyncService.main == null)
            yield return null;

        var cloud = CloudSyncService.main;

        // prefer explicit Task if available, otherwise poll the completion flag
        float timeout = 8f;
        float start = Time.unscaledTime;
        while (!(cloud.InitialAdoptCompleted || (cloud.SyncCompleted != null && cloud.SyncCompleted.IsCompleted)) &&
               Time.unscaledTime - start < timeout)
        {
            yield return null;
        }

        // resync on main thread
        ForceResyncFromCurrentSave();

        cloudWaitCoroutine = null;
    }

    private void QueueSave() => SaveManager.main?.QueueImmediateSave();

    private void ValidatePlayerData()
    {
        if (string.IsNullOrEmpty(playerData.UUID))
            playerData.UUID = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(playerData.Username))
            playerData.Username = "Player_" + playerData.UUID[..8];

        EnsureDifficultyArrayCapacity();
        playerData.maxDifficultyAchieved = Mathf.Max(playerData.maxDifficultyAchieved, GetMinDifficultyLevel());
        RefreshDifficultyProgressFromData();
    }

    private void EnsureDifficultyArrayCapacity()
    {
        if (playerData == null) return;
        int desiredLength = Mathf.Max(GetMaxConfiguredDifficultyLevel(), playerData.difficultyMaxWaveAchieved?.Length ?? 0);
        desiredLength = Mathf.Max(desiredLength, GetMinDifficultyLevel());

        if (playerData.difficultyMaxWaveAchieved == null)
        {
            playerData.difficultyMaxWaveAchieved = new int[desiredLength];
            return;
        }

        if (playerData.difficultyMaxWaveAchieved.Length < desiredLength)
        {
            Array.Resize(ref playerData.difficultyMaxWaveAchieved, desiredLength);
        }
    }

    public int GetMinDifficultyLevel() => difficultyProgression ? difficultyProgression.MinLevel : 1;

    public int GetMaxConfiguredDifficultyLevel()
    {
        if (difficultyProgression)
            return Mathf.Max(difficultyProgression.MaxLevel, GetMinDifficultyLevel());

        if (playerData?.difficultyMaxWaveAchieved != null && playerData.difficultyMaxWaveAchieved.Length > 0)
            return playerData.difficultyMaxWaveAchieved.Length;

        return GetMinDifficultyLevel();
    }

    public bool IsDifficultyUnlocked(int level)
    {
        if (playerData == null)
            return level <= GetMinDifficultyLevel();

        if (difficultyProgression)
            return difficultyProgression.IsUnlocked(playerData, level);

        return level <= Mathf.Max(GetMinDifficultyLevel(), playerData.maxDifficultyAchieved);
    }

    public int GetHighestUnlockedDifficulty()
    {
        if (playerData == null)
            return GetMinDifficultyLevel();

        if (difficultyProgression)
            return Mathf.Max(GetMinDifficultyLevel(), difficultyProgression.GetHighestUnlocked(playerData));

        return Mathf.Max(GetMinDifficultyLevel(), playerData.maxDifficultyAchieved);
    }

    public bool CanSelectDifficulty(int level)
    {
        level = Mathf.Clamp(level, GetMinDifficultyLevel(), GetMaxConfiguredDifficultyLevel());
        return IsDifficultyUnlocked(level);
    }

    public int ClampDifficultyToUnlocked(int desired)
    {
        int min = GetMinDifficultyLevel();
        int max = GetMaxConfiguredDifficultyLevel();
        desired = Mathf.Clamp(desired, min, max);

        while (desired > min && !IsDifficultyUnlocked(desired))
            desired--;

        return Mathf.Max(desired, min);
    }

    private void RefreshDifficultyProgressFromData(bool clampSelection = true)
    {
        if (playerData == null) return;

        int highestUnlocked = Mathf.Max(GetMinDifficultyLevel(), GetHighestUnlockedDifficulty());
        if (highestUnlocked > playerData.maxDifficultyAchieved)
            playerData.maxDifficultyAchieved = highestUnlocked;
        else if (playerData.maxDifficultyAchieved < GetMinDifficultyLevel())
            playerData.maxDifficultyAchieved = GetMinDifficultyLevel();

        if (clampSelection)
        {
            int desired = difficultySelected > 0 ? difficultySelected : playerData.maxDifficultyAchieved;
            difficultySelected = ClampDifficultyToUnlocked(desired);
        }
    }

    // ---------------- SKILL API (Persistent / Meta) ----------------
    // Meta upgrade using persistent currency (e.g. cores)
    public bool TryMetaUpgradeSkill(string skillId, CurrencyType currency = CurrencyType.Cores)
    {
        if (!initialized || skillService == null || Wallet == null) return false;
        bool ok = skillService.TryUpgradePersistent(skillId, currency, Wallet);
        if (ok) SavePlayerData();
        return ok;
    }

    public void SavePlayerData()
    {
        if (!initialized || playerData == null) return;

        // Pull fresh persistent states out of SkillService (levels may have changed)
        if (skillService)
        {
            playerData.skillStates = new List<PersistentSkillState>(skillService.GetPersistentStates());
        }

        QueueSave();
    }



    // ===== Tower Bases =====
    public bool IsTowerBaseUnlocked(string id)
    {
        if (string.IsNullOrEmpty(id) || playerData == null)
            return false;
        return playerData.unlockedTowerBases != null && playerData.unlockedTowerBases.Contains(id);
    }

    public void UnlockTowerBase(string id)
    {
        if (playerData == null)
            return;

        if (playerData.unlockedTowerBases == null)
            playerData.unlockedTowerBases = new List<string>();

        if (!playerData.unlockedTowerBases.Contains(id))
        {
            playerData.unlockedTowerBases.Add(id);
            SavePlayerData();
        }
    }

    public bool SelectTowerBase(string id)
    {
        if (playerData == null)
            return false;

        if (playerData.unlockedTowerBases == null || !playerData.unlockedTowerBases.Contains(id))
            return false;

        playerData.selectedTowerBaseId = id;
        SavePlayerData();
        return true;
    }

    // ===== Currency =====
    public float GetCores() => Wallet?.Get(CurrencyType.Cores) ?? 0f;
    public float GetPrisms() => Wallet?.Get(CurrencyType.Prisms) ?? 0f;
    public float GetLoops() => Wallet?.Get(CurrencyType.Loops) ?? 0f;

    public bool TrySpend(CurrencyType type, float amount) =>
        amount <= 0f || (Wallet != null && Wallet.TrySpend(type, amount));

    public void AddCurrency(float fragments = 0, float cores = 0, float prisms = 0, float loops = 0)
    {
        Wallet?.Add(CurrencyType.Fragments, fragments);
        Wallet?.Add(CurrencyType.Cores, cores);
        Wallet?.Add(CurrencyType.Prisms, prisms);
        Wallet?.Add(CurrencyType.Loops, loops);
    }

    public bool TrySpendCurrency(float cores = 0, float prisms = 0, float loops = 0)
    {
        if (Wallet == null) return false;
        if ((cores > 0 && Wallet.Get(CurrencyType.Cores) < cores) ||
            (prisms > 0 && Wallet.Get(CurrencyType.Prisms) < prisms) ||
            (loops > 0 && Wallet.Get(CurrencyType.Loops) < loops))
            return false;

        if (cores > 0 && !Wallet.TrySpend(CurrencyType.Cores, cores)) return false;
        if (prisms > 0 && !Wallet.TrySpend(CurrencyType.Prisms, prisms)) return false;
        if (loops > 0 && !Wallet.TrySpend(CurrencyType.Loops, loops)) return false;
        return true;
    }

    // ===== Store =====
    public DateTime? GetLastStorePackClaimTime(string packId)
    {
        if (playerData == null || string.IsNullOrEmpty(packId))
            return null;

        playerData.storePackClaims ??= new List<StorePackClaimRecord>();

        var entry = playerData.storePackClaims.Find(e => e != null && e.packId == packId);
        if (entry == null || string.IsNullOrEmpty(entry.lastClaimIsoUtc))
            return null;

        if (DateTime.TryParse(entry.lastClaimIsoUtc, null, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;

        return null;
    }

    public void RecordStorePackClaim(string packId, DateTime timestampUtc)
    {
        if (playerData == null || string.IsNullOrEmpty(packId))
            return;

        playerData.storePackClaims ??= new List<StorePackClaimRecord>();

        var entry = playerData.storePackClaims.Find(e => e != null && e.packId == packId);
        if (entry == null)
        {
            entry = new StorePackClaimRecord { packId = packId };
            playerData.storePackClaims.Add(entry);
        }

        entry.lastClaimIsoUtc = timestampUtc.ToString("o");
        SavePlayerData();
    }

    // ===== Achievement Progress =====
    public List<AchievementProgressData> GetAchievementProgressList()
    {
        if (playerData == null) return null;
        if (playerData.achievementProgress == null)
            playerData.achievementProgress = new List<AchievementProgressData>();
        return playerData.achievementProgress;
    }

    public AchievementProgressData GetAchievementProgress(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return null;
        var list = GetAchievementProgressList();
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry != null && entry.achievementId == achievementId)
                return entry;
        }
        return null;
    }

    public AchievementProgressData GetOrCreateAchievementProgress(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return null;
        var list = GetAchievementProgressList();
        if (list == null) return null;

        var existing = GetAchievementProgress(achievementId);
        if (existing != null) return existing;

        var created = new AchievementProgressData(achievementId);
        list.Add(created);
        SavePlayerData();
        return created;
    }

    public void NotifyAchievementProgressChanged()
    {
        SavePlayerData();
    }

    public void RecordFragmentsSpent(float amount)
    {
        if (amount <= 0) return;
        playerData.totalFragmentsSpent += amount;
        SavePlayerData();
    }

    // ---------------- DIFFICULTY ----------------
    public int GetMaxDifficulty() => Mathf.Max(GetMinDifficultyLevel(), playerData?.maxDifficultyAchieved ?? GetMinDifficultyLevel());

    public void SetDifficulty(int d)
    {
        difficultySelected = ClampDifficultyToUnlocked(d);
    }

    public int GetDifficulty() => Mathf.Max(GetMinDifficultyLevel(), difficultySelected);

    // Return highest difficulty achieved (fixed signature)
    public int GetHighestDifficulty() => GetMaxDifficulty();

    // Compatibility alias: some code expects this name
    public int GetMaxDifficultyAchieved() => GetMaxDifficulty();


    public void IncreaseMaxDifficulty()
    {
        SetMaxDifficultyAchieved(playerData.maxDifficultyAchieved + 1);
    }

    public void SetMaxDifficultyAchieved(int d)
    {
        if (playerData == null) return;
        int clamped = Mathf.Clamp(d, GetMinDifficultyLevel(), GetMaxConfiguredDifficultyLevel());
        if (!IsDifficultyUnlocked(clamped))
        {
            RefreshDifficultyProgressFromData(false);
            return;
        }

        if (clamped > playerData.maxDifficultyAchieved)
        {
            playerData.maxDifficultyAchieved = clamped;
            SavePlayerData();
        }
    }

    public bool SetMaxWaveAchieved(int difficulty, int wave, bool autoSave = true)
    {
        if (playerData == null) return false;
        if (difficulty < GetMinDifficultyLevel()) return false;

        EnsureDifficultyArrayCapacity();

        int index = Mathf.Clamp(difficulty - 1, 0, playerData.difficultyMaxWaveAchieved.Length - 1);
        bool changed = false;

        if (wave > playerData.difficultyMaxWaveAchieved[index])
        {
            playerData.difficultyMaxWaveAchieved[index] = wave;
            changed = true;
        }

        int before = playerData.maxDifficultyAchieved;
        RefreshDifficultyProgressFromData(false);
        if (playerData.maxDifficultyAchieved > before)
            changed = true;

        if (changed && autoSave)
            SavePlayerData();

        return changed;
    }

    public int GetHighestWave(int difficulty)
    {
        if (playerData?.difficultyMaxWaveAchieved == null || playerData.difficultyMaxWaveAchieved.Length == 0)
            return 0;

        int index = Mathf.Clamp(difficulty - 1, 0, playerData.difficultyMaxWaveAchieved.Length - 1);
        return playerData.difficultyMaxWaveAchieved[index];
    }


    public bool UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername)) return false;
        playerData.Username = newUsername.Trim();
        SavePlayerData();
        return true;
    }

    // ===== Enemy tracking =====
    public void IncrementEnemyDestructionCount(string enemyType)
    {
        if (enemyDestructionCounts.ContainsKey(enemyType))
            enemyDestructionCounts[enemyType]++;
        else
            enemyDestructionCounts[enemyType] = 1;
    }

    public int GetEnemyDestructionCount(string enemyType) =>
        enemyDestructionCounts.TryGetValue(enemyType, out var v) ? v : 0;

    private void OnEnemyDestroyedDefinition(object payload)
    {
        if (payload is not EnemyDestroyedDefinitionEvent e) return;
        if (playerData.enemyKills == null) playerData.enemyKills = new();
        var entry = playerData.enemyKills.Find(k => k.definitionId == e.definitionId);
        if (entry == null)
        {
            playerData.enemyKills.Add(new EnemyKillEntry
            {
                definitionId = e.definitionId,
                tier = e.tier,
                family = e.family,
                traits = e.traits,
                count = 1
            });
        }
        else entry.count++;
        SavePlayerData();
    }

    private void OnRoundRecordUpdated(object eventData)
    {
        if (eventData is RoundRecord rr)
        {
            playerData.RoundHistory.Add(rr);

            // Accumulate lifetime stats
            playerData.lifetimeShotsFired += rr.bulletsFired;
            playerData.lifetimeTotalDamage += rr.totalDamageDealt;
            playerData.lifetimeCriticalHits += rr.criticalHits;

            // Accumulate projectile stats
            if (playerData.lifetimeProjectileStats == null)
                playerData.lifetimeProjectileStats = new List<ProjectileUsageSummary>();

            if (rr.projectileUsage != null)
            {
                foreach (var usage in rr.projectileUsage)
                {
                    var existing = playerData.lifetimeProjectileStats.Find(p => p.projectileId == usage.projectileId);
                    if (existing.projectileId == null)
                    {
                        // Not found, add new
                        playerData.lifetimeProjectileStats.Add(new ProjectileUsageSummary
                        {
                            projectileId = usage.projectileId,
                            shotsFired = usage.shotsFired,
                            damageDealt = usage.damageDealt
                        });
                    }
                    else
                    {
                        // Found, update existing
                        int index = playerData.lifetimeProjectileStats.FindIndex(p => p.projectileId == usage.projectileId);
                        if (index >= 0)
                        {
                            var updated = playerData.lifetimeProjectileStats[index];
                            updated.shotsFired += usage.shotsFired;
                            updated.damageDealt += usage.damageDealt;
                            playerData.lifetimeProjectileStats[index] = updated;
                        }
                    }
                }
            }

            // Accumulate turret stats
            if (playerData.lifetimeTurretStats == null)
                playerData.lifetimeTurretStats = new List<TurretUsageSummary>();

            if (rr.turretUsage != null)
            {
                foreach (var usage in rr.turretUsage)
                {
                    var existing = playerData.lifetimeTurretStats.Find(t => t.turretId == usage.turretId);
                    if (existing.turretId == null)
                    {
                        // Not found, add new
                        playerData.lifetimeTurretStats.Add(new TurretUsageSummary
                        {
                            turretId = usage.turretId,
                            shotsFired = usage.shotsFired
                        });
                    }
                    else
                    {
                        // Found, update existing
                        int index = playerData.lifetimeTurretStats.FindIndex(t => t.turretId == usage.turretId);
                        if (index >= 0)
                        {
                            var updated = playerData.lifetimeTurretStats[index];
                            updated.shotsFired += usage.shotsFired;
                            playerData.lifetimeTurretStats[index] = updated;
                        }
                    }
                }
            }

            SavePlayerData();
        }
    }

    public void ForceResyncFromCurrentSave()
    {
        var payload = SaveManager.main?.Current;
        if (payload == null) return;
        // Re-run the same path used when a save is first loaded/adopted.
        OnSaveLoaded(payload);
    }

    public void BackfillPlayerStats(PlayerData data)
    {
        data.totalRoundsCompleted = data.RoundHistory?.Count ?? 0;
        data.totalWavesCompleted = 0;
        if (data.RoundHistory != null)
        {
            foreach (var round in data.RoundHistory)
            {
                data.totalWavesCompleted += round.highestWave; // Adjust field name as needed
            }
        }
    }

    private void OnWaveCompleted(object eventData)
    {
        if (playerData == null) return;

        int difficulty = GetDifficulty();
        int waveNumber = 0;

        switch (eventData)
        {
            case WaveCompletedEvent wce:
                waveNumber = Mathf.Max(0, wce.waveNumber);
                if (wce.difficulty > 0)
                    difficulty = Mathf.Max(GetMinDifficultyLevel(), wce.difficulty);
                break;
            case int i:
                waveNumber = Mathf.Max(0, i);
                break;
            case float f:
                waveNumber = Mathf.Max(0, Mathf.RoundToInt(f));
                break;
            case double d:
                waveNumber = Mathf.Max(0, (int)System.Math.Round(d));
                break;
        }

        int previousMaxDifficulty = playerData.maxDifficultyAchieved;

        if (waveNumber > 0)
        {
            SetMaxWaveAchieved(difficulty, waveNumber, false);
        }

        playerData.totalWavesCompleted++;
        SavePlayerData();

        if (playerData.maxDifficultyAchieved > previousMaxDifficulty)
        {
            int newMax = playerData.maxDifficultyAchieved;
            var payload = new DifficultyAchievedEvent
            {
                difficultyLevel = newMax,
                previousMaxDifficulty = previousMaxDifficulty,
                triggeredDifficulty = difficulty,
                waveNumber = waveNumber,
                highestWaveAtNewDifficulty = GetHighestWave(newMax)
            };

            EventManager.TriggerEvent(EventNames.DifficultyAchieved, payload);
        }
    }

    public void OnRoundStarted()
    {
        if (skillService != null)
            skillService.BuildRoundStates(); // clones persistent levels into round layer
    }

    private void OnRoundEnded(object eventData)
    {
        var svc = skillService != null ? skillService : SkillService.Instance;
        if (svc != null)
        {
            svc.ClearRoundStates();
            skillService = svc;
        }

        playerData.totalRoundsCompleted++;
        SavePlayerData();
    }

    // --- New PlayerManager API for turret unlock / selection management ---
    public bool IsTurretUnlocked(string turretId)
    {
        if (string.IsNullOrEmpty(turretId) || playerData == null) return false;
        return playerData.unlockedTurretIds != null && playerData.unlockedTurretIds.Contains(turretId);
    }

    public void UnlockTurret(string turretId)
    {
        if (string.IsNullOrEmpty(turretId) || playerData == null) return;
        if (playerData.unlockedTurretIds == null) playerData.unlockedTurretIds = new List<string>();
        if (!playerData.unlockedTurretIds.Contains(turretId))
        {
            playerData.unlockedTurretIds.Add(turretId);
            SavePlayerData();
        }
    }

    public void LockTurret(string turretId)
    {
        if (string.IsNullOrEmpty(turretId) || playerData == null || playerData.unlockedTurretIds == null) return;
        if (playerData.unlockedTurretIds.Remove(turretId))
        {
            SavePlayerData();
        }
    }

    // Ensure list has at least 'count' entries
    private void EnsureSelectedSlots(int count)
    {
        if (playerData == null) return;
        if (playerData.selectedTurretIds == null) playerData.selectedTurretIds = new List<string>();
        while (playerData.selectedTurretIds.Count < count) playerData.selectedTurretIds.Add(string.Empty);
    }

    public string GetSelectedTurretForSlot(int index)
    {
        EnsureSelectedSlots(index + 1);
        string id = playerData.selectedTurretIds[index];
        Debug.Log($"GetSelectedTurretForSlot({index}) = {id}");
        return id;
    }

    public void SetSelectedTurretForSlot(int index, string id)
    {
        EnsureSelectedSlots(index + 1);
        playerData.selectedTurretIds[index] = id ?? string.Empty;

        // Optional: auto-unlock if selecting something not yet unlocked
        if (playerData.unlockedTurretIds == null) playerData.unlockedTurretIds = new List<string>();
        if (!string.IsNullOrEmpty(id) && !playerData.unlockedTurretIds.Contains(id))
            playerData.unlockedTurretIds.Add(id);

        SavePlayerData();
        Debug.Log($"SetSelectedTurretForSlot({index}) = {id}");
    }

    // --- Projectile Management API ---
    public bool IsProjectileUnlocked(string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId) || playerData == null) return false;
        return playerData.unlockedProjectileIds != null && playerData.unlockedProjectileIds.Contains(projectileId);
    }

    public void UnlockProjectile(string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId) || playerData == null) return;
        if (playerData.unlockedProjectileIds == null) playerData.unlockedProjectileIds = new List<string>();
        if (!playerData.unlockedProjectileIds.Contains(projectileId))
        {
            playerData.unlockedProjectileIds.Add(projectileId);
            SavePlayerData();
        }
    }

    public void LockProjectile(string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId) || playerData == null || playerData.unlockedProjectileIds == null) return;
        if (playerData.unlockedProjectileIds.Remove(projectileId))
        {
            SavePlayerData();
        }
    }

    public string GetSelectedProjectileForSlot(int slotIndex)
    {
        if (playerData == null || playerData.selectedProjectilesBySlot == null) return string.Empty;

        var assignment = playerData.selectedProjectilesBySlot.Find(x => x.slotIndex == slotIndex);
        return assignment?.projectileId ?? string.Empty;
    }

    public void SetSelectedProjectileForSlot(int slotIndex, string projectileId)
    {
        if (playerData == null) return;
        if (playerData.selectedProjectilesBySlot == null)
            playerData.selectedProjectilesBySlot = new List<ProjectileSlotAssignment>();

        // Validate projectile is unlocked
        if (!string.IsNullOrEmpty(projectileId) && !IsProjectileUnlocked(projectileId))
        {
            Debug.LogWarning($"Cannot assign projectile '{projectileId}' - not unlocked");
            return;
        }

        // Validate projectile type matches turret requirements
        string turretId = GetSelectedTurretForSlot(slotIndex);
        if (!string.IsNullOrEmpty(turretId) && !string.IsNullOrEmpty(projectileId))
        {
            var turretDef = TurretDefinitionManager.Instance?.GetById(turretId);
            var projDef = ProjectileDefinitionManager.Instance?.GetById(projectileId);

            if (turretDef != null && projDef != null)
            {
                if (!turretDef.AcceptsProjectileType(projDef.projectileType))
                {
                    Debug.LogWarning($"Turret '{turretId}' does not accept projectile type '{projDef.projectileType}'");
                    return;
                }
            }
        }

        // Find or create assignment
        var assignment = playerData.selectedProjectilesBySlot.Find(x => x.slotIndex == slotIndex);
        if (assignment != null)
        {
            assignment.projectileId = projectileId ?? string.Empty;
        }
        else
        {
            playerData.selectedProjectilesBySlot.Add(new ProjectileSlotAssignment
            {
                slotIndex = slotIndex,
                projectileId = projectileId ?? string.Empty
            });
        }

        SavePlayerData();
        Debug.Log($"SetSelectedProjectileForSlot({slotIndex}) = {projectileId}");
    }

    // --- Projectile Upgrade Management API ---
    public int GetProjectileUpgradeLevel(string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId) || playerData == null) return 0;
        if (playerData.projectileUpgradeLevels == null) return 0;

        var upgrade = playerData.projectileUpgradeLevels.Find(x => x.projectileId == projectileId);
        return upgrade?.level ?? 0;
    }

    public bool CanUpgradeProjectile(string projectileId, out string reason, out int cost)
    {
        reason = "";
        cost = 0;

        if (string.IsNullOrEmpty(projectileId) || playerData == null)
        {
            reason = "Invalid projectile";
            return false;
        }

        // Check if projectile is unlocked
        if (!IsProjectileUnlocked(projectileId))
        {
            reason = "Projectile not unlocked";
            return false;
        }

        // Get projectile definition
        var projDef = ProjectileDefinitionManager.Instance?.GetById(projectileId);
        if (projDef == null)
        {
            reason = "Projectile definition not found";
            return false;
        }

        // Check current level
        int currentLevel = GetProjectileUpgradeLevel(projectileId);
        if (currentLevel >= projDef.maxUpgradeLevel)
        {
            reason = "Max level reached";
            return false;
        }

        // Calculate cost
        cost = projDef.GetUpgradeCost(currentLevel);

        // Check if player can afford
        if (playerData.prisms < cost)
        {
            reason = $"Need {cost} Prisms";
            return false;
        }

        reason = "Available";
        return true;
    }

    public bool TryUpgradeProjectile(string projectileId, out string failReason)
    {
        failReason = "";

        if (!CanUpgradeProjectile(projectileId, out failReason, out int cost))
            return false;

        // Spend currency
        if (!TrySpend(CurrencyType.Prisms, cost))
        {
            failReason = "Failed to spend currency";
            return false;
        }

        // Initialize list if needed
        if (playerData.projectileUpgradeLevels == null)
            playerData.projectileUpgradeLevels = new List<ProjectileUpgradeLevel>();

        // Find or create upgrade entry
        var upgrade = playerData.projectileUpgradeLevels.Find(x => x.projectileId == projectileId);
        if (upgrade != null)
        {
            upgrade.level++;
        }
        else
        {
            playerData.projectileUpgradeLevels.Add(new ProjectileUpgradeLevel
            {
                projectileId = projectileId,
                level = 1
            });
        }

        SavePlayerData();
        Debug.Log($"Upgraded projectile {projectileId} to level {GetProjectileUpgradeLevel(projectileId)}");
        return true;
    }
    
    public float CalculateTotalShotsFired()
    {
        if (playerData == null || playerData.lifetimeTurretStats == null) return 0f;
        float total = 0f;
        foreach (var turret in playerData.lifetimeTurretStats)
        {
            total += turret.shotsFired;
        }
        return total;
    }

    // --- Chip Management API ---
    public ChipSystemConfig GetChipConfig()
    {
        if (playerData == null)
        {
            playerData = new PlayerData();
        }
        if (playerData.chipConfig == null)
        {
            playerData.chipConfig = new ChipSystemConfig();
        }
        return playerData.chipConfig;
    }
    
    public List<ChipProgressData> GetChipProgress()
    {
        if (playerData == null) return null;
        if (playerData.chipProgress == null)
            playerData.chipProgress = new List<ChipProgressData>();
        return playerData.chipProgress;
    }
    
    public List<ChipSlotData> GetEquippedChips()
    {
        if (playerData == null) return null;
        if (playerData.equippedChips == null)
            playerData.equippedChips = new List<ChipSlotData>();
        return playerData.equippedChips;
    }

    // --- Research Management API ---
    public ResearchSystemConfig GetResearchConfig()
    {
        if (playerData == null)
        {
            playerData = new PlayerData();
        }
        if (playerData.researchConfig == null)
        {
            playerData.researchConfig = new ResearchSystemConfig();
        }
        return playerData.researchConfig;
    }
    
    public List<ResearchProgressData> GetResearchProgress()
    {
        if (playerData == null) return null;
        if (playerData.researchProgress == null)
            playerData.researchProgress = new List<ResearchProgressData>();
        return playerData.researchProgress;
    }

}
