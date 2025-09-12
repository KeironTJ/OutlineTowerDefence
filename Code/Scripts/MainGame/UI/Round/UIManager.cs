using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Serialization;   


public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject sideMenu;
    [SerializeField] private GameObject skillMenu;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private PlayerManager playerManager;

    [Header("Services")]
    [SerializeField] private SkillService skillService; 

    [Header("Shop Menu")]
    [SerializeField] private Menu shopMenu;

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
    [FormerlySerializedAs("health")]            // preserves old inspector reference
    [SerializeField] private TextMeshProUGUI towerHealth;
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
    [SerializeField] private GameObject roundStatsPanel;

    [Header("Skill Ids (match SkillDefinition ids)")]

    [SerializeField] private string attackDamageSkillId = "Attack Damage";

    private Tower tower;
    private ICurrencyWallet roundWallet;
    private float waveEndTime;

    private void Awake()
    {
        gameOverPanel.SetActive(false);
        if (roundStatsPanel != null) roundStatsPanel.SetActive(false);
        if (!skillService) skillService = SkillService.Instance;
    }

    // OLD SIGNATURE (removed SkillManager)
    public void Initialize(RoundManager roundManager, WaveManager waveManager, Tower tower, PlayerManager playerManager)
    {
        this.roundManager = roundManager;
        this.waveManager = waveManager;
        this.tower = tower;
        this.playerManager = playerManager;
        this.roundWallet = roundManager != null ? roundManager.RoundWallet : null;

        if (!skillService) skillService = SkillService.Instance;

        if (hudRoundStatsView != null && this.roundManager != null)
            hudRoundStatsView.BindLive(this.roundManager);

        if (shopMenu == null)
            shopMenu = UnityEngine.Object.FindFirstObjectByType<Menu>(UnityEngine.FindObjectsInactive.Include);

        if (shopMenu != null)
        {
            // Ensure your Menu script was updated to rely on SkillService (not SkillManager)
            shopMenu.Initialize(roundManager, waveManager, tower, roundWallet);
        }
        else
        {
            Debug.LogWarning("UIManager: Shop Menu not found. Assign 'shopMenu' in the Inspector.");
        }

        if (roundWallet != null) roundWallet.BalanceChanged += OnBalanceChanged;
        if (playerManager?.Wallet != null) playerManager.Wallet.BalanceChanged += OnBalanceChanged;
        else Debug.LogError("PlayerManager or Player Wallet is not set in UIManager.");

        UpdateAllCurrencyUI();

        if (tower != null)
        {
            tower.TowerDestroyed += OnTowerDestroyedHandler;
            tower.HealthChanged += OnTowerHealthChanged;
            // We missed the initial event fired inside Tower.Initialize, so force a manual refresh:
            OnTowerHealthChanged(tower.GetCurrentHealth(), tower.MaxHealth); 
        }
    }

    private void OnDisable()
    {
        if (tower != null)
        {
            tower.TowerDestroyed -= OnTowerDestroyedHandler;
            tower.HealthChanged -= OnTowerHealthChanged;
        }
        if (roundWallet != null)
            roundWallet.BalanceChanged -= OnBalanceChanged;
        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged;
    }

    private void OnDestroy()
    {
        if (roundWallet != null)
            roundWallet.BalanceChanged -= OnBalanceChanged;
        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged;
        if (tower != null)
            tower.HealthChanged -= OnTowerHealthChanged;
    }

    private void Update()
    {
        UpdateWaveUI();
        // Fallback update (still polling for attack damage changes if skill upgraded)
        UpdateTowerAttackDamage();
    }

    private void OnTowerHealthChanged(float current, float max)
    {
        if (towerHealth)
            towerHealth.text = $"{NumberManager.FormatLargeNumber(current)} / {NumberManager.FormatLargeNumber(max)}";
        if (healthProgressBar)
            healthProgressBar.value = max > 0f ? current / max : 0f;
    }

    private void UpdateTowerAttackDamage()
    {
        if (!towerAttackDamage) return;
        float dmg = skillService ? skillService.GetValue(attackDamageSkillId) : 0f;
        towerAttackDamage.text = $"Attack: {NumberManager.FormatLargeNumber(dmg)}";
    }

    // Removed UpdateTowerHealth method as it's no longer needed

    // Removed old UpdateTowerUI body relying on SkillManager; now split:
    public void UpdateTowerUI()
    {
        if (!tower) return;
        // Health is event-driven; ensure attack damage stays fresh:
        UpdateTowerAttackDamage();
    }

    private void OnBalanceChanged(CurrencyType type, float newValue)
    {
        if (type == CurrencyType.Fragments)
            UpdateFragmentsUI(newValue);
        else
            UpdateGlobalCurrencyUI();
    }

    private void UpdateAllCurrencyUI()
    {
        UpdateFragmentsUI(roundWallet != null ? roundWallet.Get(CurrencyType.Fragments) : 0f);
        UpdateGlobalCurrencyUI();
    }

    private void UpdateFragmentsUI(float newValue)
    {
        if (fragmentsUI)
            fragmentsUI.text = NumberManager.FormatLargeNumber(newValue);
    }

    private void UpdateGlobalCurrencyUI()
    {
        if (playerManager?.Wallet == null) return;
        if (coresUI)  coresUI.text  = NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Cores));
        if (prismsUI) prismsUI.text = NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Prisms));
        if (loopsUI)  loopsUI.text  = NumberManager.FormatLargeNumber(playerManager.Wallet.Get(CurrencyType.Loops));
    }

    public void UpdateWaveUI()
    {
        if (isGameOver || waveManager == null || roundManager == null) return;

        if (difficultyNumberUI)
            difficultyNumberUI.text = $"Difficulty: {roundManager.GetRoundDifficulty()}";
        if (waveNumberUI)
            waveNumberUI.text = $"Wave: {waveManager.GetCurrentWave()}";
        if (loseScreenWaveNumberUI)
            loseScreenWaveNumberUI.text = waveManager.GetCurrentWave().ToString();

        if (waveManager.IsBetweenWaves())
        {
            float timeRemaining = waveManager.GetTimeBetweenWavesRemaining();
            if (waveTimeRemainingUI) waveTimeRemainingUI.text = $"{timeRemaining:F1}s";
            if (waveProgressBar) waveProgressBar.value = 1 - (timeRemaining / waveManager.timeBetweenWaves);
            if (fillAreaImage) fillAreaImage.color = Color.red;
        }
        else
        {
            float timeRemaining = waveManager.GetWaveTimeRemaining();
            if (waveTimeRemainingUI) waveTimeRemainingUI.text = $"Time Left: {timeRemaining:F1}s";
            if (waveProgressBar) waveProgressBar.value = 1 - (timeRemaining / waveManager.timePerWave);
            if (fillAreaImage) fillAreaImage.color = Color.cyan;
        }
    }

    public void ToggleShopMenu()
    {
        if (shopMenu != null) shopMenu.ToggleMenu();
    }

    private void OnTowerDestroyedHandler() => ShowGameOverPanel();

    public void ShowGameOverPanel()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (sideMenu) sideMenu.SetActive(false);
        if (skillMenu) skillMenu.SetActive(false);
        isGameOver = true;
    }

    public void ViewStats()
    {
        if (!roundStatsPanel) { Debug.LogWarning("RoundStats panel not assigned."); return; }
        bool show = !roundStatsPanel.activeSelf;
        roundStatsPanel.SetActive(show);

        if (show && roundManager != null && hudRoundStatsView != null)
            hudRoundStatsView.BindLive(roundManager);
    }

    public void ReturnToMenu()
    {
        var t = UnityEngine.Object.FindFirstObjectByType<Tower>();
        if (t) Destroy(t.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }
}
