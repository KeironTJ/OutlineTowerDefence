using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "ScriptableObjects/Skill", order = 1)]
public class Skill : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string description;
    public float baseValue; 
    
    [Header("Core Values")]
    public float level;
    public float maxLevel;
    public float upgradeModifier; // Used for exponent

    [Header("Research Values")]
    public float researchLevel;
    public float maxResearchlevel;
    public float researchModifier; // steps upgrade Modifier

    [Header("ROUND Costs")]
    public float basicCost;
    public float basicCostModifier; 

    [Header("PLAYER Costs")]
    public float premiumCost;
    public float premiumCostModifier; 


    [Header("Unlock Costs")] // Used for the initial unlocking of the skill
    public float premiumUnlockCost;
    public float luxuryUnlockCost;
    public float specialUnlockCost;

    [Header("Status")]
    public bool skillActive; // Condition to manage skill activity in game lifecycle. 
    public bool playerSkillUnlocked; // Condition to view skill in Main Shop Menu
    public bool roundSkillUnlocked; // Condition to view skill in Round Shop Menu

    // Other TBC
}
