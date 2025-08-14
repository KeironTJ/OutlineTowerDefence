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

    public ICurrencyWallet GetRoundWallet() => roundWallet;

    [Header("Round Wallet")]
    [SerializeField] private RoundCurrencyWallet roundWallet;

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

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.CreditsEarned, OnCreditsEarned);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.CreditsEarned, OnCreditsEarned);

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
            uiManager.Initialize(this, waveManager, tower, skillManager, playerManager); // Initialize UIManager with necessary references
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
            roundWallet = new RoundCurrencyWallet(
                playerManager.Wallet,
                amount =>
                {
                    playerManager.RecordBasicSpent(amount);
                }
            );

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
        roundWallet?.ClearRound();
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

    // CREDITS
    private void OnCreditsEarned(object eventData)
    {
        // TODO: SIMPLIFY BELOW LOGIC 
        // Case 1: event sends Enemy
        if (eventData is Enemy enemy)
        {
            IncreaseBasicCredits(enemy.GetBasicCreditsWorth());
            playerManager.Wallet.Add(CurrencyType.Premium, enemy.GetPremiumCreditsWorth());
            playerManager.Wallet.Add(CurrencyType.Luxury, enemy.GetLuxuryCreditsWorth());
            playerManager.Wallet.Add(CurrencyType.Special, enemy.GetSpecialCreditsWorth());
            return;
        }

        if (eventData is Dictionary<CurrencyType, float> rewards)
        {
            if (rewards.TryGetValue(CurrencyType.Basic, out var b)) IncreaseBasicCredits(b);
            if (rewards.TryGetValue(CurrencyType.Premium, out var p)) playerManager.Wallet.Add(CurrencyType.Premium, p);
            if (rewards.TryGetValue(CurrencyType.Luxury, out var l)) playerManager.Wallet.Add(CurrencyType.Luxury, l);
            if (rewards.TryGetValue(CurrencyType.Special, out var s)) playerManager.Wallet.Add(CurrencyType.Special, s);
            return;
        }

        Debug.LogWarning("CreditsEarned received unexpected event payload.");
    }

    private void SetStartBasicCredits()
    {
        float startingBasicCredits = skillManager.GetSkillValue(skillManager.GetSkill("Start Basic Credit"));
        IncreaseBasicCredits(startingBasicCredits);
    }

    public void IncreaseBasicCredits(float amount)
    {
        if (amount == 0f) return;

        // Treat skill as a bonus (0 => x1.0, 0.25 => x1.25)
        float bonus = skillManager.GetSkillValue(skillManager.GetSkill("Basic Credit Modifier"));
        float multiplier = Mathf.Max(0f, 1f + bonus);
        roundWallet?.Add(CurrencyType.Basic, amount * multiplier);
    }

    public bool SpendBasicCredits(float amount)
    {
        return roundWallet?.TrySpend(CurrencyType.Basic, amount) ?? false; // NEW
    }

    public float GetBasicCredits()
    {
        return roundWallet?.Get(CurrencyType.Basic) ?? 0f; // NEW
    }


}
