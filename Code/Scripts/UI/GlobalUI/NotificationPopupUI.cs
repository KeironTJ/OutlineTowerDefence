using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick notification that auto-dismisses after a short duration
/// </summary>
public class NotificationPopupUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject container;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";
    
    [Header("Fallback (if no animator)")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private float slideOutDuration = 0.3f;
    [SerializeField] private Vector2 hiddenPosition = new Vector2(400, 0);
    
    private NotificationData currentNotification;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isAnimating;
    private Coroutine activeAnimation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            originalPosition = rectTransform.anchoredPosition;
        
        if (container != null)
            container.SetActive(false);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.NotificationTriggered, OnNotificationTriggered);
        EventManager.StartListening(EventNames.NotificationDismissed, OnNotificationDismissed);
        Debug.Log("[NotificationPopupUI] Listening for notifications");

    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.NotificationTriggered, OnNotificationTriggered);
        EventManager.StopListening(EventNames.NotificationDismissed, OnNotificationDismissed);
        Debug.Log("[NotificationPopupUI] Stopped listening for notifications");
    }

    private void OnNotificationTriggered(object eventData)
    {
        if (eventData is NotificationData notification)
        {
            // Only handle quick notifications
            if (notification.type == NotificationType.Quick)
                Show(notification);
        }
    }

    private void OnNotificationDismissed(object eventData)
    {
        if (eventData is NotificationData notification && notification == currentNotification)
            Hide();
    }

    public void Show(NotificationData notification)
    {
        CancelInvoke(nameof(HideContainer));

        if (animator == null)
            StopActiveAnimation();

        currentNotification = notification;

        if (titleText != null)
            titleText.text = notification.title;
        
        if (descriptionText != null)
            descriptionText.text = notification.description;

        if (iconImage != null)
            iconImage.gameObject.SetActive(iconImage.sprite != null);

        if (container != null)
            container.SetActive(true);

        if (animator != null && !string.IsNullOrEmpty(showTrigger))
        {
            animator.ResetTrigger(hideTrigger);
            animator.SetTrigger(showTrigger);
        }
        else
        {
            if (rectTransform != null)
                rectTransform.anchoredPosition = originalPosition + hiddenPosition;

            activeAnimation = StartCoroutine(AnimateSlideIn());
        }
    }

    public void Hide()
    {
        CancelInvoke(nameof(HideContainer));

        if (animator != null && !string.IsNullOrEmpty(hideTrigger))
        {
            animator.ResetTrigger(showTrigger);
            animator.SetTrigger(hideTrigger);
            Invoke(nameof(HideContainer), slideOutDuration);
        }
        else
        {
            StopActiveAnimation();
            activeAnimation = StartCoroutine(AnimateSlideOut());
        }

        currentNotification = null;
    }

    private void HideContainer()
    {
        if (container != null)
            container.SetActive(false);
    }

    private void StopActiveAnimation()
    {
        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        isAnimating = false;
    }

    private System.Collections.IEnumerator AnimateSlideIn()
    {
        if (rectTransform == null)
            yield break;
        
        isAnimating = true;
        float elapsed = 0f;
        Vector2 startPos = originalPosition + hiddenPosition;
        
        while (elapsed < slideInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideInDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = originalPosition;
        isAnimating = false;
        activeAnimation = null;
    }

    private System.Collections.IEnumerator AnimateSlideOut()
    {
        if (rectTransform == null)
            yield break;
        
        isAnimating = true;
        float elapsed = 0f;
        Vector2 endPos = originalPosition + hiddenPosition;
        
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideOutDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(originalPosition, endPos, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
        HideContainer();
        isAnimating = false;
        activeAnimation = null;
    }

    // Can be called from UI button if user wants to dismiss early
    public void DismissEarly()
    {
        if (currentNotification != null && NotificationManager.Instance != null)
            NotificationManager.Instance.DismissCurrentNotification();
    }
}
