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

    [Header("Shop Menu")]
    [SerializeField] private Menu shopMenu;          // assign the Menu component under Canvas in Inspector


    // Wave Info
    [Header("Wave Information")]
    [SerializeField] private TextMeshProUGUI difficultyNumberUI;
    [SerializeField] private TextMeshProUGUI waveNumberUI;
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
    [SerializeField] private TextMeshProUGUI fragmentsUI;
    [SerializeField] private TextMeshProUGUI coresUI;
    [SerializeField] private TextMeshProUGUI prismsUI;
    [SerializeField] private TextMeshProUGUI loopsUI;

    [Header("Stats")]
    [SerializeField] private RoundStatsView hudRoundStatsView;

    [Header("Round Stats Panel")]
    [SerializeField] private GameObject roundStatsPanel; // parent panel that contains the RoundStatsView



    private Tower tower;
    private ICurrencyWallet roundWallet;
    private float waveEndTime;


    private void Awake()
    {
        // Ensure the game over panel is hidden at the start
        gameOverPanel.SetActive(false);
        if (roundStatsPanel != null) roundStatsPanel.SetActive(false);
    }

    public void Initialize(RoundManager roundManager, WaveManager waveManager, Tower tower, SkillManager skillManager, PlayerManager playerManager)
    {
        this.roundManager = roundManager;
        this.waveManager = waveManager;
        this.tower = tower;
        this.skillManager = skillManager;
        this.playerManager = playerManager;
        this.roundWallet = roundManager.GetRoundWallet();

        if (hudRoundStatsView != null && this.roundManager != null)
            hudRoundStatsView.BindLive(this.roundManager);

        // NEW: initialize Shop Menu even if it's not a child of UIManager
        if (shopMenu == null)
            shopMenu = UnityEngine.Object.FindFirstObjectByType<Menu>(UnityEngine.FindObjectsInactive.Include);

        if (shopMenu != null)
        {
            shopMenu.Initialize(roundManager, waveManager, tower, skillManager);
        }
        else
        {
            Debug.LogWarning("UIManager: Shop Menu not found. Assign 'shopMenu' in the Inspector.");
        }

        if (roundWallet != null) roundWallet.BalanceChanged += OnBalanceChanged;
        if (playerManager?.Wallet != null) playerManager.Wallet.BalanceChanged += OnBalanceChanged;
        else Debug.LogError("PlayerManager or Player Wallet is not set in UIManager.");

        UpdateAllCurrencyUI();

        // REMOVE (no longer a child): GetComponentInChildren<Menu>()
        // Menu menu = GetComponentInChildren<Menu>();
        // if (menu != null) { menu.Initialize(roundManager, waveManager, tower, skillManager); }

        if (tower != null) tower.TowerDestroyed += OnTowerDestroyedHandler;
        else Debug.LogError("Tower reference is not set in UIManager.");
    }

    // Optional helper for your button
    public void ToggleShopMenu()
    {
        if (shopMenu != null) shopMenu.ToggleMenu();      // if your Menu has this
        else Debug.LogWarning("UIManager: shopMenu not assigned.");
    }

    public void OnRoundStart(RoundManager rm)
    {
        if (hudRoundStatsView != null)
        {
            hudRoundStatsView.BindLive(rm);
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
            health.text = $"{NumberManager.FormatLargeNumber(tower.GetCurrentHealth())} / {NumberManager.FormatLargeNumber(skillManager.GetSkillValue(skillManager.GetSkill("Health")))}";

            // Update health bar
            float currentHealth = tower.GetCurrentHealth();
            float maxHealth = skillManager.GetSkillValue(skillManager.GetSkill("Health"));
            healthProgressBar.value = currentHealth / maxHealth;

            // Update attack damage text
            towerAttackDamage.text = $"Attack: {NumberManager.FormatLargeNumber(skillManager.GetSkillValue(skillManager.GetSkill("Attack Damage")))}";
        }
    }

    private void OnBalanceChanged(CurrencyType type, float newValue)
    {
        // Update only what changed
        if (type == CurrencyType.Fragments)
        {
            UpdateFragmentsUI(newValue);
        }
        else
        {
            UpdateGlobalCurrencyUI(); // read from PlayerManager.Wallet
        }
    }

    private void UpdateAllCurrencyUI()
    {
        UpdateFragmentsUI(roundWallet != null ? roundWallet.Get(CurrencyType.Fragments) : 0f);
        UpdateGlobalCurrencyUI();
    }

    private void UpdateFragmentsUI(float newValue)
    {
        fragmentsUI.text = $"F: {NumberManager.FormatLargeNumber(newValue)}";
    }

    private void UpdateGlobalCurrencyUI()
    {
        if (playerManager?.Wallet != null)
        {
            coresUI.text = $"{NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Cores))}";
            prismsUI.text = $"{NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Prisms))}";
            loopsUI.text = $"{NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Loops))}";
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
            loseScreenWaveNumberUI.text = $"{waveManager.GetCurrentWave().ToString()}";
            if (waveManager.IsBetweenWaves())
            {
                float timeRemaining = waveManager.GetTimeBetweenWavesRemaining();
                waveTimeRemainingUI.text = $"{timeRemaining:F1}s";
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
        {
            waveProgressBar.value = waveProgressBar.value;
            waveNumberUI.text = $"{waveManager.GetCurrentWave().ToString()}";
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
        Tower tower = UnityEngine.Object.FindFirstObjectByType<Tower>();
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
        if (roundStatsPanel == null)
        {
            Debug.LogWarning("RoundStats panel is not assigned on UIManager.");
            return;
        }

        bool show = !roundStatsPanel.activeSelf;
        roundStatsPanel.SetActive(show);

        if (show && roundManager != null)
        {
            // Ensure the view is listening again and refresh immediately
            if (hudRoundStatsView != null)
                hudRoundStatsView.BindLive(roundManager);
            else
                Debug.LogWarning("hudRoundStatsView is not assigned.");

            // Optional: also push an update event
            EventManager.TriggerEvent(EventNames.RoundStatsUpdated, roundManager.GetCurrentRoundSummary());
        }
    }

}
