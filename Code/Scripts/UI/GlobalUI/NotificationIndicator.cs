using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual indicator component that can be attached to UI buttons to show notification count
/// Displays a red dot badge with optional count text
/// </summary>
public class NotificationIndicator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private NotificationSource sourceFilter = NotificationSource.System;
    [SerializeField] private bool showCount = true;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject indicatorObject; // The red dot or badge container
    [SerializeField] private TextMeshProUGUI countText; // Optional count text
    [SerializeField] private Image badgeImage; // Optional badge background image
    
    [Header("Visual Settings")]
    [SerializeField] private Color badgeColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;
    
    private int currentCount;
    private float pulseTimer;

    private void Awake()
    {
        if (badgeImage != null)
            badgeImage.color = badgeColor;
        
        UpdateIndicator();
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.NotificationIndicatorUpdate, OnIndicatorUpdate);
        UpdateIndicator();
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.NotificationIndicatorUpdate, OnIndicatorUpdate);
    }

    private void Update()
    {
        if (enablePulse && indicatorObject != null && indicatorObject.activeSelf)
        {
            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            indicatorObject.transform.localScale = Vector3.one * scale;
        }
    }

    private void OnIndicatorUpdate(object eventData = null)
    {
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (NotificationManager.Instance == null)
        {
            SetVisible(false);
            return;
        }

        currentCount = NotificationManager.Instance.GetPendingCount(sourceFilter);
        
        bool hasNotifications = currentCount > 0;
        SetVisible(hasNotifications);

        if (hasNotifications && showCount && countText != null)
        {
            // Format count (show "9+" if over 9 for compact display)
            countText.text = currentCount > 9 ? "9+" : currentCount.ToString();
        }
    }

    private void SetVisible(bool visible)
    {
        if (indicatorObject != null)
            indicatorObject.SetActive(visible);
    }

    /// <summary>
    /// Manually set the source filter for this indicator
    /// </summary>
    public void SetSourceFilter(NotificationSource source)
    {
        sourceFilter = source;
        UpdateIndicator();
    }

    /// <summary>
    /// Get the current notification count
    /// </summary>
    public int GetCount()
    {
        return currentCount;
    }
}
