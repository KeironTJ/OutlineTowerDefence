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
    [SerializeField] private UIManager uiManager; // Serialized field for UIManager

    [Header("Managers")]
    [SerializeField] private SkillManager skillManager; // Reference to SkillManager

    public float tempBasicCredits = 0;

    private Tower tower;
    public int roundDifficulty;

    PlayerManager playerManager;


    private void Start()
    {
        if (tower == null)
        {
            StartNewRound();
            SpawnTower(); // Use TowerSpawner to spawn the Tower
        }
    }

    private void OnDisable()
    {
        if (tower != null)
        {
            tower.TowerDestroyed -= EndRound; // Unsubscribe from the TowerDestroyed event
        }
    }

    // TOWER MANAGEMENT
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
            tower.Initialize(this, enemySpawner, skillManager, uiManager); // Initialize the Tower with SkillManager
            tower.TowerDestroyed += EndRound; // Subscribe to the TowerDestroyed event
            waveManager.StartWave(enemySpawner, tower); // Start the wave using WaveManager
            uiManager.Initialize(this, waveManager, tower, skillManager); // Initialize UIManager with necessary references
        }
        else
        {
            Debug.LogError("Failed to spawn Tower.");
        }
    }

    public void DestroyAllBullets()
    {
        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (Bullet bullet in bullets)
        {
            Destroy(bullet.gameObject);
        }
    }

    // ROUND MANAGEMENT
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

        SetStartBasicCredits();
    }

    public void EndRound()
    {
        SetHighestWaves();
        DestroyAllEnemies();
        DestroyAllBullets();
    

    }

    // PLAYER MANAGEMENT
    private void SetHighestWaves()
    {
        if (playerManager != null)
        {
            int difficulty = playerManager.GetDifficulty();
            int highestWave = playerManager.GetHighestWave(difficulty);

            playerManager.SetMaxWaveAchieved(difficulty, waveManager.GetCurrentWave());
            
        }
    }

    // SKILLS
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

    // Enemies
    private void DestroyAllEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    // ROUND DIFFICULTY
    public void SetRoundDifficulty(int difficulty)
    {
        roundDifficulty = difficulty;
    }

    public int GetRoundDifficulty()
    {
        return roundDifficulty;
    }

    // BASIC CREDITS
    private void SetStartBasicCredits()
    {
        float startingBasicCredits = GetSkillValue(GetSkill("Start Basic Credit"));
        IncreaseBasicCredits(startingBasicCredits);
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

    public float GetBasicCredits()
    {
        return tempBasicCredits;
    }

    // PREMIUM CREDITS

    public float GetPremiumCredits()
    {
        return playerManager.GetPremiumCredits();
    }

    public void IncreasePremiumCredits(float amount)
    {
        playerManager.playerData.premiumCredits += amount;
    }

    // LUXURY CREDITS
    public float GetLuxuryCredits()
    {
        return playerManager.playerData.luxuryCredits;
    }

    public void IncreaseLuxuryCredits(float amount)
    {
        playerManager.playerData.luxuryCredits += amount;
    }


}
