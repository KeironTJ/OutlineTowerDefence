using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for controlling game speed and pause
/// </summary>
public class SpeedControlUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button decreaseSpeedButton;
    [SerializeField] private Button increaseSpeedButton;
    [SerializeField] private TextMeshProUGUI speedDisplayText;
    
    [Header("Icons/Sprites (Optional)")]
    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private Sprite playIcon;
    
    private TimeScaleManager timeScaleManager;
    
    private void Start()
    {
        timeScaleManager = TimeScaleManager.Instance;
        
        if (timeScaleManager == null)
        {
            Debug.LogError("SpeedControlUI: TimeScaleManager instance not found!");
            enabled = false;
            return;
        }
        
        // Setup button listeners
        if (pauseButton) pauseButton.onClick.AddListener(OnPauseButtonClicked);
        if (decreaseSpeedButton) decreaseSpeedButton.onClick.AddListener(OnDecreaseSpeedClicked);
        if (increaseSpeedButton) increaseSpeedButton.onClick.AddListener(OnIncreaseSpeedClicked);
        
        // Subscribe to speed changes
        if (timeScaleManager != null)
        {
            timeScaleManager.SpeedChanged += OnSpeedChanged;
        }
        
        UpdateUI();
    }
    
    private void OnDestroy()
    {
        // Cleanup button listeners
        if (pauseButton) pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        if (decreaseSpeedButton) decreaseSpeedButton.onClick.RemoveListener(OnDecreaseSpeedClicked);
        if (increaseSpeedButton) increaseSpeedButton.onClick.RemoveListener(OnIncreaseSpeedClicked);
        
        // Unsubscribe from events
        if (timeScaleManager != null)
        {
            timeScaleManager.SpeedChanged -= OnSpeedChanged;
        }
    }
    
    private void OnPauseButtonClicked()
    {
        if (timeScaleManager == null) return;
        timeScaleManager.TogglePause();
    }
    
    private void OnDecreaseSpeedClicked()
    {
        if (timeScaleManager == null) return;
        timeScaleManager.DecreaseSpeed();
    }
    
    private void OnIncreaseSpeedClicked()
    {
        if (timeScaleManager == null) return;
        timeScaleManager.IncreaseSpeed();
    }
    
    private void OnSpeedChanged(float newSpeed)
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (timeScaleManager == null) return;
        
        float currentSpeed = timeScaleManager.CurrentSpeed;
        float maxSpeed = timeScaleManager.MaxUnlockedSpeed;
        bool isPaused = timeScaleManager.IsPaused;
        
        // Update speed display text
        if (speedDisplayText != null)
        {
            if (isPaused)
            {
                speedDisplayText.text = "PAUSED";
            }
            else
            {
                speedDisplayText.text = $"{currentSpeed:F2}x";
            }
        }
        
        // Update pause button icon/text
        if (pauseButton != null)
        {
            var image = pauseButton.GetComponent<Image>();
            if (image != null && pauseIcon != null && playIcon != null)
            {
                image.sprite = isPaused ? playIcon : pauseIcon;
            }
            else
            {
                var buttonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isPaused ? "Resume" : "Pause";
                }
            }
        }
        
        // Enable/disable speed buttons based on current state
        if (decreaseSpeedButton != null)
        {
            // Can decrease if not at minimum and not paused
            bool canDecrease = currentSpeed > TimeScaleManager.GetSpeedIncrement() || isPaused;
            decreaseSpeedButton.interactable = canDecrease;
        }
        
        if (increaseSpeedButton != null)
        {
            // Can increase if not at max unlocked speed
            bool canIncrease = currentSpeed < maxSpeed;
            increaseSpeedButton.interactable = canIncrease;
        }
    }
}
