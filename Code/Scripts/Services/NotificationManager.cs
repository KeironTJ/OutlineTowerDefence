using System.Collections.Generic;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int maxQueueSize = 50;
    [SerializeField] private bool enableDebugLogs = false;

    private readonly Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private readonly Dictionary<NotificationSource, int> pendingNotificationsBySource = new Dictionary<NotificationSource, int>();
    
    private NotificationData currentNotification;
    private bool isProcessing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Listen for events that should trigger notifications
        EventManager.StartListening(EventNames.AchievementTierCompleted, OnAchievementTierCompleted);
        EventManager.StartListening(EventNames.ChipUnlocked, OnChipUnlocked);
        EventManager.StartListening(EventNames.SkillUnlocked, OnSkillUnlocked);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.AchievementTierCompleted, OnAchievementTierCompleted);
        EventManager.StopListening(EventNames.ChipUnlocked, OnChipUnlocked);
        EventManager.StopListening(EventNames.SkillUnlocked, OnSkillUnlocked);
    }

    /// <summary>
    /// Queue a notification to be displayed
    /// </summary>
    public void QueueNotification(NotificationData notification)
    {
        if (notification == null)
        {
            Debug.LogWarning("[NotificationManager] Attempted to queue null notification");
            return;
        }

        if (notificationQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning("[NotificationManager] Notification queue full, removing oldest");
            notificationQueue.Dequeue();
        }

        notificationQueue.Enqueue(notification);
        
        // Track pending notifications by source
        if (!pendingNotificationsBySource.ContainsKey(notification.source))
            pendingNotificationsBySource[notification.source] = 0;
        pendingNotificationsBySource[notification.source]++;

        UpdateIndicators();

        if (enableDebugLogs)
            Debug.Log($"[NotificationManager] Queued notification: {notification.title}");

        // Trigger event for UI to listen to
        EventManager.TriggerEvent(EventNames.NotificationTriggered, notification);

        // Process immediately if not already processing
        if (!isProcessing)
            ProcessNextNotification();
    }

    /// <summary>
    /// Quick helper to create and queue a simple notification
    /// </summary>
    public void ShowQuickNotification(string title, string description, NotificationSource source = NotificationSource.System, float duration = 3f)
    {
        var notification = new NotificationData(
            NotificationType.Quick,
            title,
            description,
            NotificationPriority.Normal,
            duration,
            null,
            "",
            source
        );
        QueueNotification(notification);
    }

    /// <summary>
    /// Quick helper to create and queue a modal notification with rewards
    /// </summary>
    public void ShowModalNotification(string title, string description, NotificationReward[] rewards, NotificationSource source, string sourceId = "")
    {
        var notification = new NotificationData(
            NotificationType.Modal,
            title,
            description,
            NotificationPriority.High,
            0f,
            rewards,
            sourceId,
            source
        );
        QueueNotification(notification);
    }

    /// <summary>
    /// Marks the current notification as dismissed and processes the next one
    /// </summary>
    public void DismissCurrentNotification()
    {
        if (currentNotification != null)
        {
            // Decrease pending count for this source
            if (pendingNotificationsBySource.ContainsKey(currentNotification.source))
            {
                pendingNotificationsBySource[currentNotification.source]--;
                if (pendingNotificationsBySource[currentNotification.source] <= 0)
                    pendingNotificationsBySource.Remove(currentNotification.source);
            }

            UpdateIndicators();
            EventManager.TriggerEvent(EventNames.NotificationDismissed, currentNotification);
            
            if (enableDebugLogs)
                Debug.Log($"[NotificationManager] Dismissed notification: {currentNotification.title}");
            
            currentNotification = null;
        }

        isProcessing = false;
        ProcessNextNotification();
    }

    /// <summary>
    /// Get the count of pending notifications for a specific source
    /// </summary>
    public int GetPendingCount(NotificationSource source)
    {
        return pendingNotificationsBySource.ContainsKey(source) ? pendingNotificationsBySource[source] : 0;
    }

    /// <summary>
    /// Get the total count of pending notifications
    /// </summary>
    public int GetTotalPendingCount()
    {
        return notificationQueue.Count + (currentNotification != null ? 1 : 0);
    }

    /// <summary>
    /// Clear all pending notifications (use carefully)
    /// </summary>
    public void ClearAllNotifications()
    {
        notificationQueue.Clear();
        pendingNotificationsBySource.Clear();
        currentNotification = null;
        isProcessing = false;
        UpdateIndicators();
    }

    private void ProcessNextNotification()
    {
        if (isProcessing || notificationQueue.Count == 0)
            return;

        isProcessing = true;
        currentNotification = notificationQueue.Dequeue();

        if (enableDebugLogs)
            Debug.Log($"[NotificationManager] Processing notification: {currentNotification.title}");

        // The UI will handle the actual display
        // For quick notifications, we could auto-dismiss after duration
        if (currentNotification.type == NotificationType.Quick && currentNotification.displayDuration > 0)
        {
            Invoke(nameof(DismissCurrentNotification), currentNotification.displayDuration);
        }
    }

    private void UpdateIndicators()
    {
        // Trigger event to update UI indicators
        EventManager.TriggerEvent(EventNames.NotificationIndicatorUpdate);
    }

    // Event handlers for auto-generating notifications

    private void OnAchievementTierCompleted(object eventData)
    {
        if (eventData is AchievementTierCompletedEvent tierEvent)
        {
            ShowQuickNotification(
                "Achievement Progress!",
                $"{tierEvent.tierName} completed!",
                NotificationSource.Achievement,
                3f
            );
        }
    }

    private void OnChipUnlocked(object eventData)
    {
        if (eventData is ChipUnlockedEvent chipEvent)
        {
            var rewards = new NotificationReward[]
            {
                new NotificationReward(NotificationRewardType.UnlockChip, chipEvent.chipId)
            };

            ShowModalNotification(
                "New Chip Unlocked!",
                $"You've unlocked {chipEvent.chipName}",
                rewards,
                NotificationSource.Chip,
                chipEvent.chipId
            );
        }
    }

    private void OnSkillUnlocked(object eventData)
    {
        if (eventData is SkillUnlockedEvent skillEvent)
        {
            ShowQuickNotification(
                "Skill Unlocked!",
                $"New skill available: {skillEvent.skillId}",
                NotificationSource.Skill,
                3f
            );
        }
    }
}
