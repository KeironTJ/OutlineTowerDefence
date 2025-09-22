using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SkillData
{
    public string skillName;
    public float level;
    public float researchLevel;
    public bool skillActive;
    public bool playerSkillUnlocked;
    public bool roundSkillUnlocked;
}

[Serializable]
public class ObjectiveProgressData
{
    public string objectiveId;
    public float currentProgress;
    public bool completed;
    public bool claimed;
    public string assignedAtIsoUtc;
}

[Serializable]
public class PlayerData
{
    [Header("Player Information")]
    public string UUID;
    public string Username;

    [Header("Tower Visuals")]
    public List<string> unlockedTowerVisuals = new List<string>();
    public string selectedTowerVisualId = "0001";

    [Header("Turrets")]
    public List<string> unlockedTurretIds = new List<string>();
    public List<string> selectedTurretIds = new List<string> { "", "", "", "" };

    [Header("Skills")]
    public List<PersistentSkillState> skillStates = new List<PersistentSkillState>();

    [Header("Currency (Persistent Balances)")]
    public float cores;
    public float prisms;
    public float loops;

    [Header("Currency Lifetime Totals")]
    public float totalFragmentsEarned;
    public float totalCoresEarned;
    public float totalPrismsEarned;
    public float totalLoopsEarned;

    public float totalFragmentsSpent;
    public float totalCoresSpent;
    public float totalPrismsSpent;
    public float totalLoopsSpent;

    [Header("Progress")]
    public int maxDifficultyAchieved;
    public int[] difficultyMaxWaveAchieved = new int[9];

    [Header("Enemy Kills (By Definition)")]
    public List<EnemyKillEntry> enemyKills = new List<EnemyKillEntry>(); // replaces EnemiesDestroyed

    [Header("Round History")]
    public int totalRoundsCompleted;
    public int totalWavesCompleted;
    public List<RoundRecord> RoundHistory = new List<RoundRecord>();

    [Header("Daily Login")]
    public string lastDailyLoginIsoUtc = "";
    public int dailyLoginStreak = 0;

    [Header("Objective Progress")]
    public List<ObjectiveProgressData> dailyObjectives = new List<ObjectiveProgressData>();
    public List<ObjectiveProgressData> weeklyObjectives = new List<ObjectiveProgressData>();
    public string lastDailyObjectiveAddIsoUtc = "";
    public string lastDailyObjectiveSlotKey = "";

    [Header("Loadout")]
    public string selectedLoadoutId = "";

    public PlayerData()
    {
        UUID = Guid.NewGuid().ToString();
        Username = UUID;
        unlockedTowerVisuals = new List<string> { "0001", "0002", "0003" };
        selectedTowerVisualId = "0001";
        if (selectedTurretIds == null) selectedTurretIds = new List<string> { "", "", "", "" };
        unlockedTurretIds = new List<string> { "MSB" };
    }
}
