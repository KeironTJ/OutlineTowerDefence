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
public class PlayerData
{
    public string UUID;
    public string Username;

    public List<SkillData> attackSkills = new List<SkillData>();
    public List<SkillData> defenceSkills = new List<SkillData>();
    public List<SkillData> supportSkills = new List<SkillData>();
    public List<SkillData> specialSkills = new List<SkillData>();

    public float premiumCredits;
    public float specialCredits;
    public float luxuryCredits;

    public int maxDifficultyAchieved;

    public int[] difficultyMaxWaveAchieved = new int[9];

    // Constructor to initialize a new player
    public PlayerData()
    {
        UUID = Guid.NewGuid().ToString(); // Generate a new UUID
        Username = UUID; // Assign the UUID as the default username
    }

}
