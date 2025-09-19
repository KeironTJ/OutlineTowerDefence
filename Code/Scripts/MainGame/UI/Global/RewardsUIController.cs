using UnityEngine;

public class RewardsUIController : MonoBehaviour
{
    [Header("Assign prefab with MainRewardSceen on root")]
    [SerializeField] private GameObject rewardsPrefab;
    [SerializeField] private Transform uiParent; // optional: canvas transform to parent into (leave null -> root canvas found)
    [SerializeField] private KeyCode openHotkey = KeyCode.R;
    [SerializeField] private bool pauseGameWhenOpen = false;
    [SerializeField] private bool persistAcrossScenes = true; // optional

    private GameObject instance;
    private MainRewardSceen screen;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (openHotkey != KeyCode.None && Input.GetKeyDown(openHotkey))
            Show();
    }

    public void Show()
    {
        if (instance == null)
        {
            if (rewardsPrefab == null)
            {
                Debug.LogError("[RewardsUIController] rewardsPrefab not assigned.");
                return;
            }

            // Find a canvas if no parent supplied
            Transform parent = uiParent;
            if (parent == null)
            {
                var canv = FindObjectOfType<Canvas>();
                if (canv != null) parent = canv.transform;
            }

            instance = Instantiate(rewardsPrefab, parent, false);
            instance.name = rewardsPrefab.name;
            instance.transform.SetAsLastSibling();

            screen = instance.GetComponent<MainRewardSceen>();
            if (screen == null) Debug.LogWarning("[RewardsUIController] prefab missing MainRewardSceen component.");
        }

        instance.SetActive(true);
        screen?.RefreshUI();

        if (pauseGameWhenOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void Hide()
    {
        if (instance == null) return;

        instance.SetActive(false);

        if (pauseGameWhenOpen)
            Time.timeScale = previousTimeScale;
    }

    public void Toggle()
    {
        if (instance == null || !instance.activeSelf) Show(); else Hide();
    }
}