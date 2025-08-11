using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;

    public int difficultySelected; // Difficulty selected by player

    [Header("Skills")]
    public Dictionary<string, Skill> attackSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> defenceSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> supportSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> specialSkills = new Dictionary<string, Skill>();

    public SaveLoadManager saveLoadManager; // Make this public to access from StartMenu
    public PlayerData playerData;

    // Dictionary to track destroyed enemies by type and subtype
    private Dictionary<string, int> enemyDestructionCounts = new Dictionary<string, int>();

    private void Awake()
    {
        if (main == null)
        {
            main = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        saveLoadManager = GetComponent<SaveLoadManager>();
    }

    private void Start()
    {
        if (saveLoadManager.SaveFileExists())
        {
            InitializeSkills();
            playerData = saveLoadManager.LoadData();
            ValidatePlayerData();
            LoadSkills();
            SavePlayerData();
        }
        else
        {
            InitializeSkills();
            playerData = new PlayerData(); // Ensure playerData is initialized for new players.
            ValidatePlayerData();
            IncreaseMaxDifficulty();
            SavePlayerData();
        }

        // Start the periodic save coroutine
        StartCoroutine(SaveDataPeriodically());
    }

    public void ValidatePlayerData()
    {
        if (playerData == null)
        {
            Debug.LogError("Player data is null. Initializing new player data.");
            playerData = new PlayerData();
        }

        if (string.IsNullOrEmpty(playerData.UUID))
        {
            playerData.UUID = Guid.NewGuid().ToString();
            Debug.Log("Generated new UUID for player.");
        }

        if (string.IsNullOrEmpty(playerData.Username))
        {
            playerData.Username = "Player_" + playerData.UUID.Substring(0, 8);
            Debug.Log("Generated default username for player.");
        }

        SavePlayerData();
    }

    public static T CloneScriptableObject<T>(T original) where T : ScriptableObject
    {
        T clone = ScriptableObject.Instantiate(original);
        return clone;
    }

    private void InitializeSkills()
    {
        playerData = new PlayerData
        {
            attackSkills = new List<SkillData>(),
            defenceSkills = new List<SkillData>(),
            supportSkills = new List<SkillData>(),
            specialSkills = new List<SkillData>()
        };

        LoadSkillsFromResources("Skill/Attack", attackSkills, playerData.attackSkills);
        LoadSkillsFromResources("Skill/Defence", defenceSkills, playerData.defenceSkills);
        LoadSkillsFromResources("Skill/Support", supportSkills, playerData.supportSkills);
        LoadSkillsFromResources("Skill/Special", specialSkills, playerData.specialSkills);
    }

    // SAVE AND LOAD MANAGEMENT

    private void LoadSkillsFromResources(string path, Dictionary<string, Skill> skillDictionary, List<SkillData> skillDataList)
    {
        Skill[] skills = Resources.LoadAll<Skill>(path);
        // Debug.Log($"Loading skills from path: {path}");
        if (skills.Length == 0)
        {
            Debug.LogWarning($"No skills found at path: {path}");
        }
        foreach (Skill skill in skills)
        {
            Skill clonedSkill = CloneScriptableObject(skill);
            skillDictionary.Add(clonedSkill.skillName, clonedSkill);
            skillDataList.Add(new SkillData { skillName = clonedSkill.skillName, level = 0, researchLevel = 0 });
            //Debug.Log($"Loaded skill: {clonedSkill.skillName} from path: {path}");
        }
    }

    private void LoadSkills()
    {
        LoadSkillData(playerData.attackSkills, attackSkills);
        LoadSkillData(playerData.defenceSkills, defenceSkills);
        LoadSkillData(playerData.supportSkills, supportSkills);
        LoadSkillData(playerData.specialSkills, specialSkills);
    }

    private void LoadSkillData(List<SkillData> skillDataList, Dictionary<string, Skill> skillDictionary)
    {
        foreach (SkillData skillData in skillDataList)
        {
            if (skillDictionary.TryGetValue(skillData.skillName, out Skill skill))
            {
                skill.level = skillData.level;
                skill.researchLevel = skillData.researchLevel;
                skill.skillActive = skillData.skillActive;
                skill.playerSkillUnlocked = skillData.playerSkillUnlocked;
                skill.roundSkillUnlocked = skillData.roundSkillUnlocked;
                //Debug.Log($"Loaded skill: {skill.skillName}, Level: {skill.level}, Research Level: {skill.researchLevel}, Skill Active: {skill.skillActive}, Skill Unlocked: {skill.isUnlocked}");
            }
            else
            {
                Debug.LogWarning($"Skill not found in dictionary: {skillData.skillName}");
            }
        }
    }

    private IEnumerator SaveDataPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(10); // Save every 10 seconds
            SavePlayerData();
        }
    }

    private void SavePlayerData()
    {
        // Only update persistent skill data
        UpdateSkillData(playerData.attackSkills, attackSkills);
        UpdateSkillData(playerData.defenceSkills, defenceSkills);
        UpdateSkillData(playerData.supportSkills, supportSkills);
        UpdateSkillData(playerData.specialSkills, specialSkills);

        

        //Debug.Log("Player data before saving: " + JsonUtility.ToJson(playerData, true));
        saveLoadManager.SaveData(playerData);
    }

    // TOWER VISUALS MANAGEMENT
    public void UnlockTowerVisual(string visualId)
    {
        if (!playerData.unlockedTowerVisuals.Contains(visualId))
        {
            playerData.unlockedTowerVisuals.Add(visualId);
            SavePlayerData();
            Debug.Log($"Unlocked tower visual: {visualId}");
        }
    }

    public bool SelectTowerVisual(string visualId)
    {
        if (playerData.unlockedTowerVisuals.Contains(visualId))
        {
            playerData.selectedTowerVisualId = visualId;
            SavePlayerData();
            Debug.Log($"Selected tower visual: {visualId}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Tower visual {visualId} is not unlocked.");
            return false;
        }
    }

    // SKILLS MANAGEMENT

    private void UpdateSkillData(List<SkillData> skillDataList, Dictionary<string, Skill> skillDictionary)
    {
        foreach (var kvp in skillDictionary)
        {
            var skillData = skillDataList.Find(s => s.skillName == kvp.Key);
            if (skillData != null)
            {
                skillData.level = kvp.Value.level;
                skillData.researchLevel = kvp.Value.researchLevel;
                skillData.skillActive = kvp.Value.skillActive;
                skillData.playerSkillUnlocked = kvp.Value.playerSkillUnlocked;
                skillData.roundSkillUnlocked = kvp.Value.roundSkillUnlocked;
            }
        }
    }

    public Skill GetSkill(string skillName)
    {
        if (attackSkills.TryGetValue(skillName, out Skill attackSkill))
        {
            return attackSkill;
        }
        if (defenceSkills.TryGetValue(skillName, out Skill defenceSkill))
        {
            return defenceSkill;
        }
        if (supportSkills.TryGetValue(skillName, out Skill supportSkill))
        {
            return supportSkill;
        }
        if (specialSkills.TryGetValue(skillName, out Skill specialSkill))
        {
            return specialSkill;
        }

        // Debug log to check if skill is not found
        // Debug.LogWarning($"Skill not found: {skillName}");
        return null;
    }

    public float GetSkillValue(Skill skill)
    {
        return skill.baseValue * Mathf.Pow(skill.level, skill.upgradeModifier + (skill.researchLevel * skill.researchModifier));
    }

    public float GetSkillCost(Skill skill)
    {
        float cost = skill.premiumCost * (float)Math.Pow(skill.level, skill.premiumCostModifier);
        return Mathf.Round(cost);
    }

    public float GetSkillLevel(Skill skill)
    {
        return skill.level;
    }


    // CURRENCY MANAGEMENT

    public void IncreasePremiumCredits(float amount)
    {
        playerData.premiumCredits += amount; // Update playerData
        SavePlayerData();
    }

    public bool SpendPremiumCredits(float amount)
    {
        if (playerData.premiumCredits >= amount)
        {
            playerData.premiumCredits -= amount;
            SavePlayerData();
            return true;
        }
        else
        {
            Debug.Log("Not enough Premium Credits");
            return false;
        }
    }

    public void IncreaseLuxuryCredits(float amount)
    {
        playerData.luxuryCredits += amount;
        SavePlayerData();
    }

    public bool SpendLuxuryCredits(float amount)
    {
        if (playerData.luxuryCredits >= amount)
        {
            playerData.luxuryCredits -= amount;
            SavePlayerData();
            return true;
        }
        else
        {
            Debug.Log("Not enough Luxury Credits");
            return false;
        }
    }

    public void IncreaseSpecialCredits(float amount)
    {
        playerData.specialCredits += amount;
        SavePlayerData();
    }

    public bool SpendSpecialCredits(float amount)
    {
        if (playerData.specialCredits >= amount)
        {
            playerData.specialCredits -= amount;
            SavePlayerData();
            return true;
        }
        else
        {
            Debug.Log("Not enough Special Credits");
            return false;
        }
    }

    public float GetPremiumCredits()
    {
        return playerData.premiumCredits;
    }

    public float GetLuxuryCredits()
    {
        return playerData.luxuryCredits;
    }

    public float GetSpecialCredits()
    {
        return playerData.specialCredits;
    }

    // DIFFICULTY MANAGEMENT

    public int GetMaxDifficulty()
    {
        return playerData.maxDifficultyAchieved;
    }

    public void SetDifficulty(int difficulty)
    {
        difficultySelected = difficulty;
    }

    public int GetDifficulty()
    {
        return difficultySelected;
    }

    public void IncreaseMaxDifficulty()
    {
        playerData.maxDifficultyAchieved += 1;
        SavePlayerData();
    }

    public void SetMaxWaveAchieved(int difficulty, int wave)
    {
        if (wave > playerData.difficultyMaxWaveAchieved[difficulty])
        {
            Debug.Log($"New max wave achieved for difficulty {difficulty}: {wave}");
            playerData.difficultyMaxWaveAchieved[difficulty] = wave;
            SavePlayerData();
        }
        else
        {
            Debug.Log($"Wave {wave} is not greater than current max wave {playerData.difficultyMaxWaveAchieved[difficulty]} for difficulty {difficulty}");
        }
    }

    public int GetHighestWave(int difficulty)
    {
        Debug.Log($"Max wave achieved for difficulty {difficulty}: {playerData.difficultyMaxWaveAchieved[difficulty]}");
        return playerData.difficultyMaxWaveAchieved[difficulty];
    }

    public void UpgradeSkill(Skill skill, int levelIncrease)
    {
        if (skill != null)
        {
            skill.level += levelIncrease;
        }
    }

    public bool UpdateUsername(string newUsername)
    {
        if (string.IsNullOrEmpty(newUsername))
        {
            Debug.LogWarning("New username cannot be empty.");
            return false;
        }

        playerData.Username = newUsername;
        SavePlayerData();
        return true;
    }

    // Method to increment the count of destroyed enemies
    public void IncrementEnemyDestructionCount(string enemyType)
    {
        if (enemyDestructionCounts.ContainsKey(enemyType))
        {
            enemyDestructionCounts[enemyType]++;
        }
        else
        {
            enemyDestructionCounts[enemyType] = 1;
        }
    }

    // Method to get the count of destroyed enemies by type
    public int GetEnemyDestructionCount(string enemyType)
    {
        return enemyDestructionCounts.ContainsKey(enemyType) ? enemyDestructionCounts[enemyType] : 0;
    }

    private void OnEnable()
    {
        // Subscribe to the EnemyDestroyed event via EventManager
        EventManager.StartListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));

        //Subscribe to Currency events
        // TODO: Add listener
    }

    private void OnDisable()
    {
        // Unsubscribe from the EnemyDestroyed event via EventManager
        EventManager.StopListening(EventNames.EnemyDestroyed, new Action<object>(OnEnemyDestroyed));
    }

    private void OnEnemyDestroyed(object eventData)
    {
        if (eventData is Enemy enemy)
        {
            EnemyType enemyType = enemy.Type;
            EnemySubtype enemySubtype = enemy.Subtype;

            // Find the existing entry in the list
            var existingEntry = playerData.EnemiesDestroyed.Find(e => e.EnemyType == enemyType && e.EnemySubtype == enemySubtype);

            if (existingEntry != null)
            {
                // Increment the count if the entry exists
                existingEntry.Count++;
            }
            else
            {
                // Add a new entry if it doesn't exist
                playerData.EnemiesDestroyed.Add(new SerializableEnemyData
                {
                    EnemyType = enemyType,
                    EnemySubtype = enemySubtype,
                    Count = 1
                });
            }

            SavePlayerData();
        }
    }
}
