using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;

    public int difficultySelected; // Difficulty selected by player

    [Header("Player Currency")]
    public float premiumCredits; // Game Currency
    public float specialCredits; // Special Currency
    public float luxuryCredits; // Paid Currency

    [Header("Player Progress")]
    public int maxDifficultyAchieved; // Max difficulty achieved
    public int[] difficultyMaxWaveAchieved = new int[9]; // Max wave achieved for each difficulty

    [Header("Skills")]
    public Dictionary<string, Skill> attackSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> defenceSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> supportSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> specialSkills = new Dictionary<string, Skill>();

    public SaveLoadManager saveLoadManager; // Make this public to access from StartMenu
    public PlayerData playerData;

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
            premiumCredits = premiumCredits,
            specialCredits = specialCredits,
            luxuryCredits = luxuryCredits,
            maxDifficultyAchieved = maxDifficultyAchieved,

            difficultyMaxWaveAchieved = (int[])difficultyMaxWaveAchieved.Clone(),

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
        premiumCredits = playerData.premiumCredits;
        specialCredits = playerData.specialCredits;
        luxuryCredits = playerData.luxuryCredits;
        maxDifficultyAchieved = playerData.maxDifficultyAchieved;

        Array.Copy(playerData.difficultyMaxWaveAchieved, difficultyMaxWaveAchieved, difficultyMaxWaveAchieved.Length);

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
        playerData.premiumCredits = premiumCredits;
        playerData.specialCredits = specialCredits;
        playerData.luxuryCredits = luxuryCredits;
        playerData.maxDifficultyAchieved = maxDifficultyAchieved;

        Array.Copy(difficultyMaxWaveAchieved, playerData.difficultyMaxWaveAchieved, difficultyMaxWaveAchieved.Length);

        // Only update persistent skill data
        UpdateSkillData(playerData.attackSkills, attackSkills);
        UpdateSkillData(playerData.defenceSkills, defenceSkills);
        UpdateSkillData(playerData.supportSkills, supportSkills);
        UpdateSkillData(playerData.specialSkills, specialSkills);

        //Debug.Log("Player data before saving: " + JsonUtility.ToJson(playerData, true));
        saveLoadManager.SaveData(playerData);
    }

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

    public void IncreasePremiumCredits(float amount)
    {
        premiumCredits += amount;
        playerData.premiumCredits = premiumCredits; // Update playerData
        SavePlayerData();
    }

    public bool SpendPremiumCredits(float amount)
    {
        if (premiumCredits >= amount)
        {
            premiumCredits -= amount;
            playerData.premiumCredits = premiumCredits; // Update playerData
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
        luxuryCredits += amount;
        playerData.luxuryCredits = luxuryCredits; // Update playerData
        SavePlayerData();
    }

    public bool SpendLuxuryCredits(float amount)
    {
        if (luxuryCredits >= amount)
        {
            luxuryCredits -= amount;
            playerData.luxuryCredits = luxuryCredits; // Update playerData
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
        specialCredits += amount;
        playerData.specialCredits = specialCredits; // Update playerData
        SavePlayerData();
    }

    public bool SpendSpecialCredits(float amount)
    {
        if (specialCredits >= amount)
        {
            specialCredits -= amount;
            playerData.specialCredits = specialCredits; // Update playerData
            SavePlayerData();
            return true;
        }
        else
        {
            Debug.Log("Not enough Special Credits");
            return false;
        }
    }

    public int GetMaxDifficulty()
    {
        return maxDifficultyAchieved = playerData.maxDifficultyAchieved;
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
        maxDifficultyAchieved += 1;
        playerData.maxDifficultyAchieved = maxDifficultyAchieved; // Update playerData
        SavePlayerData();
    }

    public void SetMaxWaveAchieved(int difficulty, int wave)
    {
        if (wave > difficultyMaxWaveAchieved[difficulty])
        {
            Debug.Log($"New max wave achieved for difficulty {difficulty}: {wave}");
            difficultyMaxWaveAchieved[difficulty] = wave;
            playerData.difficultyMaxWaveAchieved = difficultyMaxWaveAchieved; // Update playerData
            SavePlayerData();
        }
        else
        {
            Debug.Log($"Wave {wave} is not greater than current max wave {difficultyMaxWaveAchieved[difficulty]} for difficulty {difficulty}");
        }
    }

    public int GetHighestWave(int difficulty)
    {
        Debug.Log($"Max wave achieved for difficulty {difficulty}: {difficultyMaxWaveAchieved[difficulty]}");
        return difficultyMaxWaveAchieved[difficulty];
    }

    public void UpgradeSkill(Skill skill, int levelIncrease)
    {
        if (skill != null)
        {
            skill.level += levelIncrease;
        }
    }

    public float GetPremiumCredits()
    {
        return premiumCredits;
    }
    
    public float GetLuxuryCredits()
    {
        return luxuryCredits;
    }

}
