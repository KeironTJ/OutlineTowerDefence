using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SkillData
{
    public string skillName;
    public float level;
    public float researchLevel;
    
    public bool skillActive;
    public bool playerSkillUnlocked;
    public bool roundSkillUnlocked;
}

[System.Serializable]
public class SerializableEnemyData
{
    public EnemyType EnemyType;
    public EnemySubtype EnemySubtype;
    public int Count;
}

[System.Serializable]
public class ObjectiveProgressData
{
    public string objectiveId;
    public float currentProgress;
    public bool completed;
    public bool claimed;                 
    public string assignedAtIsoUtc;
}


/// <summary>
/// Stores all persistent player data, including currency, skills, progress, and history.
/// </summary>
[System.Serializable]
public class PlayerData
{
    [Header("Player Information")]
    public string UUID;
    public string Username;

    [Header("Tower Visuals")]
    public List<string> unlockedTowerVisuals = new List<string>();
    public string selectedTowerVisualId = "0001";

    [Header("Skills")]
    public List<SkillData> attackSkills = new List<SkillData>();
    public List<SkillData> defenceSkills = new List<SkillData>();
    public List<SkillData> supportSkills = new List<SkillData>();
    public List<SkillData> specialSkills = new List<SkillData>();

    public List<PersistentSkillState> skillStates = new List<PersistentSkillState>();

    [Header("Currency Information")]
    // Currency Balances
    public float cores;
    public float prisms;
    public float loops;

    // Currency Lifetime Totals
    public float totalFragmentsEarned;
    public float totalCoresEarned;
    public float totalPrismsEarned;
    public float totalLoopsEarned;

    public float totalFragmentsSpent;
    public float totalCoresSpent;
    public float totalPrismsSpent;
    public float totalLoopsSpent;

    [Header("Game Progress")]
    public int maxDifficultyAchieved;
    public int[] difficultyMaxWaveAchieved = new int[9];

    [Header("Enemy Destruction Data")]
    public List<SerializableEnemyData> EnemiesDestroyed = new List<SerializableEnemyData>();

    [Header("Round History")]
    public int totalRoundsCompleted;
    public int totalWavesCompleted;
    public List<RoundRecord> RoundHistory = new List<RoundRecord>();

    [Header("Daily Login Data")]
    public string lastDailyLoginIsoUtc = "";
    public int dailyLoginStreak = 0; 

    [Header("Objective Progress")]
    public List<ObjectiveProgressData> dailyObjectives = new List<ObjectiveProgressData>();
    public List<ObjectiveProgressData> weeklyObjectives = new List<ObjectiveProgressData>();

    // Timestamp controlling next auto‑slot fill (daily)
    public string lastDailyObjectiveAddIsoUtc = "";   
    public string lastDailyObjectiveSlotKey = ""; //e.g. "20250828-12" (UTC date + slot hour)


    /// <summary>
    /// Constructor to initialize a new player.
    /// </summary>
    public PlayerData()
    {
        UUID = Guid.NewGuid().ToString(); // Generate a new UUID
        Username = UUID; // Assign the UUID as the default username

        // Unlock default tower visuals
        unlockedTowerVisuals = new List<string> { "0001", "0002", "0003" };
        selectedTowerVisualId = "0001";
    }
}
