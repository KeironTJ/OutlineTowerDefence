using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private WaveManager waveManager; 
    [SerializeField] private GameObject sideMenu;
    [SerializeField] private GameObject skillMenu;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private PlayerManager playerManager;


    // Wave Info
    [Header("Wave Information")]
    [SerializeField] private TextMeshProUGUI difficultyNumberUI;
    [SerializeField] private TextMeshProUGUI waveNumberUI;
    [SerializeField] private TextMeshProUGUI enemyNumberUI;
    [SerializeField] private TextMeshProUGUI loseScreenWaveNumberUI;
    [SerializeField] private TextMeshProUGUI waveTimeRemainingUI;
    [SerializeField] private Slider waveProgressBar;
    [SerializeField] private Image fillAreaImage;
    [SerializeField] private bool isGameOver = false;

    // Tower Info
    [Header("Tower Information")]
    [SerializeField] private TextMeshProUGUI health;
    [SerializeField] private Slider healthProgressBar;
    [SerializeField] private Image healthBardImage;
    [SerializeField] private TextMeshProUGUI towerAttackDamage;

    [Header("Currency Information")]
    [SerializeField] private TextMeshProUGUI basicCreditsUI;
    [SerializeField] private TextMeshProUGUI premiumCreditsUI;
    [SerializeField] private TextMeshProUGUI luxuryCreditsUI;


    private Tower tower;
    private ICurrencyWallet roundWallet;
    private float waveEndTime;


    private void Awake()
    {
        // Ensure the game over panel is hidden at the start
        gameOverPanel.SetActive(false);
    }



    public void Initialize(RoundManager roundManager, WaveManager waveManager, Tower tower, SkillManager skillManager, PlayerManager playerManager)
    {
        this.roundManager = roundManager;
        this.waveManager = waveManager;
        this.tower = tower;
        this.skillManager = skillManager;
        this.playerManager = playerManager;
        this.roundWallet = roundManager.GetRoundWallet();

        if (roundWallet != null)
        {
            roundWallet.BalanceChanged += OnBalanceChanged; // Subscribe to balance changes
        }
        if (playerManager?.Wallet != null)
        {
            playerManager.Wallet.BalanceChanged += OnBalanceChanged; // Subscribe to player wallet balance changes
        }
        else
        {
            Debug.LogError("PlayerManager or Player Wallet is not set in UIManager.");
        }

        UpdateAllCurrencyUI();

        Menu menu = GetComponentInChildren<Menu>();
        if (menu != null)
        {
            menu.Initialize(roundManager, waveManager, tower, skillManager);
        }

        if (tower != null)
        {
            tower.TowerDestroyed += OnTowerDestroyedHandler; // Subscribe to the TowerDestroyed event
        }
        else
        {
            Debug.LogError("Tower reference is not set in UIManager.");
        }
    }

    private void OnDisable()
    {
        if (tower != null)
        {
            tower.TowerDestroyed -= OnTowerDestroyedHandler;
        }
        if (roundWallet != null)
        {
            roundWallet.BalanceChanged -= OnBalanceChanged; // prevent duplicate subs on re-enable
        }
        if (playerManager?.Wallet != null)
        {
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged;
        }
    }

    private void OnDestroy()
    {
        if (roundWallet != null)
        {
            roundWallet.BalanceChanged -= OnBalanceChanged; // Unsubscribe from balance changes
        }
        if (playerManager?.Wallet != null)
        {
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged; // Unsubscribe from player wallet balance changes
        }
    }

    private void OnTowerDestroyedHandler()
    {
        ShowGameOverPanel();
    }

    private void Update()
    {
        UpdateWaveUI();
        UpdateTowerUI();
    }
    
    public void UpdateTowerUI()
    {
        if (tower != null)
        {
            // Update health text
            health.text = $"{NumberManager.FormatLargeNumber(tower.GetCurrentHealth())} / {NumberManager.FormatLargeNumber(roundManager.GetSkillValue(roundManager.GetSkill("Health")))}";

            // Update health bar
            float currentHealth = tower.GetCurrentHealth();
            float maxHealth = roundManager.GetSkillValue(roundManager.GetSkill("Health"));
            healthProgressBar.value = currentHealth / maxHealth;

            // Update attack damage text
            towerAttackDamage.text = $"Attack: {NumberManager.FormatLargeNumber(roundManager.GetSkillValue(roundManager.GetSkill("Attack Damage")))}";
        }
    }

    private void OnBalanceChanged(CurrencyType type, float newValue)
    {
        // Update only what changed
        if (type == CurrencyType.Basic)
        {
            UpdateBasicCreditsUI(newValue);
        }
        else
        {
            UpdateGlobalCreditsUI(); // read from PlayerManager.Wallet
        }
    }

    private void UpdateAllCurrencyUI()
    {
        UpdateBasicCreditsUI(roundWallet != null ? roundWallet.Get(CurrencyType.Basic) : 0f);
        UpdateGlobalCreditsUI();
    }

    private void UpdateBasicCreditsUI(float newValue)
    {
        basicCreditsUI.text = $"Basic: {NumberManager.FormatLargeNumber(newValue)}";
    }

    private void UpdateGlobalCreditsUI()
    {
        if (playerManager?.Wallet != null)
        {
            premiumCreditsUI.text = $"Premium: {NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Premium))}";
            luxuryCreditsUI.text = $"Luxury: {NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Luxury))}";
        }
        else
        {
            Debug.LogError("Player Wallet is not set in UIManager.");
        }
    }

    public void UpdateWaveUI()
    {
        if (!isGameOver && waveManager != null)
        {
            difficultyNumberUI.text = $"Difficulty: {roundManager.GetRoundDifficulty().ToString()}";
            waveNumberUI.text = $"Wave: {waveManager.GetCurrentWave().ToString()}";
            enemyNumberUI.text = $"Enemies: {waveManager.GetEnemiesLeftToSpawn().ToString()} / {waveManager.EnemiesPerWave()}";
            loseScreenWaveNumberUI.text = $"{waveManager.GetCurrentWave().ToString()}";
            if (waveManager.IsBetweenWaves())
            {
                float timeRemaining = waveManager.GetTimeBetweenWavesRemaining();
                waveTimeRemainingUI.text = $"Next Wave In: {timeRemaining:F1}s";
                waveProgressBar.value = 1 - (timeRemaining / waveManager.timeBetweenWaves); // Updated logic
                fillAreaImage.color = Color.red;
            }
            else
            {
                float timeRemaining = waveManager.GetWaveTimeRemaining();
                waveTimeRemainingUI.text = $"Time Left: {timeRemaining:F1}s";
                waveProgressBar.value = 1 - (timeRemaining / waveManager.timePerWave); // Updated logic
                fillAreaImage.color = Color.cyan;
            }
        }
        else
        {   // Keep the wave progress bar visible but do not update its value 
            waveProgressBar.value = waveProgressBar.value;
            waveNumberUI.text = "N/A";
        }
    }

    public void SetWaveEndTime(float endTime)
    {
        waveEndTime = endTime;
    }

    public void ShowGameOverPanel() 
    { 
        gameOverPanel.SetActive(true); // Stop the wave progress bar updates 
        sideMenu.SetActive(false); // Hide the side menu
        skillMenu.SetActive(false); // Hide the skill menu
        isGameOver = true;
    }

    public void ReturnToMenu()
    {
        // Find and destroy the Tower instance
        Tower tower = FindObjectOfType<Tower>();
        if (tower != null)
        {
            Destroy(tower.gameObject);
        }



        // Destroy the UIManager instance
        Destroy(gameObject);

        // Load the MainMenu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void ViewStats()
    {
        // Implement stats viewing logic here
    }
}
