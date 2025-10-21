using UnityEngine;

public class RewardsUIController : MonoBehaviour
{
    [Header("Assign existing screen instance with MainRewardSceen")]
    [SerializeField] private GameObject rewardsScreen;
    [SerializeField] private KeyCode openHotkey = KeyCode.R;
    [SerializeField] private bool pauseGameWhenOpen = false;
    [SerializeField] private bool persistAcrossScenes = true; // optional

    private MainRewardSceen screen;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        if (rewardsScreen != null)
        {
            screen = rewardsScreen.GetComponent<MainRewardSceen>();
            if (screen == null)
                Debug.LogWarning("[RewardsUIController] Assigned screen instance is missing MainRewardSceen component.");

            rewardsScreen.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[RewardsUIController] No rewards screen assigned. Assign an existing scene instance in the inspector.");
        }
    }

    private void Update()
    {
        if (openHotkey != KeyCode.None && Input.GetKeyDown(openHotkey))
            Show();
    }

    public void Show()
    {
        if (rewardsScreen == null)
        {
            Debug.LogError("[RewardsUIController] No rewards screen assigned.");
            return;
        }

        rewardsScreen.SetActive(true);
        screen?.RefreshUI();

        if (pauseGameWhenOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void Hide()
    {
        if (rewardsScreen == null) return;

        rewardsScreen.SetActive(false);

        if (pauseGameWhenOpen)
            Time.timeScale = previousTimeScale;
    }

    public void Toggle()
    {
        if (rewardsScreen == null || !rewardsScreen.activeSelf) Show(); else Hide();
    }

    public void OpenDailyRewards()
    {
        Show();
        screen?.OpenDailyRewards();
    }

    public void OpenWeeklyRewards()
    {
        Show();
        screen?.OpenWeeklyRewards();
    }

    public void OpenAchievements()
    {
        Show();
        screen?.OpenAchievements();
    }
}