using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivePanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI progressText;   // NEW (assign in prefab)
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject completionOverlay;   // shown when completed & NOT claimed (contains Claim button)
    [SerializeField] private TextMeshProUGUI completionText;
    [SerializeField] private GameObject claimedOverlay;      // NEW: shown after claim
    [SerializeField] private TextMeshProUGUI claimedText;    // optional label

    private ObjectiveRuntime runtime;

    public void Bind(ObjectiveRuntime rt)
    {
        runtime = rt;
        var def = rt.definition;

        if (progressSlider == null)
        {
            Debug.LogError("[ObjectivePanelUI] progressSlider not assigned.", this);
            return;
        }

        progressSlider.wholeNumbers = true;
        progressSlider.minValue = 0;
        progressSlider.maxValue = Mathf.Max(1, def.targetAmount);
        progressSlider.value = Mathf.Clamp(rt.progressData.currentProgress, 0, def.targetAmount);

        titleText.text = $"{def.type} ({def.difficulty})";
        descText.text = def.description;
        rewardText.text = $"{def.rewardType} +{def.rewardAmount}";

        UpdateProgressVisual();
        UpdateStateVisuals();   // REPLACED (was Refresh + UpdateCompletion)

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() =>
        {
            DailyObjectiveManager.main?.Claim(runtime);
            UpdateProgressVisual();
            UpdateStateVisuals();
        });

        Debug.Log($"[ObjectivePanelUI] Bound {def.id} {rt.progressData.currentProgress}/{def.targetAmount}");
    }

    public void UpdateProgress()
    {
        if (runtime == null) return;
        UpdateProgressVisual();
        UpdateStateVisuals();
    }

    private void UpdateProgressVisual()
    {
        var def = runtime.definition;
        float cur = Mathf.Clamp(runtime.progressData.currentProgress, 0, def.targetAmount);
        if (progressSlider.maxValue != def.targetAmount)
            progressSlider.maxValue = Mathf.Max(1, def.targetAmount);
        progressSlider.value = cur;
        if (progressText != null)
            progressText.text = NumberManager.FormatLargeNumber(cur, true) + "/" + NumberManager.FormatLargeNumber(def.targetAmount, true);
    }

    private void UpdateStateVisuals()
    {
        if (runtime == null) return;

        bool finished = runtime.Completed;
        bool claimed = runtime.Claimed;
        bool manual = runtime.definition.manualClaim;

        // Overlays
        if (completionOverlay != null)
            completionOverlay.SetActive(finished && !claimed);
        if (claimedOverlay != null)
            claimedOverlay.SetActive(claimed);

        // Text labels (optional)
        if (completionText != null)
            completionText.text = "Claim Reward!";
        if (claimedText != null)
            claimedText.text = "Reward Claimed!";

        // Progress / slider visibility
        bool showProgress = !finished; // hide once finished (either claimed or waiting to claim)
        if (progressSlider != null) progressSlider.gameObject.SetActive(showProgress);
        if (progressText != null) progressText.gameObject.SetActive(showProgress);

        // Claim button visibility (put the button inside completionOverlay or keep here)
        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(manual && finished && !claimed);
            claimButton.interactable = manual && finished && !claimed;
        }
    }

    public void RefreshClaimState()   // keep for external calls; now just redirects
    {
        UpdateStateVisuals();
    }
}