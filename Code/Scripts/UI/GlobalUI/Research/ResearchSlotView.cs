using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual controller for a single research slot entry.
/// Handles unlocked, locked, and active research states.
/// </summary>
public class ResearchSlotView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI slotLabelText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI unlockCostText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button slotButton;
    [SerializeField] private GameObject lockedStateRoot;
    [SerializeField] private GameObject unlockedStateRoot;
    [SerializeField] private GameObject activeStateRoot;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private float fillLerpSpeed = 6f;

    private int slotIndex;
    private Action<int> unlockRequested;
    private Action<int> slotSelected;
    private ResearchProgressData boundProgress;
    private ResearchDefinition boundDefinition;
    private bool isUnlocked;
    private bool isActive;
    private bool isNextUnlock;
    private bool canAffordUnlock;
    private float unlockCost;
    private float cachedRemainingSeconds;
    private float cachedDurationSeconds;
    private bool canSelectSlot;
    private float targetFillAmount;
    private bool wasActive;

    public void Initialize(int index, Action<int> onUnlockRequested, Action<int> onSlotSelected)
    {
        slotIndex = index;
        unlockRequested = onUnlockRequested;
        slotSelected = onSlotSelected;

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnUnlockClicked);
            unlockButton.onClick.AddListener(OnUnlockClicked);
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnSlotClicked);
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    public void Bind(ResearchSlotDisplayData data)
    {
        isUnlocked = data.IsUnlocked;
        isActive = data.IsActive;
        isNextUnlock = data.IsNextUnlock;
        canAffordUnlock = data.CanAffordUnlock;
        unlockCost = data.UnlockCost;
        boundProgress = data.Progress;
        boundDefinition = data.Definition;
        cachedRemainingSeconds = data.RemainingSeconds;
        cachedDurationSeconds = data.DurationSeconds;
        canSelectSlot = data.AllowSelection;

        if (slotLabelText != null)
            slotLabelText.text = $"Slot {slotIndex + 1}";

        bool becameActive = isActive && !wasActive;
        bool becameInactive = !isActive && wasActive;

        if (lockedStateRoot != null)
            lockedStateRoot.SetActive(!isUnlocked);
        if (unlockedStateRoot != null)
            unlockedStateRoot.SetActive(isUnlocked && !isActive);
        if (activeStateRoot != null)
            activeStateRoot.SetActive(isUnlocked && isActive);

        if (slotButton != null)
        {
            slotButton.interactable = isUnlocked && canSelectSlot;
        }

        if (!isUnlocked)
        {
            RenderLockedState();
            wasActive = false;
            return;
        }

        if (becameInactive)
        {
            SetTargetFill(0f, true);
        }

        RenderUnlockedState(becameActive);
        wasActive = isActive;
    }

    public void RefreshProgress(bool immediate = false)
    {
        if (!isUnlocked || boundProgress == null)
        {
            SetTargetFill(0f, immediate);
            return;
        }

        if (!isActive)
        {
            SetTargetFill(0f, immediate);
            return;
        }

        if (ResearchService.Instance == null)
            return;

        float remaining = ResearchService.Instance.GetRemainingTime(boundProgress.researchId);
        float duration = Mathf.Max(boundProgress.durationSeconds, 0.01f);
        float fill = Mathf.Clamp01(1f - (remaining / duration));

        cachedRemainingSeconds = remaining;
        cachedDurationSeconds = duration;

        SetTargetFill(fill, immediate);

        if (detailText != null)
            detailText.text = FormatTime(remaining);
    }

    private void RenderLockedState()
    {
        SetTargetFill(0f, true);

        if (statusText != null)
        {
            statusText.text = isNextUnlock ? "Unlock available" : "Locked";
        }

        if (unlockCostText != null)
        {
            if (isNextUnlock)
                unlockCostText.text = unlockCost > 0f ? $"Unlock {unlockCost:F0} prisms" : "Unlock";
            else
                unlockCostText.text = unlockCost > 0f ? $"Unlock cost {unlockCost:F0} prisms" : "Locked";
        }

        if (detailText != null)
        {
            if (isNextUnlock && !canAffordUnlock)
                detailText.text = "Not enough prisms";
            else if (!isNextUnlock)
                detailText.text = "Unlock previous slot";
            else
                detailText.text = string.Empty;
        }

        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(isNextUnlock);
            unlockButton.interactable = canAffordUnlock;
        }

        if (slotButton != null)
            slotButton.interactable = false;

        if (progressFillImage != null)
            progressFillImage.fillAmount = targetFillAmount;
    }

    private void RenderUnlockedState(bool becameActive)
    {
        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(false);
        }

        if (!isActive)
        {
            if (statusText != null)
                statusText.text = "Idle";

            if (detailText != null)
                detailText.text = "Select research to start";

            SetTargetFill(0f, true);

            if (slotButton != null)
            {
                slotButton.interactable = canSelectSlot;
            }
            return;
        }

        string displayName = boundDefinition != null ? boundDefinition.displayName : boundProgress?.researchId;

        if (statusText != null)
            statusText.text = displayName ?? "Active";

        if (detailText != null)
        {
            detailText.text = FormatTime(cachedRemainingSeconds);
        }

        if (cachedDurationSeconds <= 0f)
            cachedDurationSeconds = Mathf.Max(boundProgress?.durationSeconds ?? 0f, 0.01f);

        float normalized = cachedDurationSeconds > 0f
            ? Mathf.Clamp01(1f - (cachedRemainingSeconds / cachedDurationSeconds))
            : 0f;

        SetTargetFill(normalized, becameActive);

        if (slotButton != null)
        {
            slotButton.interactable = false;
        }
    }

    private void OnUnlockClicked()
    {
        unlockRequested?.Invoke(slotIndex);
    }

    private void OnSlotClicked()
    {
        if (!canSelectSlot)
            return;

        slotSelected?.Invoke(slotIndex);
    }

    private void Update()
    {
        if (progressFillImage == null)
            return;

        if (fillLerpSpeed <= 0f)
        {
            if (!Mathf.Approximately(progressFillImage.fillAmount, targetFillAmount))
                progressFillImage.fillAmount = targetFillAmount;
            return;
        }

        if (Mathf.Approximately(progressFillImage.fillAmount, targetFillAmount))
            return;

        progressFillImage.fillAmount = Mathf.MoveTowards(progressFillImage.fillAmount, targetFillAmount, fillLerpSpeed * Time.deltaTime);
    }

    private void SetTargetFill(float target, bool immediate)
    {
        targetFillAmount = Mathf.Clamp01(target);

        if (progressFillImage == null)
            return;

        if (immediate || fillLerpSpeed <= 0f)
        {
            progressFillImage.fillAmount = targetFillAmount;
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        var span = TimeSpan.FromSeconds(seconds);
        if (span.TotalDays >= 1)
            return $"{span.Days}d {span.Hours}h";
        if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds}s";
        return $"{span.Seconds}s";
    }
}

public struct ResearchSlotDisplayData
{
    public int SlotIndex;
    public bool IsUnlocked;
    public bool IsActive;
    public bool IsNextUnlock;
    public bool CanAffordUnlock;
    public float UnlockCost;
    public float RemainingSeconds;
    public float DurationSeconds;
    public ResearchProgressData Progress;
    public ResearchDefinition Definition;
    public bool AllowSelection;
}
