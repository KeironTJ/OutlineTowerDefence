using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;

    [Header("Services")]
    [SerializeField] private SkillService skillService;      // Assign in scene (or auto‑find)
    [SerializeField] private SaveManager saveManager;        // Optional explicit reference

    [Header("Runtime State")]
    public PlayerData playerData;

    public ICurrencyWallet Wallet { get; private set; }      // Persistent wallet (cores / prisms / loops / total fragments KPI)
    public int difficultySelected;

    private bool initialized;
    private readonly Dictionary<string,int> enemyDestructionCounts = new();

    public bool IsInitialized => initialized;

    // ---------------- LIFECYCLE ----------------
    private void Awake()
    {
        if (main != this && main != null) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);

        if (!skillService) skillService = SkillService.Instance;
        if (!saveManager) saveManager = SaveManager.main;

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

        // Ensure turret lists exist (migrate older saves)
        if (playerData.unlockedTurretIds == null) playerData.unlockedTurretIds = new List<string>();
        if (playerData.selectedTurretIds == null) playerData.selectedTurretIds = new List<string> { "", "", "", "" };

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
        EventManager.StartListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));
        EventManager.StartListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));
        EventManager.StartListening(EventNames.WaveCompleted, new Action<object>(OnWaveCompleted));
        EventManager.StartListening(EventNames.RoundEnded, new Action<object>(OnRoundEnded));

    }

    private void OnDisable()
        {
        // Unsubscribe from the EnemyDestroyed event via EventManager
        EventManager.StopListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));
        EventManager.StopListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));
        EventManager.StopListening(EventNames.WaveCompleted, new Action<object>(OnWaveCompleted));
        EventManager.StopListening(EventNames.RoundEnded, new Action<object>(OnRoundEnded));

    }

    private void QueueSave() => SaveManager.main?.QueueImmediateSave();

    private void ValidatePlayerData()
    {
        if (string.IsNullOrEmpty(playerData.UUID))
            playerData.UUID = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(playerData.Username))
            playerData.Username = "Player_" + playerData.UUID[..8];
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



    // ===== Tower Visuals =====
    public void UnlockTowerVisual(string id)
    {
        if (!playerData.unlockedTowerVisuals.Contains(id))
        {
            playerData.unlockedTowerVisuals.Add(id);
            SavePlayerData();
        }
    }

    public bool SelectTowerVisual(string id)
    {
        if (!playerData.unlockedTowerVisuals.Contains(id)) return false;
        playerData.selectedTowerVisualId = id;
        SavePlayerData();
        return true;
    }

    // ===== Currency =====
    public float GetCores() => Wallet?.Get(CurrencyType.Cores) ?? 0f;
    public float GetPrisms() => Wallet?.Get(CurrencyType.Prisms) ?? 0f;
    public float GetLoops()  => Wallet?.Get(CurrencyType.Loops)  ?? 0f;

    public bool TrySpend(CurrencyType type, float amount) =>
        amount <= 0f || (Wallet != null && Wallet.TrySpend(type, amount));

    public void AddCurrency(float fragments=0, float cores=0, float prisms=0, float loops=0)
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

    public void RecordFragmentsSpent(float amount)
    {
        if (amount <= 0) return;
        playerData.totalFragmentsSpent += amount;
        SavePlayerData();
    }

    // ---------------- DIFFICULTY ----------------
    public int  GetMaxDifficulty() => playerData.maxDifficultyAchieved;
    public void SetDifficulty(int d) => difficultySelected = d;
    public int  GetDifficulty() => difficultySelected;

    public void IncreaseMaxDifficulty()
    {
        playerData.maxDifficultyAchieved++;
        SavePlayerData();
    }

    public void SetMaxWaveAchieved(int difficulty, int wave)
    {
        if (difficulty < 0) return;
        if (difficulty >= playerData.difficultyMaxWaveAchieved.Length) return;
        if (wave > playerData.difficultyMaxWaveAchieved[difficulty])
        {
            playerData.difficultyMaxWaveAchieved[difficulty] = wave;
            SavePlayerData();
        }
    }

    public int GetHighestWave(int difficulty) =>
        playerData.difficultyMaxWaveAchieved[difficulty];


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

    private void OnEnemyDestroyed(object eventData)
    {
        if (eventData is EnemyDestroyedEvent ede)
        {
            var existing = playerData.EnemiesDestroyed
                .Find(e => e.EnemyType == ede.type && e.EnemySubtype == ede.subtype);

            if (existing != null) existing.Count++;
            else playerData.EnemiesDestroyed.Add(new SerializableEnemyData
            {
                EnemyType = ede.type,
                EnemySubtype = ede.subtype,
                Count = 1
            });

            SavePlayerData();
        }
    }

    private void OnRoundRecordUpdated(object eventData)
    {
        if (eventData is RoundRecord rr)
        {
            playerData.RoundHistory.Add(rr);
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
        playerData.totalWavesCompleted++;
        SavePlayerData();
    }

    public void OnRoundStarted()
    {
        if (skillService != null)
            skillService.BuildRoundStates(); // clones persistent levels into round layer
    }

    private void OnRoundEnded(object eventData)
    {
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

    public string GetSelectedTurretForSlot(int slotIndex)
    {
        if (playerData == null || playerData.selectedTurretIds == null) return "";
        if (slotIndex < 0 || slotIndex >= playerData.selectedTurretIds.Count) return "";
        return playerData.selectedTurretIds[slotIndex] ?? "";
    }

    public void SetSelectedTurretForSlot(int slotIndex, string turretId)
    {
        if (playerData == null) return;
        if (playerData.selectedTurretIds == null) playerData.selectedTurretIds = new List<string> { "", "", "", "" };

        while (playerData.selectedTurretIds.Count <= slotIndex)
            playerData.selectedTurretIds.Add("");

        playerData.selectedTurretIds[slotIndex] = turretId ?? "";

        // Persist selection immediately
        SavePlayerData();
    }
}
