using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI view for a single research card
/// Displays research information, progress, and allows interaction
/// </summary>
public class ResearchCardView : MonoBehaviour
{
    [Header("UI References")]
    public Button actionButton;
    public Image icon;
    public TextMeshProUGUI researchNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI nextValueText;
    public TextMeshProUGUI timeRemainingText;
    public TextMeshProUGUI costText;
    public GameObject lockedPanel;
    public GameObject researchingPanel;
    public Image progressBar;
    public Button speedUpButton;
    public Button instantCompleteButton;
    
    private string researchId;
    private ResearchDefinition definition;
    private ResearchProgressData progress;
    
    private void Reset()
    {
        actionButton = GetComponent<Button>();
    }
    
    public void Bind(string id)
    {
        researchId = id;
        
        if (ResearchService.Instance == null)
        {
            Debug.LogWarning("[ResearchCardView] ResearchService not found");
            return;
        }
        
        definition = ResearchService.Instance.GetDefinition(researchId);
        progress = ResearchService.Instance.GetProgress(researchId);
        
        RefreshDisplay();
    }
    
    public void RefreshDisplay()
    {
        if (definition == null)
        {
            SetEmptyState();
            return;
        }
        
        // Basic info
        if (icon != null)
        {
            icon.sprite = definition.icon;
            icon.enabled = definition.icon != null;
        }
        
        if (researchNameText != null)
            researchNameText.text = definition.displayName;
        
        if (descriptionText != null)
            descriptionText.text = definition.description;
        
        // Level display
        int currentLevel = progress?.currentLevel ?? 0;
        int maxLevel = definition.maxLevel;
        
        if (levelText != null)
        {
            if (currentLevel >= maxLevel)
                levelText.text = $"Level MAX ({currentLevel}/{maxLevel})";
            else
                levelText.text = $"Level {currentLevel}/{maxLevel}";
        }
        
        // Check if available
        bool isAvailable = ResearchService.Instance.IsResearchAvailable(researchId);
        bool isResearching = progress != null && progress.isResearching;
        bool isMaxed = currentLevel >= maxLevel;
        
        // Locked/Available state
        if (lockedPanel != null)
            lockedPanel.SetActive(!isAvailable);
        
        if (researchingPanel != null)
            researchingPanel.SetActive(isResearching);
        
        // Values display (for BaseStat type)
        UpdateValueDisplay();
        
        // Time and progress
        if (isResearching)
        {
            UpdateResearchProgress();
        }
        else
        {
            if (timeRemainingText != null)
                timeRemainingText.text = "";
            if (progressBar != null)
                progressBar.fillAmount = 0f;
        }
        
        // Cost display
        if (costText != null)
        {
            if (isMaxed)
            {
                costText.text = "MAXED";
            }
            else if (isResearching)
            {
                costText.text = "Researching...";
            }
            else if (isAvailable)
            {
                int nextLevel = currentLevel + 1;
                float coreCost = definition.GetCoreCostForLevel(nextLevel);
                float timeSeconds = definition.GetTimeForLevel(nextLevel);
                costText.text = $"Cores: {coreCost:F0} | Time: {FormatTime(timeSeconds)}";
            }
            else
            {
                costText.text = "Locked";
            }
        }
        
        // Button states
        UpdateButtonStates(isAvailable, isResearching, isMaxed);
    }
    
    private void UpdateValueDisplay()
    {
        if (definition.researchType != ResearchType.BaseStat || definition.statBonuses == null || definition.statBonuses.Length == 0)
        {
            if (currentValueText != null)
                currentValueText.gameObject.SetActive(false);
            if (nextValueText != null)
                nextValueText.gameObject.SetActive(false);
            return;
        }
        
        int currentLevel = progress?.currentLevel ?? 0;
        int nextLevel = currentLevel + 1;
        
        // Show first stat bonus as example
        var bonus = definition.statBonuses[0];
        float currentValue = bonus.value * currentLevel;
        float nextValue = bonus.value * nextLevel;
        
        if (currentValueText != null)
        {
            currentValueText.gameObject.SetActive(true);
            currentValueText.text = $"Current: {FormatStatValue(bonus, currentValue)}";
        }
        
        if (nextValueText != null)
        {
            if (nextLevel <= definition.maxLevel)
            {
                nextValueText.gameObject.SetActive(true);
                nextValueText.text = $"Next: {FormatStatValue(bonus, nextValue)}";
            }
            else
            {
                nextValueText.gameObject.SetActive(false);
            }
        }
    }
    
    private string FormatStatValue(StatBonus bonus, float value)
    {
        string statName = bonus.targetStat.ToString();
        
        switch (bonus.contributionKind)
        {
            case SkillContributionKind.Percentage:
                return $"{statName} +{value:F1}%";
            case SkillContributionKind.Multiplier:
                return $"{statName} x{value:F2}";
            default:
                return $"{statName} +{value:F1}";
        }
    }
    
    private void UpdateResearchProgress()
    {
        if (ResearchService.Instance == null || progress == null || !progress.isResearching)
            return;
        
        float remaining = ResearchService.Instance.GetRemainingTime(researchId);
        float total = progress.durationSeconds;
        
        if (timeRemainingText != null)
            timeRemainingText.text = FormatTime(remaining);
        
        if (progressBar != null)
        {
            float fillAmount = total > 0 ? Mathf.Clamp01(1f - (remaining / total)) : 0f;
            progressBar.fillAmount = fillAmount;
        }
    }
    
    private void UpdateButtonStates(bool isAvailable, bool isResearching, bool isMaxed)
    {
        if (actionButton != null)
        {
            actionButton.interactable = isAvailable && !isResearching && !isMaxed;
            var buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (isMaxed)
                    buttonText.text = "MAXED";
                else if (isResearching)
                    buttonText.text = "Researching";
                else if (isAvailable)
                    buttonText.text = "Start Research";
                else
                    buttonText.text = "Locked";
            }
        }
        
        if (speedUpButton != null)
        {
            speedUpButton.gameObject.SetActive(isResearching);
            speedUpButton.interactable = isResearching;
        }
        
        if (instantCompleteButton != null)
        {
            instantCompleteButton.gameObject.SetActive(isResearching);
            instantCompleteButton.interactable = isResearching;
        }
    }
    
    private void SetEmptyState()
    {
        if (researchNameText != null)
            researchNameText.text = "Unknown Research";
        if (descriptionText != null)
            descriptionText.text = "";
        if (levelText != null)
            levelText.text = "";
        if (currentValueText != null)
            currentValueText.text = "";
        if (nextValueText != null)
            nextValueText.text = "";
        if (timeRemainingText != null)
            timeRemainingText.text = "";
        if (costText != null)
            costText.text = "";
        if (lockedPanel != null)
            lockedPanel.SetActive(true);
        if (researchingPanel != null)
            researchingPanel.SetActive(false);
        if (actionButton != null)
            actionButton.interactable = false;
    }
    
    public void OnActionButtonClicked()
    {
        if (ResearchService.Instance == null || string.IsNullOrEmpty(researchId))
            return;
        
        if (ResearchService.Instance.TryStartResearch(researchId))
        {
            RefreshDisplay();
        }
        else
        {
            Debug.Log($"[ResearchCardView] Cannot start research: {researchId}");
        }
    }
    
    public void OnSpeedUpButtonClicked()
    {
        if (ResearchService.Instance == null || string.IsNullOrEmpty(researchId))
            return;
        
        // Speed up by 1 hour
        float speedUpSeconds = 3600f;
        
        if (ResearchService.Instance.TrySpeedUpResearch(researchId, speedUpSeconds))
        {
            RefreshDisplay();
        }
    }
    
    public void OnInstantCompleteButtonClicked()
    {
        if (ResearchService.Instance == null || string.IsNullOrEmpty(researchId))
            return;
        
        if (ResearchService.Instance.TryInstantCompleteResearch(researchId))
        {
            RefreshDisplay();
        }
    }
    
    private void Update()
    {
        // Update progress display if researching
        if (progress != null && progress.isResearching)
        {
            UpdateResearchProgress();
        }
    }
    
    private string FormatTime(float seconds)
    {
        if (seconds < 0) seconds = 0;
        
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        
        if (span.TotalDays >= 1)
            return $"{span.Days}d {span.Hours}h";
        else if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m";
        else if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds}s";
        else
            return $"{span.Seconds}s";
    }
}
