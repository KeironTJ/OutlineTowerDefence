using System;
using System.Collections.Generic;
using UnityEngine;

// Helper class for serializing projectile slot assignments
[Serializable]
public class ProjectileSlotAssignment
{
    public int slotIndex;
    public string projectileId;
}

// Helper class for tracking projectile upgrade levels
[Serializable]
public class ProjectileUpgradeLevel
{
    public string projectileId;
    public int level;
}

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

    [Header("Tower Bases")]
    public List<string> unlockedTowerBases = new List<string>();
    public string selectedTowerBaseId = "0001";

    [Header("Turrets")]
    public List<string> unlockedTurretIds = new List<string>();
    public List<string> selectedTurretIds = new List<string> { "", "", "", "" };
    
    [Header("Projectiles")]
    public List<string> unlockedProjectileIds = new List<string>();
    // Serializable list instead of Dictionary for Unity compatibility
    public List<ProjectileSlotAssignment> selectedProjectilesBySlot = new List<ProjectileSlotAssignment>();
    // Track upgrade levels for each projectile (projectileId -> level)
    public List<ProjectileUpgradeLevel> projectileUpgradeLevels = new List<ProjectileUpgradeLevel>();

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
    public string lastWeeklyObjectiveSlotKey = "";

    [Header("Loadout")]
    public string selectedLoadoutId = "";

    public PlayerData()
    {
        UUID = Guid.NewGuid().ToString();
        Username = UUID;
        unlockedTowerBases = new List<string> { "0001", "0002", "0003" };
        selectedTowerBaseId = "0001";
        unlockedTurretIds = new List<string> { "STD" };
        if (selectedTurretIds == null) selectedTurretIds = new List<string> { "", "", "", "" };
        selectedTurretIds[0] = "STD";
        
        // Initialize projectile data
        unlockedProjectileIds = new List<string> { "STD_BULLET" };
        selectedProjectilesBySlot = new List<ProjectileSlotAssignment>();
        projectileUpgradeLevels = new List<ProjectileUpgradeLevel>();
    }
}
