using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;

    public ICurrencyWallet Wallet { get; private set; }
    public int difficultySelected;

    [Header("Skills")]
    public Dictionary<string, Skill> attackSkills  = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> defenceSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> supportSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> specialSkills = new Dictionary<string, Skill>();

    public PlayerData playerData; 

    private Dictionary<string, int> enemyDestructionCounts = new Dictionary<string, int>();
    private bool initializedFromSave;

    public bool IsInitialized => initializedFromSave;

    private void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);
        if (SaveManager.main != null)
            SaveManager.main.OnAfterLoad += OnSaveLoaded;
    }

    private void OnDestroy()
    {
        if (SaveManager.main != null)
            SaveManager.main.OnAfterLoad -= OnSaveLoaded;
    }

    private void Start()
    {
        // If SaveManager already has data (because it loaded in Awake) initialize now
        if (!initializedFromSave && SaveManager.main != null && SaveManager.main.Current != null)
            OnSaveLoaded(SaveManager.main.Current);
    }

    private void OnSaveLoaded(PlayerSavePayload payload)
    {
        if (initializedFromSave) return; // only load once per session
        if (payload == null) return;

        playerData = payload.player ?? (payload.player = new PlayerData());
        ValidatePlayerData();

        LoadSkillAssetsIntoDictionaries();               // fill dictionaries
        LogSkillCounts("After Resources.LoadAll");
        EnsurePlayerDataSkillListsFromDictionaries();    // push dict -> lists (creates entries)
        LoadSkills();                                    // pull list -> dict values (levels, flags)

        Wallet = new PlayerCurrencyWallet(playerData, QueueSave);
        initializedFromSave = true;

        // Persist the populated lists (was empty at first load)
        SavePlayerData();
    }

    private void OnEnable()
    {
        // Subscribe to Events
        EventManager.StartListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));
        EventManager.StartListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));

    }

    private void OnDisable()
    {
        // Unsubscribe from the EnemyDestroyed event via EventManager
        EventManager.StopListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));
        EventManager.StopListening(EventNames.RoundRecordCreated, new Action<object>(OnRoundRecordUpdated));

    }

    private void LogSkillCounts(string label)
    {
        Debug.Log($"PlayerManager {label}: atk={attackSkills.Count} def={defenceSkills.Count} sup={supportSkills.Count} spc={specialSkills.Count} listAtk={playerData.attackSkills.Count}");
    }

    public void ValidatePlayerData()
    {
        if (playerData == null) playerData = new PlayerData();

        if (string.IsNullOrEmpty(playerData.UUID))
            playerData.UUID = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(playerData.Username))
            playerData.Username = "Player_" + playerData.UUID.Substring(0, 8);
    }

    private void QueueSave() => SaveManager.main?.QueueImmediateSave();

    // ===== Skills (unchanged logic) =====

    public static T CloneScriptableObject<T>(T original) where T : ScriptableObject =>
        ScriptableObject.Instantiate(original);

    private void LoadSkillAssetsIntoDictionaries()
    {
        attackSkills.Clear(); defenceSkills.Clear(); supportSkills.Clear(); specialSkills.Clear();
        LoadSkillsFromResources("Skill/Attack",  attackSkills);
        LoadSkillsFromResources("Skill/Defence", defenceSkills);
        LoadSkillsFromResources("Skill/Support", supportSkills);
        LoadSkillsFromResources("Skill/Special", specialSkills);
    }

    private void EnsurePlayerDataSkillListsFromDictionaries()
    {
        EnsureListEntries(playerData.attackSkills,  attackSkills);
        EnsureListEntries(playerData.defenceSkills, defenceSkills);
        EnsureListEntries(playerData.supportSkills, supportSkills);
        EnsureListEntries(playerData.specialSkills, specialSkills);
    }

    private static void EnsureListEntries(List<SkillData> list, Dictionary<string, Skill> dict)
    {
        if (list == null) return;
        foreach (var kv in dict)
        {
            if (list.Exists(s => s.skillName == kv.Key)) continue;
            list.Add(new SkillData {
                skillName = kv.Key,
                level = kv.Value.level,
                researchLevel = kv.Value.researchLevel,
                skillActive = kv.Value.skillActive,
                playerSkillUnlocked = kv.Value.playerSkillUnlocked,
                roundSkillUnlocked = kv.Value.roundSkillUnlocked
            });
        }
    }

    private void LoadSkillsFromResources(string path, Dictionary<string, Skill> dict)
    {
        var skills = Resources.LoadAll<Skill>(path);
        Debug.Log($"LoadSkillsFromResources path={path} count={skills.Length}");
        foreach (var s in skills)
        {
            var clone = CloneScriptableObject(s);
            if (!dict.ContainsKey(clone.skillName))
                dict.Add(clone.skillName, clone);
        }
    }

    private void LoadSkills()
    {
        LoadSkillData(playerData.attackSkills,  attackSkills);
        LoadSkillData(playerData.defenceSkills, defenceSkills);
        LoadSkillData(playerData.supportSkills, supportSkills);
        LoadSkillData(playerData.specialSkills, specialSkills);
    }

    private void LoadSkillData(List<SkillData> list, Dictionary<string, Skill> dict)
    {
        foreach (var sd in list)
        {
            if (dict.TryGetValue(sd.skillName, out var skill))
            {
                skill.level = sd.level;
                skill.researchLevel = sd.researchLevel;
                skill.skillActive = sd.skillActive;
                skill.playerSkillUnlocked = sd.playerSkillUnlocked;
                skill.roundSkillUnlocked = sd.roundSkillUnlocked;
            }
        }
    }

    private void SavePlayerData()
    {
        if (playerData == null) return;
        UpdateSkillData(playerData.attackSkills,  attackSkills);
        UpdateSkillData(playerData.defenceSkills, defenceSkills);
        UpdateSkillData(playerData.supportSkills, supportSkills);
        UpdateSkillData(playerData.specialSkills, specialSkills);
        QueueSave();
    }

    private void UpdateSkillData(List<SkillData> list, Dictionary<string, Skill> dict)
    {
        foreach (var kv in dict)
        {
            var sd = list.Find(s => s.skillName == kv.Key);
            if (sd == null)
            {
                sd = new SkillData { skillName = kv.Key };
                list.Add(sd);
            }
            sd.level = kv.Value.level;
            sd.researchLevel = kv.Value.researchLevel;
            sd.skillActive = kv.Value.skillActive;
            sd.playerSkillUnlocked = kv.Value.playerSkillUnlocked;
            sd.roundSkillUnlocked = kv.Value.roundSkillUnlocked;
        }
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

    // ===== Skill queries =====
    public Skill GetSkill(string name)
    {
        if (attackSkills.TryGetValue(name, out var s1)) return s1;
        if (defenceSkills.TryGetValue(name, out var s2)) return s2;
        if (supportSkills.TryGetValue(name, out var s3)) return s3;
        if (specialSkills.TryGetValue(name, out var s4)) return s4;
        return null;
    }

    public float GetSkillValue(Skill s) =>
        s == null ? 0f : s.baseValue * Mathf.Pow(s.level, s.upgradeModifier + (s.researchLevel * s.researchModifier));

    public float GetSkillCost(Skill s) =>
        s == null ? 0f : Mathf.Round(s.coresCost * (float)Math.Pow(s.level, s.coresCostModifier));

    public float GetSkillLevel(Skill s) => s?.level ?? 0f;

    // ===== Currency =====
    public float GetCores() => Wallet?.Get(CurrencyType.Cores) ?? 0f;
    public float GetPrisms() => Wallet?.Get(CurrencyType.Prisms) ?? 0f;
    public float GetLoops()  => Wallet?.Get(CurrencyType.Loops)  ?? 0f;

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

    public void RecordFragmentsSpent(float amount)
    {
        if (amount <= 0) return;
        playerData.totalFragmentsSpent += amount;
        SavePlayerData();
    }

    // ===== Difficulty =====
    public int GetMaxDifficulty() => playerData.maxDifficultyAchieved;

    public void SetDifficulty(int d) => difficultySelected = d;
    public int  GetDifficulty() => difficultySelected;

    public void IncreaseMaxDifficulty()
    {
        playerData.maxDifficultyAchieved += 1;
        SavePlayerData();
    }

    public void SetMaxWaveAchieved(int difficulty, int wave)
    {
        if (wave > playerData.difficultyMaxWaveAchieved[difficulty])
        {
            playerData.difficultyMaxWaveAchieved[difficulty] = wave;
            SavePlayerData();
        }
    }

    public int GetHighestWave(int difficulty) =>
        playerData.difficultyMaxWaveAchieved[difficulty];

    public void UpgradeSkill(Skill skill, int levelIncrease)
    {
        if (skill == null) return;
        skill.level += levelIncrease;
        SavePlayerData();
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

    private void OnEnemyDestroyed(object eventData)
    {
        if (eventData is EnemyDestroyedEvent ede)
        {
            var existing = playerData.EnemiesDestroyed
                .Find(e => e.EnemyType == ede.type && e.EnemySubtype == ede.subtype);

            if (existing != null) existing.Count++;
            else playerData.EnemiesDestroyed.Add(new SerializableEnemyData {
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
        LogSkillCounts("After Cloud Adopt Resync");
    }
}
