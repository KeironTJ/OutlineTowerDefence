using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component to automatically set up notification indicators on UI buttons
/// Can be added to buttons in the inspector or at runtime
/// </summary>
[RequireComponent(typeof(NotificationIndicator))]
public class NotificationButtonHelper : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    [SerializeField] private bool autoCreateBadge = true;
    [SerializeField] private NotificationSource notificationSource = NotificationSource.System;
    
    [Header("Badge Prefab (optional)")]
    [SerializeField] private GameObject badgePrefab; // Optional prefab for the badge
    
    [Header("Auto-Creation Settings")]
    [SerializeField] private Vector2 badgePosition = new Vector2(40, 40); // Top-right corner
    [SerializeField] private Vector2 badgeSize = new Vector2(30, 30);
    [SerializeField] private Color badgeColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    
    private NotificationIndicator indicator;
    
    private void Awake()
    {
        SetupIndicator();
    }
    
    private void OnEnable()
    {
        // Ensure indicator is set up when enabled
        if (indicator == null)
            SetupIndicator();
    }
    
    /// <summary>
    /// Set up the notification indicator component
    /// </summary>
    private void SetupIndicator()
    {
        indicator = GetComponent<NotificationIndicator>();
        if (indicator == null)
        {
            indicator = gameObject.AddComponent<NotificationIndicator>();
        }
        
        // Set the source filter
        indicator.SetSourceFilter(notificationSource);
        
        // Auto-create badge if needed
        if (autoCreateBadge)
        {
            CreateOrFindBadge();
        }
    }
    
    /// <summary>
    /// Create or find the badge indicator object
    /// </summary>
    private void CreateOrFindBadge()
    {
        // First check if a badge already exists
        Transform existingBadge = transform.Find("NotificationBadge");
        if (existingBadge != null)
        {
            Debug.Log($"[NotificationButtonHelper] Found existing badge on {gameObject.name}");
            return;
        }
        
        GameObject badge;
        
        if (badgePrefab != null)
        {
            // Use provided prefab
            badge = Instantiate(badgePrefab, transform);
            badge.name = "NotificationBadge";
        }
        else
        {
            // Create a simple badge programmatically
            badge = CreateSimpleBadge();
        }
        
        // Position the badge
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        if (badgeRect != null)
        {
            badgeRect.anchorMin = new Vector2(1, 1); // Top-right
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = badgePosition;
            badgeRect.sizeDelta = badgeSize;
        }
        
        // Assign to indicator (this will be done through reflection or public method)
        AssignBadgeToIndicator(badge);
    }
    
    /// <summary>
    /// Create a simple circular badge with count text
    /// </summary>
    private GameObject CreateSimpleBadge()
    {
        GameObject badge = new GameObject("NotificationBadge");
        badge.transform.SetParent(transform, false);
        
        // Add RectTransform
        RectTransform rect = badge.AddComponent<RectTransform>();
        rect.sizeDelta = badgeSize;
        
        // Add Image component for the red circle
        Image badgeImage = badge.AddComponent<Image>();
        badgeImage.color = badgeColor;
        
        // Try to use a circular sprite if available, otherwise use filled circle
        // You can assign a sprite in the inspector later
        badgeImage.sprite = null; // Will show as solid color
        badgeImage.type = Image.Type.Filled;
        
        // Add CanvasGroup for fading if needed
        badge.AddComponent<CanvasGroup>();
        
        // Create count text child
        GameObject textObj = new GameObject("CountText");
        textObj.transform.SetParent(badge.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "0";
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 18;
        text.color = Color.white;
        text.fontStyle = TMPro.FontStyles.Bold;
        
        return badge;
    }
    
    /// <summary>
    /// Assign the badge to the indicator component
    /// </summary>
    private void AssignBadgeToIndicator(GameObject badge)
    {
        if (indicator == null) return;
        
        // Use reflection to set the indicator's badge object
        var indicatorType = indicator.GetType();
        var indicatorField = indicatorType.GetField("indicatorObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (indicatorField != null)
        {
            indicatorField.SetValue(indicator, badge);
        }
        
        // Also try to find and assign the count text
        var countText = badge.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (countText != null)
        {
            var countTextField = indicatorType.GetField("countText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (countTextField != null)
            {
                countTextField.SetValue(indicator, countText);
            }
        }
    }
    
    /// <summary>
    /// Public method to change the notification source at runtime
    /// </summary>
    public void SetNotificationSource(NotificationSource source)
    {
        notificationSource = source;
        if (indicator != null)
        {
            indicator.SetSourceFilter(source);
        }
    }
    
    /// <summary>
    /// Get the current notification count
    /// </summary>
    public int GetNotificationCount()
    {
        return indicator != null ? indicator.GetCount() : 0;
    }
}
