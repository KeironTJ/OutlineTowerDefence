using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game time scale for pause and speedup functionality.
/// Only affects in-round gameplay, not research or persistent systems.
/// </summary>
public class TimeScaleManager : SingletonMonoBehaviour<TimeScaleManager>
{
    [Header("Speed Settings")]
    [SerializeField] private float currentSpeed = 1f;
    [SerializeField] private float maxUnlockedSpeed = 1f;
    
    private const float MinSpeed = 0f; // Pause
    private const float NormalSpeed = 1f;
    private const float MaxSpeed = 5f;
    private const float SpeedIncrement = 0.25f;
    
    // Auto-pause settings
    [Header("Auto-Pause")]
    [SerializeField] private bool autoPauseOnOptions = true;
    private bool wasAutoPaused = false;
    private float speedBeforeAutoPause = 1f;
    
    // Events
    public event Action<float> SpeedChanged;
    public event Action Paused;
    public event Action Resumed;
    
    // Properties
    public float CurrentSpeed => currentSpeed;
    public float MaxUnlockedSpeed => maxUnlockedSpeed;
    public bool IsPaused => currentSpeed == 0f;
    public bool AutoPauseOnOptions
    {
        get => autoPauseOnOptions;
        set => autoPauseOnOptions = value;
    }
    
    protected override void OnAwakeAfterInit()
    {
        // Start at normal speed
        currentSpeed = NormalSpeed;
        maxUnlockedSpeed = NormalSpeed;
        
        // Ensure timeScale is reset (in case it was left in a different state)
        Time.timeScale = 1f;
        ApplyTimeScale();
        
        // Load auto-pause setting from PlayerPrefs
        if (PlayerPrefs.HasKey("AutoPauseOnOptions"))
        {
            autoPauseOnOptions = PlayerPrefs.GetInt("AutoPauseOnOptions") == 1;
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to options menu events if they exist
        EventManager.StartListening("OptionsMenuOpened", OnOptionsMenuOpened);
        EventManager.StartListening("OptionsMenuClosed", OnOptionsMenuClosed);
        
        // Subscribe to scene changes to reset time scale
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        EventManager.StopListening("OptionsMenuOpened", OnOptionsMenuOpened);
        EventManager.StopListening("OptionsMenuClosed", OnOptionsMenuClosed);
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Reset time scale when manager is destroyed to avoid issues
        Time.timeScale = 1f;
    }
    
    /// <summary>
    /// Set the maximum unlocked speed (called by research system)
    /// </summary>
    public void SetMaxUnlockedSpeed(float maxSpeed)
    {
        maxUnlockedSpeed = Mathf.Clamp(maxSpeed, NormalSpeed, MaxSpeed);
        
        // If current speed exceeds new max, reduce to max
        if (currentSpeed > maxUnlockedSpeed)
        {
            SetSpeed(maxUnlockedSpeed);
        }
    }
    
    /// <summary>
    /// Get available speed increments
    /// </summary>
    public float[] GetAvailableSpeeds()
    {
        int count = Mathf.RoundToInt(maxUnlockedSpeed / SpeedIncrement);
        float[] speeds = new float[count + 1]; // +1 for pause
        speeds[0] = MinSpeed; // Pause
        for (int i = 1; i <= count; i++)
        {
            speeds[i] = i * SpeedIncrement;
        }
        return speeds;
    }
    
    /// <summary>
    /// Toggle pause
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }
    
    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (!IsPaused)
        {
            SetSpeed(MinSpeed);
            Paused?.Invoke();
        }
    }
    
    /// <summary>
    /// Resume from pause (goes back to 1x speed)
    /// </summary>
    public void Resume()
    {
        if (IsPaused)
        {
            SetSpeed(NormalSpeed);
            Resumed?.Invoke();
        }
    }
    
    /// <summary>
    /// Increase speed by one increment
    /// </summary>
    public void IncreaseSpeed()
    {
        float newSpeed = currentSpeed + SpeedIncrement;
        if (newSpeed <= maxUnlockedSpeed && newSpeed <= MaxSpeed)
        {
            SetSpeed(newSpeed);
        }
    }
    
    /// <summary>
    /// Decrease speed by one increment
    /// </summary>
    public void DecreaseSpeed()
    {
        float newSpeed = currentSpeed - SpeedIncrement;
        if (newSpeed >= MinSpeed)
        {
            SetSpeed(newSpeed);
        }
        else
        {
            // If going below min, pause
            Pause();
        }
    }
    
    /// <summary>
    /// Set speed to specific value (clamped to available range)
    /// </summary>
    public void SetSpeed(float speed)
    {
        // Clamp to valid range
        speed = Mathf.Clamp(speed, MinSpeed, Mathf.Min(maxUnlockedSpeed, MaxSpeed));
        
        // Round to nearest increment
        if (speed > 0)
        {
            speed = Mathf.Round(speed / SpeedIncrement) * SpeedIncrement;
        }
        
        if (Mathf.Abs(currentSpeed - speed) > 0.01f)
        {
            currentSpeed = speed;
            ApplyTimeScale();
            SpeedChanged?.Invoke(currentSpeed);
        }
    }
    
    /// <summary>
    /// Reset to normal speed
    /// </summary>
    public void ResetToNormalSpeed()
    {
        SetSpeed(NormalSpeed);
    }
    
    private void ApplyTimeScale()
    {
        Time.timeScale = currentSpeed;
    }
    
    private void OnOptionsMenuOpened(object data)
    {
        if (autoPauseOnOptions && !IsPaused)
        {
            speedBeforeAutoPause = currentSpeed;
            wasAutoPaused = true;
            Pause();
        }
    }
    
    private void OnOptionsMenuClosed(object data)
    {
        if (wasAutoPaused)
        {
            wasAutoPaused = false;
            SetSpeed(speedBeforeAutoPause);
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset to normal speed when a new scene loads
        // This prevents issues with paused/sped up states carrying over
        ResetToNormalSpeed();
        wasAutoPaused = false;
    }
    
    /// <summary>
    /// Save auto-pause setting
    /// </summary>
    public void SaveAutoPauseSetting()
    {
        PlayerPrefs.SetInt("AutoPauseOnOptions", autoPauseOnOptions ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get speed increment constant
    /// </summary>
    public static float GetSpeedIncrement() => SpeedIncrement;
}
