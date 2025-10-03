using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPanelUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Progress")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tierStatusText;
    [SerializeField] private TextMeshProUGUI currentTierText;
    [SerializeField] private TextMeshProUGUI nextTierText;
    [SerializeField] private TextMeshProUGUI nextRewardText;
    [SerializeField] private GameObject completionBadge;

    [Header("Tier Summary")]
    [SerializeField] private TextMeshProUGUI tierListText;

    private AchievementRuntime runtime;

    public void Bind(AchievementRuntime rt)
    {
        runtime = rt;
        var def = runtime.definition;

        if (iconImage)
        {
            iconImage.sprite = def.icon;
            iconImage.enabled = def.icon != null;
        }

        if (titleText) titleText.text = def.displayName;
        if (categoryText) categoryText.text = def.category.ToString();
        if (descriptionText) descriptionText.text = def.description;

        Refresh();
    }

    public void Refresh()
    {
        if (runtime == null) return;

        var def = runtime.definition;
        var nextTier = runtime.GetNextUncompletedTier();
        var currentTier = runtime.HighestTierCompleted >= 0 && def.tiers != null && def.tiers.Length > 0
            ? def.tiers[Mathf.Clamp(runtime.HighestTierCompleted, 0, def.tiers.Length - 1)]
            : null;

        float progressFraction = Mathf.Clamp01(runtime.GetProgressToNextTier());

        if (progressSlider)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = progressFraction;
            progressSlider.gameObject.SetActive(nextTier != null);
        }

        if (progressText)
        {
            if (nextTier == null)
            {
                progressText.text = "All tiers complete";
            }
            else
            {
                progressText.text =
                    $"{NumberManager.FormatLargeNumber(runtime.Current, true)}/" +
                    NumberManager.FormatLargeNumber(nextTier.targetAmount, true);
            }
        }

        if (tierStatusText)
        {
            int completed = Mathf.Clamp(runtime.HighestTierCompleted + 1, 0, def.tiers?.Length ?? 0);
            int total = def.tiers?.Length ?? 0;
            tierStatusText.text = total > 0 ? $"{completed}/{total} tiers complete" : "No tiers configured";
        }

        if (currentTierText)
        {
            currentTierText.text = currentTier != null ? currentTier.tierName : "No tier complete";
        }

        if (nextTierText)
        {
            nextTierText.text = nextTier != null ? nextTier.tierName : "Max tier reached";
        }

        if (nextRewardText)
        {
            nextRewardText.text = nextTier != null
                ? BuildRewardSummary(nextTier.rewards)
                : "All rewards claimed";
        }

        if (completionBadge)
        {
            completionBadge.SetActive(nextTier == null);
        }

        UpdateTierListing();
    }

    private void UpdateTierListing()
    {
        if (tierListText == null || runtime == null) return;

        var def = runtime.definition;
        if (def.tiers == null || def.tiers.Length == 0)
        {
            tierListText.text = "No tiers configured.";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < def.tiers.Length; i++)
        {
            var tier = def.tiers[i];
            bool completed = runtime.HighestTierCompleted >= i;
            string statusGlyph = completed ? "<color=#4CAF50>✔</color>" : "<color=#888888>○</color>";

            sb.Append(statusGlyph)
              .Append(' ')
              .Append(string.IsNullOrEmpty(tier.tierName) ? $"Tier {tier.tierLevel}" : tier.tierName)
              .Append(" — ")
              .Append(NumberManager.FormatLargeNumber(tier.targetAmount, true));

            if (!string.IsNullOrEmpty(tier.tierDescription))
            {
                sb.AppendLine()
                  .Append("   <size=90%>")
                  .Append(tier.tierDescription)
                  .Append("</size>");
            }

            if (tier.rewards != null && tier.rewards.Length > 0)
            {
                sb.AppendLine()
                  .Append("   <size=85%>")
                  .Append(BuildRewardSummary(tier.rewards))
                  .Append("</size>");
            }

            if (i < def.tiers.Length - 1)
                sb.AppendLine();
        }

        tierListText.text = sb.ToString();
    }

    private string BuildRewardSummary(AchievementReward[] rewards)
    {
        if (rewards == null || rewards.Length == 0) return "—";

        var parts = new List<string>(rewards.Length);
        foreach (var reward in rewards)
        {
            switch (reward.rewardType)
            {
                case AchievementRewardType.Currency:
                    string amount = NumberManager.FormatLargeNumber(reward.amount, false);
                    parts.Add($"{amount} {reward.currencyType}");
                    break;
                case AchievementRewardType.UnlockTurret:
                    parts.Add($"Unlock Turret: {reward.rewardId}");
                    break;
                case AchievementRewardType.UnlockProjectile:
                    parts.Add($"Unlock Projectile: {reward.rewardId}");
                    break;
                case AchievementRewardType.UnlockTowerBase:
                    parts.Add($"Unlock Tower Base: {reward.rewardId}");
                    break;
                case AchievementRewardType.StatBonus:
                    parts.Add($"Stat Bonus: {reward.rewardId} (+{reward.amount})");
                    break;
                default:
                    parts.Add("Reward configured");
                    break;
            }
        }

        return string.Join(", ", parts);
    }
}
