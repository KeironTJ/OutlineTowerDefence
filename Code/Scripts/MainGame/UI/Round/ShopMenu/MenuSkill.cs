using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuSkill : MonoBehaviour
{
    [Header("Refs")]
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

    private string skillId;
    private ICurrencyWallet wallet;
    private CurrencyType currency;
    private Button button;
    private bool inRound;
    private bool subscribed;

    public void Bind(string skillId, ICurrencyWallet wallet, bool inRound)
    {
        this.skillId = skillId;
        this.wallet = wallet;
        this.inRound = inRound;
        currency = inRound ? CurrencyType.Fragments : CurrencyType.Cores;

        if (!button) button = GetComponent<Button>();
        if (currencyImage)
            currencyImage.sprite = currency == CurrencyType.Fragments ? fragmentsIcon : coresIcon;

        Subscribe();
        Refresh();
    }

    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribed) return;
        if (wallet != null) wallet.BalanceChanged += OnWalletChanged;
        if (SkillService.Instance)
        {
            SkillService.Instance.SkillUpgraded += OnSkillEvent;
            SkillService.Instance.SkillValueChanged += OnSkillEvent;
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
        subscribed = false;
    }

    private void OnWalletChanged(CurrencyType type, float _) { if (type == currency) Refresh(); }
    private void OnSkillEvent(string id) { if (id == skillId) Refresh(); }

    public void OnClickUpgrade()
    {
        var svc = SkillService.Instance;
        if (!svc || wallet == null) return;

        bool ok = inRound
            ? svc.TryUpgradeRound(skillId, currency, wallet)
            : PlayerManager.main.TryMetaUpgradeSkill(skillId, currency);

        if (ok) Refresh();
    }

    public void Refresh()
    {
        var svc = SkillService.Instance;
        if (!svc) return;
        var def = svc.GetDefinition(skillId);
        if (!def) return;

        int lvl = svc.GetLevel(skillId);
        int max = def.maxLevel;

        if (skillHeaderText) skillHeaderText.text = def.displayName;
        if (skillLevelText)  skillLevelText.text  = $"{lvl}/{max}";
        if (skillValueText)  skillValueText.text  = NumberManager.FormatLargeNumber(svc.GetValue(skillId));
        if (upgradeMultiplierText) upgradeMultiplierText.text = $"x1";


        var preview = svc.GetUpgradePreview(skillId, currency);
        if (preview.isMaxed)
        {
            if (skillCostText) skillCostText.text = "MAX";
            if (skillNextValueText) skillNextValueText.text = "";
            if (button) button.interactable = false;
        }
        else
        {
            if (skillCostText)
            {
                skillCostText.text = NumberManager.FormatLargeNumber(preview.cost);
                skillCostText.color = wallet.Get(currency) >= preview.cost ? Color.white : Color.red;
            }
            if (skillNextValueText)
                skillNextValueText.text = "→ " + NumberManager.FormatLargeNumber(preview.nextValue);
            if (button) button.interactable = wallet.Get(currency) >= preview.cost;
        }
    }
}
