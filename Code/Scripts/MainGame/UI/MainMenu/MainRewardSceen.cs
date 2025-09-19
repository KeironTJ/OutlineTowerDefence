using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;              // <-- add this
using System.Collections.Generic;

public class MainRewardSceen : MonoBehaviour
{
    [Header("Sub-Tab References")]
    public Button dailyRewardButton;
    public GameObject dailyRewardTab;

    public Button weeklyRewardButton;
    public GameObject weeklyRewardTab;

    public Button achievementRewardButton;
    public GameObject achievementRewardTab;


    [Header("Daily Login Reward")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    [Header("Daily Objectives")]
    public GameObject objectivePanelPrefab;
    public Transform dailyObjectivesContentParent; 

    [Header("Daily Objective Timer")]
    [SerializeField] private TextMeshProUGUI nextSlotTimerText;

    private readonly Dictionary<ObjectiveRuntime, ObjectivePanelUI> panelMap = new Dictionary<ObjectiveRuntime, ObjectivePanelUI>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (nextSlotTimerText && DailyObjectiveManager.main)
            nextSlotTimerText.text = "Next Objective: " + DailyObjectiveManager.main.GetNextSlotCountdownString();
    }

    private IEnumerator DelayedPopulate()  
    {
        yield return null;
        PopulateDailyObjectives();
    }

    void OnEnable()
    {
        DailyObjectiveManager.main?.EnsureInitialized();
        DailyObjectiveManager.OnProgress += HandleObjectiveProgress;
        DailyObjectiveManager.OnSlotRollover += HandleSlotRollover;
        UpdateDailyLoginUI();
        OpenDailyRewardTab();
        StartCoroutine(DelayedPopulate());
    }

    void OnDisable()
    {
        DailyObjectiveManager.OnProgress -= HandleObjectiveProgress;
        DailyObjectiveManager.OnSlotRollover -= HandleSlotRollover;
        if (claimButton) claimButton.interactable = false;
        if (statusText) statusText.text = "";
    }
    
    void UpdateDailyLoginUI()
    {
        var dlm = DailyLoginRewardManager.main;
        if (dlm == null)
        {
            if (claimButton) claimButton.interactable = false;
            if (statusText) statusText.text = "Daily login unavailable";
            Debug.LogWarning("[MainRewardSceen] DailyLoginRewardManager.main is null; login UI disabled.");
            return;
        }

        bool canClaim = dlm.CanClaimToday();
        if (claimButton) claimButton.interactable = canClaim;
        if (statusText) statusText.text = canClaim ? "Daily login reward available!" : "Already claimed today.";
    }
    
    public void OnClaimDailyLogin()
    {
        var dlm = DailyLoginRewardManager.main;
        if (dlm == null) { Debug.LogWarning("[MainRewardSceen] Can't claim: DailyLoginRewardManager.main is null"); return; }
        dlm.ClaimToday();
        UpdateDailyLoginUI();
    }

    public void OpenDailyRewardTab()
    {
        CloseSubTabScreens();
        HighlightButton(dailyRewardButton);
        dailyRewardTab.SetActive(true);
        PopulateDailyObjectives();
    }

    public void OpenWeeklyRewardTab()
    {
        CloseSubTabScreens();
        HighlightButton(weeklyRewardButton);
        weeklyRewardTab.SetActive(true);
    }

    public void OpenAchievementRewardTab()
    {
        CloseSubTabScreens();
        HighlightButton(achievementRewardButton);
        achievementRewardTab.SetActive(true);
    }

    public void CloseSubTabScreens()
    {
        dailyRewardTab.SetActive(false);
        weeklyRewardTab.SetActive(false);
        achievementRewardTab.SetActive(false);
    }

    // Highlight button when button activates panel
    public void HighlightButton(Button button)
    {
        // Reset all buttons' colors
        dailyRewardButton.GetComponent<Image>().color = Color.cyan;
        dailyRewardButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;

        weeklyRewardButton.GetComponent<Image>().color = Color.cyan;
        weeklyRewardButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;

        achievementRewardButton.GetComponent<Image>().color = Color.cyan;
        achievementRewardButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;

        // Highlight the selected button and make button text white
        button.GetComponent<Image>().color = Color.black;
        button.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
    }

    void PopulateDailyObjectives()
    {
        if (objectivePanelPrefab == null || dailyObjectivesContentParent == null) return;
        DailyObjectiveManager.main?.EnsureInitialized();

        var list = DailyObjectiveManager.main?.GetActiveDailyObjectives();
        if (list == null) return;

        // Build a set for membership checks (IReadOnlyList has no Contains)
        var activeSet = new System.Collections.Generic.HashSet<ObjectiveRuntime>(list);

        // Remove panels no longer active
        var toRemove = new System.Collections.Generic.List<ObjectiveRuntime>();
        foreach (var kvp in panelMap)
        {
            if (!activeSet.Contains(kvp.Key))
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var rt in toRemove) panelMap.Remove(rt);

        // Add new panels
        foreach (var rt in list)
        {
            if (panelMap.ContainsKey(rt)) continue;
            var go = Instantiate(objectivePanelPrefab, dailyObjectivesContentParent);
            var ui = go.GetComponent<ObjectivePanelUI>();
            if (ui == null)
            {
                Debug.LogError("[RewardsUI] ObjectivePanelUI missing on prefab.");
                Destroy(go);
                continue;
            }
            ui.Bind(rt);
            panelMap.Add(rt, ui);
        }

        // Refresh existing panels
        foreach (var ui in panelMap.Values)
            ui.UpdateProgress();
    }

    private void HandleObjectiveProgress(ObjectiveRuntime rt)
    {
        if (rt != null && panelMap.TryGetValue(rt, out var ui))
        {
            ui.UpdateProgress();
        }
        else
        {
            PopulateDailyObjectives(); // fallback
        }
    }

    private void HandleSlotRollover(string newSlotKey)
    {
        PopulateDailyObjectives(); // refresh list after rollover
    }

    // Public entrypoint for external callers (Options menu / HUD / controller)
    public void RefreshUI()
    {
        DailyObjectiveManager.main?.EnsureInitialized();
        UpdateDailyLoginUI();
        PopulateDailyObjectives();
    }

    // Close/hide the rewards screen (wire a UI Close button to this)
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
