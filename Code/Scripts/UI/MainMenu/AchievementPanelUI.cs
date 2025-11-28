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
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [SerializeField] private GameObject claimIndicator;

    [Header("Ready State Visuals")]
    [SerializeField] private Image readyBackground;
    [SerializeField] private Color readyBackgroundColor = new(0.18f, 0.22f, 0.32f, 1f);
    [SerializeField] private Graphic[] readyAccentGraphics;
    [SerializeField] private Color readyAccentColor = new(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private GameObject readyFxRoot;
    [SerializeField] private Animator readyAnimator;
    [SerializeField] private string readyAnimatorBool = "HasReward";
    [SerializeField] private Color claimButtonReadyColor = new(1f, 0.78f, 0.2f, 1f);

    private Color defaultBackgroundColor;
    private readonly List<Color> defaultAccentColors = new();
    private ColorBlock defaultClaimButtonColors;
    private bool visualsInitialized;

    [Header("Tier Summary")]
    [SerializeField] private TextMeshProUGUI tierListText;

    private AchievementRuntime runtime;

    private void Awake()
    {
        CacheDefaultVisuals();
    }

    private void OnEnable()
    {
        AchievementManager.OnProgress += OnAnyProgress;
        AchievementManager.OnListChanged += OnListChanged;
    }

    private void OnDisable()
    {
        AchievementManager.OnProgress -= OnAnyProgress;
        AchievementManager.OnListChanged -= OnListChanged;
    }

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

        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonPressed);
        }

        Refresh();
    }

    private void OnAnyProgress(AchievementRuntime rt)
    {
        if (runtime == rt) Refresh();
    }

    private void OnListChanged()
    {
        // Order changed; still refresh bound item for up-to-date progress visuals
        if (runtime != null) Refresh();
    }

    public void Refresh()
    {
        if (runtime == null) return;
        CacheDefaultVisuals();

        var def = runtime.definition;
        var nextTier = runtime.GetNextUncompletedTier();
        var nextClaimableTier = runtime.GetNextUnclaimedTier();
        var currentTier = runtime.HighestTierCompleted >= 0 && def.tiers != null && def.tiers.Length > 0
            ? def.tiers[Mathf.Clamp(runtime.HighestTierCompleted, 0, def.tiers.Length - 1)]
            : null;
        int pendingClaims = runtime.GetUnclaimedTierCount();
        bool hasClaimable = pendingClaims > 0;

        float progressFraction = Mathf.Clamp01(runtime.GetProgressToNextTier());
        // If manager provides refined tier ratio use that
        var mgr = AchievementManager.Instance;
        if (mgr != null)
        {
            progressFraction = mgr.GetCurrentTierCompletionRatio(runtime);
        }

        if (progressSlider)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = progressFraction;
            progressSlider.gameObject.SetActive(nextTier != null);
        }

        if (progressText)
        {
            if (hasClaimable && nextClaimableTier != null)
            {
                progressText.text = "Reward ready";
            }
            else if (nextTier == null)
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
            if (total > 0)
            {
                tierStatusText.text = pendingClaims > 0
                    ? $"{completed}/{total} tiers complete — {pendingClaims} reward{(pendingClaims == 1 ? "" : "s")} ready"
                    : $"{completed}/{total} tiers complete";
            }
            else
            {
                tierStatusText.text = "No tiers configured";
            }
        }

        if (currentTierText)
        {
            currentTierText.text = currentTier != null ? currentTier.tierName : "";
        }

        if (nextTierText)
        {
            nextTierText.text = nextTier != null ? nextTier.tierName : "MASTERED";
        }

        if (nextRewardText)
        {
            if (hasClaimable && nextClaimableTier != null)
            {
                nextRewardText.text = "Claim: " + BuildRewardSummary(nextClaimableTier.rewards);
            }
            else
            {
                nextRewardText.text = nextTier != null
                    ? BuildRewardSummary(nextTier.rewards)
                    : "All rewards claimed";
            }
        }

        if (completionBadge)
        {
            completionBadge.SetActive(nextTier == null && !hasClaimable);
        }

        if (claimButton)
        {
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = hasClaimable;
        }

        if (claimButtonText)
        {
            claimButtonText.text = hasClaimable ? "Claim reward" : "No reward";
        }

        if (claimIndicator)
        {
            claimIndicator.SetActive(hasClaimable);
        }

        ApplyReadyVisuals(hasClaimable);
        UpdateTierListing();
    }

    private void CacheDefaultVisuals()
    {
        if (visualsInitialized) return;

        if (readyBackground != null)
        {
            defaultBackgroundColor = readyBackground.color;
        }

        defaultAccentColors.Clear();
        if (readyAccentGraphics != null && readyAccentGraphics.Length > 0)
        {
            for (int i = 0; i < readyAccentGraphics.Length; i++)
            {
                var graphic = readyAccentGraphics[i];
                if (graphic == null)
                {
                    defaultAccentColors.Add(Color.white);
                    continue;
                }
                defaultAccentColors.Add(graphic.color);
            }
        }

        if (claimButton != null)
        {
            defaultClaimButtonColors = claimButton.colors;
        }

        visualsInitialized = true;
    }

    private void ApplyReadyVisuals(bool hasReward)
    {
        if (readyBackground != null)
        {
            readyBackground.color = hasReward ? readyBackgroundColor : defaultBackgroundColor;
        }

        if (readyAccentGraphics != null && readyAccentGraphics.Length > 0)
        {
            for (int i = 0; i < readyAccentGraphics.Length; i++)
            {
                var graphic = readyAccentGraphics[i];
                if (graphic == null) continue;

                Color targetColor = hasReward
                    ? readyAccentColor
                    : i < defaultAccentColors.Count ? defaultAccentColors[i] : graphic.color;
                graphic.color = targetColor;
            }
        }

        if (claimButton != null)
        {
            var colors = defaultClaimButtonColors;
            if (hasReward)
            {
                colors.normalColor = claimButtonReadyColor;
                colors.highlightedColor = claimButtonReadyColor;
                colors.pressedColor = claimButtonReadyColor * 0.9f;
                colors.selectedColor = claimButtonReadyColor;
            }
            claimButton.colors = colors;
        }

        if (readyFxRoot != null)
        {
            readyFxRoot.SetActive(hasReward);
        }

        if (readyAnimator != null && !string.IsNullOrEmpty(readyAnimatorBool))
        {
            readyAnimator.SetBool(readyAnimatorBool, hasReward);
        }
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
            bool claimed = runtime.IsTierClaimed(i);
            string statusGlyph;
            if (claimed)
                statusGlyph = "<color=#4CAF50>✔</color>";
            else if (completed)
                statusGlyph = "<color=#FFD54F>●</color>";
            else
                statusGlyph = "<color=#888888>○</color>";

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
                  .Append(BuildRewardSummary(tier.rewards));
                if (completed && !claimed)
                    sb.Append(" <color=#FFD54F>(Claim)</color>");
                sb.Append("</size>");
            }

            if (i < def.tiers.Length - 1)
                sb.AppendLine();
        }

        tierListText.text = sb.ToString();
    }

    private void OnClaimButtonPressed()
    {
        if (runtime == null) return;

        var manager = AchievementManager.Instance;
        if (manager == null) return;

        if (manager.ClaimNextTier(runtime))
        {
            Refresh();
        }
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
