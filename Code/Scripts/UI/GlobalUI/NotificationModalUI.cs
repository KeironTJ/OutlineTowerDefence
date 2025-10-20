using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal notification that requires user interaction (claim rewards, close, etc.)
/// </summary>
public class NotificationModalUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Image iconImage;
    
    [Header("Buttons")]
    [SerializeField] private Button claimButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    
    [Header("Rewards Display")]
    [SerializeField] private GameObject rewardsContainer;
    
    private NotificationData currentNotification;
    private bool rewardsClaimed;

    private void Awake()
    {
        if (modalPanel != null)
            modalPanel.SetActive(false);

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimButtonPressed);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonPressed);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.NotificationTriggered, OnNotificationTriggered);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.NotificationTriggered, OnNotificationTriggered);
    }

    private void OnNotificationTriggered(object eventData)
    {
        if (eventData is NotificationData notification)
        {
            // Only handle modal notifications
            if (notification.type == NotificationType.Modal)
            {
                Show(notification);
            }
        }
    }

    public void Show(NotificationData notification)
    {
        currentNotification = notification;
        rewardsClaimed = false;

        // Set title
        if (titleText != null)
            titleText.text = notification.title;
        
        // Set description
        if (descriptionText != null)
            descriptionText.text = notification.description;

        // Build and display rewards
        bool hasRewards = notification.rewards != null && notification.rewards.Length > 0;
        
        if (rewardsContainer != null)
            rewardsContainer.SetActive(hasRewards);

        if (hasRewards && rewardsText != null)
        {
            rewardsText.text = BuildRewardsSummary(notification.rewards);
        }

        // Set icon based on source
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }

        // Configure buttons
        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(hasRewards);
            claimButton.interactable = true;
        }

        if (claimButtonText != null)
        {
            claimButtonText.text = hasRewards ? "Claim" : "OK";
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }

        // Show modal
        if (modalPanel != null)
            modalPanel.SetActive(true);
    }

    public void Hide()
    {
        if (modalPanel != null)
            modalPanel.SetActive(false);

        currentNotification = null;
    }

    private void OnClaimButtonPressed()
    {
        if (currentNotification == null || rewardsClaimed)
            return;

        // Grant rewards
        if (currentNotification.rewards != null && currentNotification.rewards.Length > 0)
        {
            GrantRewards(currentNotification.rewards);
            rewardsClaimed = true;
        }

        // Dismiss and close
        DismissAndClose();
    }

    private void OnCloseButtonPressed()
    {
        // If there are unclaimed rewards, ask for confirmation or auto-claim
        if (currentNotification != null && !rewardsClaimed && 
            currentNotification.rewards != null && currentNotification.rewards.Length > 0)
        {
            // Auto-claim on close for better UX
            GrantRewards(currentNotification.rewards);
            rewardsClaimed = true;
        }

        DismissAndClose();
    }

    private void DismissAndClose()
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.DismissCurrentNotification();
        }
        
        Hide();
    }

    private void GrantRewards(NotificationReward[] rewards)
    {
        var playerManager = PlayerManager.main;
        if (playerManager == null)
        {
            Debug.LogWarning("[NotificationModalUI] PlayerManager not available to grant rewards");
            return;
        }

        foreach (var reward in rewards)
        {
            switch (reward.rewardType)
            {
                case NotificationRewardType.Currency:
                    // Grant currency through the player's wallet
                    var wallet = playerManager.GetComponent<PlayerCurrencyWallet>();
                    if (wallet != null)
                    {
                        wallet.AddCurrency(reward.currencyType, reward.amount);
                    }
                    break;

                case NotificationRewardType.UnlockChip:
                    // Chip is already unlocked, just acknowledge
                    Debug.Log($"[NotificationModalUI] Acknowledged chip unlock: {reward.rewardId}");
                    break;

                case NotificationRewardType.UnlockTurret:
                    // Turret is already unlocked, just acknowledge
                    Debug.Log($"[NotificationModalUI] Acknowledged turret unlock: {reward.rewardId}");
                    break;

                case NotificationRewardType.UnlockProjectile:
                    // Projectile is already unlocked, just acknowledge
                    Debug.Log($"[NotificationModalUI] Acknowledged projectile unlock: {reward.rewardId}");
                    break;

                case NotificationRewardType.UnlockSkill:
                    // Skill is already unlocked, just acknowledge
                    Debug.Log($"[NotificationModalUI] Acknowledged skill unlock: {reward.rewardId}");
                    break;

                case NotificationRewardType.UnlockTowerBase:
                    // Tower base is already unlocked, just acknowledge
                    Debug.Log($"[NotificationModalUI] Acknowledged tower base unlock: {reward.rewardId}");
                    break;
            }
        }
    }

    private string BuildRewardsSummary(NotificationReward[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("Rewards:");

        foreach (var reward in rewards)
        {
            switch (reward.rewardType)
            {
                case NotificationRewardType.Currency:
                    sb.AppendLine($"• {NumberManager.FormatLargeNumber(reward.amount, false)} {reward.currencyType}");
                    break;
                case NotificationRewardType.UnlockTurret:
                    sb.AppendLine($"• Turret: {reward.rewardId}");
                    break;
                case NotificationRewardType.UnlockProjectile:
                    sb.AppendLine($"• Projectile: {reward.rewardId}");
                    break;
                case NotificationRewardType.UnlockChip:
                    sb.AppendLine($"• Chip: {reward.rewardId}");
                    break;
                case NotificationRewardType.UnlockSkill:
                    sb.AppendLine($"• Skill: {reward.rewardId}");
                    break;
                case NotificationRewardType.UnlockTowerBase:
                    sb.AppendLine($"• Tower Base: {reward.rewardId}");
                    break;
            }
        }

        return sb.ToString().TrimEnd();
    }
}
