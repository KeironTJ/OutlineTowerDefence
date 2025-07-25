using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner; // Serialized field for EnemySpawner
    [SerializeField] private TowerSpawner towerSpawner; // Serialized field for TowerSpawner
    [SerializeField] private WaveManager waveManager; // Serialized field for WaveManager

    [Header("Managers")]
    [SerializeField] private SkillManager skillManager; // Reference to SkillManager

    public float tempBasicCredits = 0;

    private Tower tower;
    public int roundDifficulty;

    PlayerManager playerManager;


    private void Start()
    {
        StartNewRound();
        SpawnTower(); // Use TowerSpawner to spawn the Tower
        UIManager.Instance.Initialize(this, waveManager, tower);
    }

    private void SpawnTower()
    {
        if (towerSpawner == null)
        {
            Debug.LogError("TowerSpawner is not assigned to RoundManager.");
            return;
        }

        tower = towerSpawner.SpawnTower(); // Spawn the Tower and store the reference
        if (tower != null)
        {
            tower.Initialize(this, enemySpawner, skillManager); // Initialize the Tower with SkillManager
            waveManager.StartWave(enemySpawner, tower); // Start the wave using WaveManager
        }
        else
        {
            Debug.LogError("Failed to spawn Tower.");
        }
    }

    private void StartNewRound()
    {
        playerManager = PlayerManager.main;

        if (playerManager != null)
        {
            InitializeRound(playerManager);
            roundDifficulty = playerManager.GetDifficulty();
            
        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }
    }

    public void InitializeRound(PlayerManager playerManager)
    {
        InitializeRoundSkills(playerManager);
    }

    private void InitializeRoundSkills(PlayerManager playerManager)
    {
        if (skillManager == null)
        {
            Debug.LogError("SkillManager is not assigned to RoundManager.");
            return;
        }

        skillManager.InitializeSkills(playerManager.attackSkills, playerManager.defenceSkills, playerManager.supportSkills, playerManager.specialSkills);

        Debug.Log("Round skills initialized successfully.");

        SetStartBasicCredits();
    }

    private void SetStartBasicCredits()
    {
        float startingBasicCredits = GetSkillValue(GetSkill("Start Basic Credit"));
        IncreaseBasicCredits(startingBasicCredits);
    }

    public Skill GetSkill(string skillName)
    {
        return skillManager?.GetSkill(skillName);
    }

    public float GetSkillValue(Skill skill)
    {
        return skillManager?.GetSkillValue(skill) ?? 0f;
    }

    public void UpgradeSkill(Skill skill, int levelIncrease)
    {
        skillManager?.UpgradeSkill(skill, levelIncrease);
    }

    public void IncreaseBasicCredits(float amount)
    {
        float basicCreditModifier = GetSkillValue(GetSkill("Basic Credit Modifier"));
        tempBasicCredits += (amount * basicCreditModifier);
    }

    public bool SpendBasicCredits(float amount)
    {
        if (tempBasicCredits >= amount)
        {
            tempBasicCredits -= amount;
            return true;
        }
        else
        {
            Debug.Log("Not enough Basic Credits");
            return false;
        }
    }

    public void SetRoundDifficulty(int difficulty)
    {
        roundDifficulty = difficulty;
    }

    public int GetRoundDifficulty()
    {
        return roundDifficulty;
    }

    public float GetBasicCredits()
    {
        return tempBasicCredits;
    }

    public float GetPremiumCredits()
    {
        return playerManager.GetPremiumCredits();
    }

    public float GetLuxuryCredits()
    {
        return playerManager.GetLuxuryCredits();
    }

    public void IncreasePremiumCredits(float amount)
    {
        playerManager.premiumCredits += amount;
    }

    public void IncreaseLuxuryCredits(float amount)
    {
        playerManager.luxuryCredits += amount;
    }

    

}
