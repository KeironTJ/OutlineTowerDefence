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

    [Header("Currency Information")]
    // Credit Balances
    public float premiumCredits;
    public float specialCredits;
    public float luxuryCredits;

    // Credit Lifetime Totals
    public float totalBasicCreditsEarned;
    public float totalPremiumCreditsEarned;
    public float totalSpecialCreditsEarned;
    public float totalLuxuryCreditsEarned;
    public float totalBasicCreditsSpent;
    public float totalPremiumCreditsSpent;
    public float totalSpecialCreditsSpent;
    public float totalLuxuryCreditsSpent;

    [Header("Game Progress")]
    public int maxDifficultyAchieved;
    public int[] difficultyMaxWaveAchieved = new int[9];

    [Header("Enemy Destruction Data")]
    public List<SerializableEnemyData> EnemiesDestroyed = new List<SerializableEnemyData>();

    // Constructor to initialize a new player
    public PlayerData()
    {
        UUID = Guid.NewGuid().ToString(); // Generate a new UUID
        Username = UUID; // Assign the UUID as the default username

        // Unlock default tower visuals
        unlockedTowerVisuals = new List<string> { "0001", "0002", "0003" };
        selectedTowerVisualId = "0001";
    }

}
