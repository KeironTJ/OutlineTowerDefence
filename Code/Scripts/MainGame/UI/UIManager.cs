using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private WaveManager waveInfo; // Updated to WaveManager
    [SerializeField] private GameObject sideMenu;
    [SerializeField] private GameObject skillMenu;

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
    private RoundManager roundManager;
    private float waveEndTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Ensure the game over panel is hidden at the start
        gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(FindRoundInstance());
        StartCoroutine(FindWaveInfo());
        StartCoroutine(FindTowerInstance());

        //gameOverPanel.SetActive(false);
    }

    private IEnumerator FindRoundInstance()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
            if (roundManager == null)
            {
                yield return wait;
            }
        }
    }

    private IEnumerator FindTowerInstance()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (tower == null)
        {
            tower = FindObjectOfType<Tower>();
            if (tower == null)
            {
                yield return wait;
            }
        }
    }

    private IEnumerator FindWaveInfo()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (waveInfo == null)
        {
            waveInfo = FindObjectOfType<WaveManager>(); // Updated to find WaveManager
            if (waveInfo == null)
            {
                Debug.LogWarning("waveInfo not found, retrying...");
                yield return wait;
            }
            else
            {
                Debug.Log("waveInfo found.");
            }
        }
    }

    private void Update()
    {
        UpdateWaveUI();
        UpdateTowerUI();
        UpdateCreditsUI();
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

    public void UpdateCreditsUI()
    {
        if (roundManager != null)
        {
            basicCreditsUI.text = $"Basic: {NumberManager.FormatLargeNumber(roundManager.GetBasicCredits())}";
            premiumCreditsUI.text = $"Premium: {NumberManager.FormatLargeNumber(roundManager.GetPremiumCredits())}";
            luxuryCreditsUI.text = $"Luxury: {NumberManager.FormatLargeNumber(roundManager.GetLuxuryCredits())}";
        }
    }

    public void UpdateWaveUI()
    {
        if (!isGameOver && waveInfo != null)
        {
            difficultyNumberUI.text = $"Difficulty: {roundManager.GetRoundDifficulty().ToString()}";
            waveNumberUI.text = $"Wave: {waveInfo.GetCurrentWave().ToString()}";
            enemyNumberUI.text = $"Enemies: {waveInfo.GetEnemiesLeftToSpawn().ToString()} / {waveInfo.EnemiesPerWave()}";
            loseScreenWaveNumberUI.text = $"{waveInfo.GetCurrentWave().ToString()}";
            if (waveInfo.IsBetweenWaves())
            {
                float timeRemaining = waveInfo.GetTimeBetweenWavesRemaining();
                waveTimeRemainingUI.text = $"Next Wave In: {timeRemaining:F1}s";
                waveProgressBar.value = 1 - (timeRemaining / waveInfo.timeBetweenWaves); // Updated logic
                fillAreaImage.color = Color.red;
            }
            else
            {
                float timeRemaining = waveInfo.GetWaveTimeRemaining();
                waveTimeRemainingUI.text = $"Time Left: {timeRemaining:F1}s";
                waveProgressBar.value = 1 - (timeRemaining / waveInfo.timePerWave); // Updated logic
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
