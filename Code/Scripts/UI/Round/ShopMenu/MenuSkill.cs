using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MenuSkill : MonoBehaviour
{


    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI skillHeaderText;
    [SerializeField] private TextMeshProUGUI skillCostText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillValueText;
    [SerializeField] private TextMeshProUGUI skillNextValueText;
    [SerializeField] private TextMeshProUGUI upgradeMultiplierText;
    [SerializeField] private Image currencyImage;

    [Header("Icons")]
    [SerializeField] private Sprite fragmentsIcon;
    [SerializeField] private Sprite coresIcon;

    // assign these on the prefab so we don't search at runtime
    [Header("Unlock UI (assign in prefab)")]
    [SerializeField] private GameObject unlockPanel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockCostText;
    [SerializeField] private Image unlockCostIcon;
    
    [Header("Standard Upgrade Panel (optional - for clarity)")]
    [SerializeField] private GameObject standardPanel;

    private string skillId;
    private ICurrencyWallet wallet;
    private CurrencyType currency;
    private Button button;
    private bool inRound;
    private bool subscribed;
    private int multiplier = 1;
    private TowerStatPipeline statPipeline;
    private bool pipelineSubscribed;
    private ChipService chipService;
    private bool chipsSubscribed;

    public void Bind(string skillId, ICurrencyWallet wallet, bool inRound)
    {
        this.skillId = skillId;
        this.wallet = wallet;
        this.inRound = inRound;
        currency = inRound ? CurrencyType.Fragments : CurrencyType.Cores;

        if (!button) button = GetComponent<Button>();
        if (currencyImage)
            currencyImage.sprite = currency == CurrencyType.Fragments ? fragmentsIcon : coresIcon;

        EnsurePipelineHook();
        Subscribe();
        Refresh();
    }

    private void OnDestroy() => Unsubscribe();

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(skillId))
            EnsurePipelineHook();
    }

    private void OnDisable()
    {
        UnhookPipeline();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (wallet != null) wallet.BalanceChanged += OnWalletChanged;
        if (SkillService.Instance)
        {
            SkillService.Instance.SkillUpgraded += OnSkillEvent;
            SkillService.Instance.SkillValueChanged += OnSkillEvent;
        }
        chipService = ChipService.Instance;
        if (chipService != null && !chipsSubscribed)
        {
            chipService.ChipEquipped += OnChipChanged;
            chipService.ChipUnequipped += OnChipSlotChanged;
            chipService.ChipUpgraded += OnChipLevelChanged;
            chipService.ChipUnlocked += OnChipUnlocked;
            chipService.SlotUnlocked += OnChipSlotUnlocked;
            chipsSubscribed = true;
        }
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (wallet != null) wallet.BalanceChanged -= OnWalletChanged;
        if (SkillService.Instance)
        {
            SkillService.Instance.SkillUpgraded -= OnSkillEvent;
            SkillService.Instance.SkillValueChanged -= OnSkillEvent;
        }
        if (chipService != null && chipsSubscribed)
        {
            chipService.ChipEquipped -= OnChipChanged;
            chipService.ChipUnequipped -= OnChipSlotChanged;
            chipService.ChipUpgraded -= OnChipLevelChanged;
            chipService.ChipUnlocked -= OnChipUnlocked;
            chipService.SlotUnlocked -= OnChipSlotUnlocked;
            chipsSubscribed = false;
            chipService = null;
        }
        subscribed = false;
        UnhookPipeline();
    }

    private void OnWalletChanged(CurrencyType type, float _) { if (type == currency) Refresh(); }
    private void OnSkillEvent(string id) { if (id == skillId) Refresh(); }

    public void OnClickUpgrade()
    {
        var svc = SkillService.Instance;
        if (!svc || wallet == null) return;
        var def = svc.GetDefinition(skillId);
        if (def == null) return;

        int lvl = svc.GetLevel(skillId);
        int max = def.maxLevel;
        int remaining = Mathf.Max(0, max - lvl);
        float available = wallet.Get(currency);

        // MAX = buy as many as affordable (up to remaining)
        if (multiplier == -1)
        {
            if (remaining == 0) { Refresh(); return; }

            int canBuy = 0;
            for (int i = 1; i <= remaining; i++)
            {
                var p = svc.GetUpgradePreview(skillId, currency, lvl, i);
                if (available >= p.cost) canBuy = i; else break;
            }
            if (canBuy <= 0) { Refresh(); return; }

            for (int i = 0; i < canBuy; i++)
            {
                bool ok = inRound
                    ? svc.TryUpgradeRound(skillId, currency, wallet)
                    : svc.TryUpgradePersistent(skillId, currency, wallet);
                if (!ok) break;
            }
            Refresh();
            return;
        }

        // Fixed multiplier
        int desired = Mathf.Max(1, multiplier);

        // Case A: desired within cap -> all-or-nothing
        if (desired <= remaining)
        {
            var bundle = svc.GetUpgradePreview(skillId, currency, lvl, desired);
            if (available < bundle.cost) { Refresh(); return; }

            for (int i = 0; i < desired; i++)
            {
                bool ok = inRound
                    ? svc.TryUpgradeRound(skillId, currency, wallet)
                    : svc.TryUpgradePersistent(skillId, currency, wallet);
                if (!ok) break;
            }
            Refresh();
            return;
        }

        // Case B: desired exceeds cap -> show MAX preview and only buy full path to MAX if affordable
        if (remaining == 0) { Refresh(); return; }

        var capPreview = svc.GetUpgradePreview(skillId, currency, lvl, remaining);
        if (available < capPreview.cost) { Refresh(); return; } // not enough for full MAX path

        for (int i = 0; i < remaining; i++)
        {
            bool ok = inRound
                ? svc.TryUpgradeRound(skillId, currency, wallet)
                : svc.TryUpgradePersistent(skillId, currency, wallet);
            if (!ok) break;
        }
        Refresh();
    }

    public void SetMultiplier(int value)
    {
        multiplier = value;
        Refresh();
    }

    public void Refresh()
    {
        var svc = SkillService.Instance;
        if (!svc) return;
        var def = svc.GetDefinition(skillId);
        if (!def) return;

        float currentBaseValue = svc.GetValue(skillId);

        // If we’re in-round and this skill is main-menu-only, show message and disable
        if (inRound && !svc.IsUpgradableInRound(skillId))
        {
            if (skillHeaderText) skillHeaderText.text = def.displayName;
            if (skillLevelText)  skillLevelText.text  = $"{svc.GetLevel(skillId)}/{def.maxLevel}";
            if (skillValueText)
            {
                float displayValue = ResolveCurrentDisplayValue(svc, currentBaseValue);
                skillValueText.text = BuildValueString(displayValue, def.valueFormat);
            }
            if (skillCostText)   { skillCostText.text = "Main Menu Only"; skillCostText.color = Color.white; }
            if (skillNextValueText) skillNextValueText.text = "";
            if (currencyImage) currencyImage.enabled = false;
            if (button) button.interactable = false;
            return;
        }

        int lvl = svc.GetLevel(skillId);
        int max = def.maxLevel;
        int remaining = Mathf.Max(0, max - lvl);

        if (skillHeaderText) skillHeaderText.text = def.displayName;
        if (skillLevelText)  skillLevelText.text  = $"{lvl}/{max}";
        if (skillValueText)
        {
            float displayValue = ResolveCurrentDisplayValue(svc, currentBaseValue);
            skillValueText.text = BuildValueString(displayValue, def.valueFormat);
        }

        float available = wallet.Get(currency);

        // Maxed
        if (remaining == 0)
        {
            if (backgroundImage) backgroundImage.color = new Color(0f, 0.75f, 0f);
            if (skillCostText) { skillCostText.text = "MAX"; skillCostText.fontStyle = FontStyles.Bold; }
            if (skillNextValueText) skillNextValueText.text = "";
            if (currencyImage) currencyImage.enabled = false;
            if (button) button.interactable = false;
            return;
        }

        // MAX: only show the first step when even the first is unaffordable.
        if (multiplier == -1)
        {
            // Preview the first step (for the "cannot afford any" case)
            var firstStep = svc.GetUpgradePreview(skillId, currency, lvl, 1);

            // Find how many steps up to MAX are affordable
            int affordable = 0;
            float costAffordable = 0f;
            float baseAffordable = currentBaseValue;
            for (int i = 1; i <= remaining; i++)
            {
                var p = svc.GetUpgradePreview(skillId, currency, lvl, i);
                if (available >= p.cost)
                {
                    affordable = i;
                    costAffordable = p.cost;
                    baseAffordable = p.nextValue;
                }
                else break;
            }

            if (affordable == 0)
            {
                // Can't afford even 1: show first step cost/value (disabled)
                if (skillCostText)
                {
                    skillCostText.text  = NumberManager.FormatLargeNumber(firstStep.cost);
                    skillCostText.color = Color.red;
                }
                if (skillNextValueText)
                {
                    float projected = ProjectDisplayValue(currentBaseValue, firstStep.nextValue);
                    skillNextValueText.text = $"x1 -> {NumberManager.FormatLargeNumber(projected)}";
                }

                if (button) button.interactable = false;
                if (currencyImage) currencyImage.enabled = true;
            }
            else
            {
                // Can afford some: show what MAX will actually buy
                if (skillCostText)
                {
                    skillCostText.text  = NumberManager.FormatLargeNumber(costAffordable);
                    skillCostText.color = Color.white;
                }
                if (skillNextValueText)
                {
                    float projected = ProjectDisplayValue(currentBaseValue, baseAffordable);
                    skillNextValueText.text = $"x{affordable} -> {NumberManager.FormatLargeNumber(projected)}";
                }

                if (button) button.interactable = true;
                if (currencyImage) currencyImage.enabled = true;
            }
            return;
        }

        // Fixed multiplier label inside next-value: x{multiplier} -> {newValue}
        int desired = Mathf.Max(1, multiplier);

        // A) within cap -> all-or-nothing
        if (desired <= remaining)
        {
            var bundle = svc.GetUpgradePreview(skillId, currency, lvl, desired);
            bool canAffordFull = available >= bundle.cost;

            if (skillCostText)
            {
                skillCostText.text  = NumberManager.FormatLargeNumber(bundle.cost);
                skillCostText.color = canAffordFull ? Color.white : Color.red;
            }
            if (skillNextValueText)
            {
                float projected = ProjectDisplayValue(currentBaseValue, bundle.nextValue);
                skillNextValueText.text = $"x{desired} -> {NumberManager.FormatLargeNumber(projected)}";
            }

            if (button) button.interactable = canAffordFull;
            if (currencyImage) currencyImage.enabled = true;
            return;
        }

        // B) desired exceeds cap -> show FINAL MAXED STEP as MAX>{newValue} (no stepping)
        {
            var capPreview = svc.GetUpgradePreview(skillId, currency, lvl, remaining);

            if (skillCostText)
            {
                skillCostText.text  = NumberManager.FormatLargeNumber(capPreview.cost);
                skillCostText.color = (available >= capPreview.cost) ? Color.white : Color.red;
            }
            if (skillNextValueText)
            {
                float projected = ProjectDisplayValue(currentBaseValue, capPreview.nextValue);
                skillNextValueText.text = $"x{remaining} -> {NumberManager.FormatLargeNumber(projected)}";
            }

            if (button) button.interactable = available >= capPreview.cost;
            if (currencyImage) currencyImage.enabled = true;
        }
    }

    // called from MainUpgradeScreen instead of doing Find
    public void ShowUnlockUI(SkillDefinition def, PlayerManager pm, SkillService skillService, Action onUnlocked = null)
    {
        if (unlockPanel) unlockPanel.SetActive(true);
        // Hide standard upgrade panel when showing unlock panel for clarity
        if (standardPanel) standardPanel.SetActive(false);
        
        // Ensure the unlock cost icon shows the cores icon (since unlocking always uses cores)
        if (unlockCostIcon)
        {
            unlockCostIcon.gameObject.SetActive(true);
            if (coresIcon != null)
                unlockCostIcon.sprite = coresIcon;
        }

        float cost = Mathf.Max(0f, def.coresToUnlock);
        if (unlockCostText)
        {
            unlockCostText.text = cost > 0f
                ? $"{NumberManager.FormatLargeNumber(cost)} to unlock {def.displayName}"
                : "FREE";
        }

        if (unlockButton)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() =>
            {
                if (skillService.TryUnlockPersistent(def.id, pm.Wallet))
                {
                    onUnlocked?.Invoke();
                }
                else
                {
                    bool canAfford = skillService.CanUnlockPersistent(def.id, pm.Wallet);
                    if (unlockCostText) unlockCostText.color = canAfford ? Color.white : Color.red;
                }
            });

            bool canAfford = skillService.CanUnlockPersistent(def.id, pm.Wallet);
            unlockButton.interactable = canAfford;
            if (unlockCostText) unlockCostText.color = canAfford ? Color.white : Color.red;
        }
    }

    public void HideUnlockUI()
    {
        if (unlockPanel) unlockPanel.SetActive(false);
        // Show standard upgrade panel when hiding unlock panel
        if (standardPanel) standardPanel.SetActive(true);
    }

    private float ResolveCurrentDisplayValue(SkillService svc, float currentBase)
    {
        if (svc == null)
            return 0f;

        EnsurePipelineHook();
        if (SkillStatDisplayUtility.TryGetEffectiveValue(skillId, out float effective))
            return effective;

        return currentBase;
    }

    private static string BuildValueString(float value, string valueFormat)
    {
        string formatted = NumberManager.FormatLargeNumber(value);
        if (!string.IsNullOrWhiteSpace(valueFormat))
            formatted += $" {valueFormat}";
        return formatted;
    }

    private float ProjectDisplayValue(float currentBase, float projectedBase)
    {
        EnsurePipelineHook();
        if (SkillStatDisplayUtility.TryProjectEffectiveValue(skillId, currentBase, projectedBase, out float projected))
            return projected;
        return projectedBase;
    }

    private void EnsurePipelineHook()
    {
        var pipeline = TowerStatPipeline.Instance;
        if (pipeline == null)
            return;

        if (pipelineSubscribed && statPipeline == pipeline)
            return;

        UnhookPipeline();

        statPipeline = pipeline;
        statPipeline.EnsureServiceHooks();
        statPipeline.StatsRebuilt -= OnPipelineStatsRebuilt;
        statPipeline.StatsRebuilt += OnPipelineStatsRebuilt;
        pipelineSubscribed = true;
    }

    private void UnhookPipeline()
    {
        if (!pipelineSubscribed || statPipeline == null)
            return;

        statPipeline.StatsRebuilt -= OnPipelineStatsRebuilt;
        pipelineSubscribed = false;
        statPipeline = null;
    }

    private void OnPipelineStatsRebuilt(TowerStatBundle _)
    {
        Refresh();
    }

    private void OnChipChanged(int _, string __)
    {
        TowerStatPipeline.SignalDirty();
        Refresh();
    }

    private void OnChipSlotChanged(int _)
    {
        TowerStatPipeline.SignalDirty();
        Refresh();
    }

    private void OnChipLevelChanged(string __, int ___)
    {
        TowerStatPipeline.SignalDirty();
        Refresh();
    }

    private void OnChipUnlocked(string __)
    {
        TowerStatPipeline.SignalDirty();
        Refresh();
    }

    private void OnChipSlotUnlocked(int _)
    {
        TowerStatPipeline.SignalDirty();
        Refresh();
    }
}
