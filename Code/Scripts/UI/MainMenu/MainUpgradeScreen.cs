using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainUpgradeScreen : MonoBehaviour
{
    [Header("Skill Button Root")]
    [SerializeField] private Transform upgradesPanel; // The single parent for all upgrade buttons
    [SerializeField] private Image panelBackgroundImage; // Background image to change color

    [Header("Category Buttons")]
    [SerializeField] private GameObject attackUpgradesButton;
    [SerializeField] private GameObject defenceUpgradesButton;
    [SerializeField] private GameObject supportUpgradesButton;
    [SerializeField] private GameObject turretUpgradesButton;

    [Header("Prefabs / UI")]
    [SerializeField] private GameObject upgradeButtonPrefab;
    [SerializeField] private TextMeshProUGUI upgradesTitleText;

    [Header("Services")]
    [SerializeField] private SkillService skillService;
    [SerializeField] private ScrollRect scrollRect; // assign your Scroll View here

    [Header("Multiplier Buttons")]
    [SerializeField] private List<Button> multiplierButtons;
    [SerializeField] private List<int> multiplierValues = new List<int> { 1, 5, 10, 50, 100, -1 }; // -1 for MAX

    [Header("Multiplier Button Styling")]
    [SerializeField] private Color multBgNormal   = new Color(0.16f, 0.18f, 0.22f, 1f);
    [SerializeField] private Color multBgSelected = new Color(0.95f, 0.85f, 0.30f, 1f);
    [SerializeField] private Color multTextNormal = Color.white;
    [SerializeField] private Color multTextSelected = Color.black;

    private PlayerManager playerManager;
    private SkillCategory currentCategory = SkillCategory.Attack;
    private int multiplierSelected = 1;

    private void Awake()
    {
        InitMultiplierButtons();
        SyncMultiplierButtonVisuals();
    }

    private void Start()
    {
        playerManager = PlayerManager.main;
        if (!skillService) skillService = SkillService.Instance;

        ShowCategory(SkillCategory.Attack);
    }

    // Consolidated: Only one panel, clear and rebuild for selected category
    public void ShowCategory(SkillCategory category)
    {
        currentCategory = category;
        ClearGrid(upgradesPanel);

        // Update title
        upgradesTitleText.text = category.ToString() + " Upgrades";

        // Change panel background colour
        Color panelColor = Color.white;
        switch (category)
        {
            case SkillCategory.Attack:   panelColor = new Color(1f, 0.5f, 0.5f, 0.3f); break;    // soft red, low alpha
            case SkillCategory.Defence:  panelColor = new Color(0.5f, 0.7f, 1f, 0.3f); break;    // soft blue, low alpha
            case SkillCategory.Support:  panelColor = new Color(1f, 1f, 0.6f, 0.3f); break;      // soft yellow, low alpha
            case SkillCategory.Turret:  panelColor = new Color(0.9f, 0.7f, 1f, 0.3f); break;    // soft magenta, low alpha
        }
        if (panelBackgroundImage)
            panelBackgroundImage.color = panelColor;

        BuildButtonsForCategory(category, upgradesPanel);
        SetCategoryButtonColours(category);

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f; // scroll to top
    }

    private void ClearGrid(Transform root)
    {
        if (!root) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private int BuildButtonsForCategory(SkillCategory category, Transform root)
    {
        if (!root || !skillService || playerManager == null) return 0;
        int built = 0;

        foreach (var def in skillService.GetByCategory(category))
        {
            if (!def) continue;

            // 1) Only show skills whose prerequisite is empty OR unlocked (persistent)
            bool prereqOk = string.IsNullOrEmpty(def.prerequisiteSkillId)
                            || skillService.IsUnlocked(def.prerequisiteSkillId, persistentOnly: true);
            if (!prereqOk) continue;

            // 2) If cost to unlock is 0 and it's locked, auto-unlock
            bool isUnlocked = skillService.IsUnlocked(def.id, persistentOnly: true);
            if (!isUnlocked && def.coresToUnlock <= 0f)
            {
                skillService.TryUnlockPersistent(def.id, playerManager.Wallet);
                isUnlocked = skillService.IsUnlocked(def.id, persistentOnly: true);
            }

            var go = Instantiate(upgradeButtonPrefab, root);
            if (!go) continue;

            var btn = go.GetComponent<Button>();
            if (!btn)
            {
                Debug.LogError("Upgrade button prefab missing Button component.");
                Destroy(go);
                continue;
            }

            var menuSkill = go.GetComponent<MenuSkill>() ?? go.AddComponent<MenuSkill>();
            menuSkill.Bind(def.id, playerManager.Wallet, false);
            menuSkill.SetMultiplier(multiplierSelected);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(menuSkill.OnClickUpgrade);

            if (!isUnlocked)
            {
                // use cached refs on MenuSkill instead of hierarchy search
                btn.interactable = false;
                menuSkill.ShowUnlockUI(def, playerManager, skillService, () => ShowCategory(currentCategory));
            }
            else
            {
                // Unlocked: hide unlock UI if present
                menuSkill.HideUnlockUI();
            }

            built++;
        }

        return built;
    }

    // Looks for child objects named "UnlockPanel", "UnlockButton", "UnlockCostText"
    private void ShowUnlockPanel(GameObject root, SkillDefinition def)
    {
        var unlockPanel = root.transform.Find("UnlockPanel");
        if (!unlockPanel)
        {
            Debug.LogWarning($"UnlockPanel not found under {root.name}. Add a child named 'UnlockPanel' with a Button 'UnlockButton' and TMP 'UnlockCostText'.");
            return;
        }

        unlockPanel.gameObject.SetActive(true);

        var costTextTf = unlockPanel.Find("UnlockCostText");
        var costText = costTextTf ? costTextTf.GetComponent<TextMeshProUGUI>() : null;

        // optional: UnlockCostIcon (Image) next to the text if you want an icon shown
        var costIconTf = unlockPanel.Find("UnlockCostIcon");
        var costIcon = costIconTf ? costIconTf.GetComponent<UnityEngine.UI.Image>() : null;
        if (costIcon != null) costIcon.gameObject.SetActive(true);

        var btnTf = unlockPanel.Find("UnlockButton");
        var unlockBtn = btnTf ? btnTf.GetComponent<UnityEngine.UI.Button>() : null;

        float cost = Mathf.Max(0f, def.coresToUnlock);

        // Format display: "123 Cores" or "FREE"
        if (costText)
        {
            if (cost > 0f)
            {
                // prefer integer display when whole number
                string costStr = (Mathf.Approximately(cost, Mathf.Round(cost))) ? ((long)Mathf.Round(cost)).ToString() : NumberManager.FormatLargeNumber(cost);
                costText.text = $"{costStr} Cores";
            }
            else
            {
                costText.text = "FREE";
            }
        }

        if (unlockBtn)
        {
            unlockBtn.onClick.RemoveAllListeners();
            unlockBtn.onClick.AddListener(() =>
            {
                if (skillService.TryUnlockPersistent(def.id, playerManager.Wallet))
                {
                    // Rebuild to reflect new unlocked state
                    ShowCategory(currentCategory);
                }
                else
                {
                    // Update afford state
                    bool canAfford = skillService.CanUnlockPersistent(def.id, playerManager.Wallet);
                    if (costText) costText.color = canAfford ? Color.white : Color.red;
                }
            });

            bool canAfford = skillService.CanUnlockPersistent(def.id, playerManager.Wallet);
            unlockBtn.interactable = canAfford;
            if (costText) costText.color = canAfford ? Color.white : Color.red;
        }
    }

    private void HideUnlockPanel(GameObject root)
    {
        var unlockPanel = root.transform.Find("UnlockPanel");
        if (unlockPanel) unlockPanel.gameObject.SetActive(false);
    }

    // CATEGORY BUTTON HOOKS
    public void OnAttackCategory()  => ShowCategory(SkillCategory.Attack);
    public void OnDefenceCategory() => ShowCategory(SkillCategory.Defence);
    public void OnSupportCategory() => ShowCategory(SkillCategory.Support);
    public void OnTurretCategory() => ShowCategory(SkillCategory.Turret);

    public void SetCategoryButtonColours(SkillCategory category)
    {
        ChangeButtonColor(attackUpgradesButton,   new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(defenceUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(supportUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(turretUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));

        switch (category)
        {
            case SkillCategory.Attack:   ChangeButtonColor(attackUpgradesButton, new Color(1f, 0.5f, 0.5f)); break;
            case SkillCategory.Defence:  ChangeButtonColor(defenceUpgradesButton, new Color(0.5f, 0.7f, 1f)); break;
            case SkillCategory.Support:  ChangeButtonColor(supportUpgradesButton, new Color(1f, 1f, 0.6f)); break;
            case SkillCategory.Turret:  ChangeButtonColor(turretUpgradesButton, new Color(0.9f, 0.7f, 1f)); break;
        }
    }

    public void ChangeButtonColor(GameObject button, Color color)
    {
        if (!button) return;
        var img = button.GetComponent<Image>();
        if (img) img.color = color;
    }

    // -------- Multiplier wiring (same behavior as Round Shop Menu) --------
    private void InitMultiplierButtons()
    {
        if (multiplierButtons == null || multiplierValues == null) return;

        int count = Mathf.Min(multiplierButtons.Count, multiplierValues.Count);
        if (multiplierButtons.Count != multiplierValues.Count)
            Debug.LogWarning($"MainUpgradeScreen: multiplierButtons ({multiplierButtons.Count}) and multiplierValues ({multiplierValues.Count}) size mismatch.");

        for (int i = 0; i < count; i++)
        {
            var btn = multiplierButtons[i];
            if (!btn) continue;
            int value = multiplierValues[i]; // capture
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectMultiplier(value));

            // Ensure ColorTint so our colors apply consistently
            if (btn.transition == Selectable.Transition.SpriteSwap)
                btn.transition = Selectable.Transition.ColorTint;
        }
    }

    private void ApplyMultiplierButtonStyle(Button btn, bool selected)
    {
        if (!btn) return;

        var graphic = btn.targetGraphic;
        if (graphic)
        {
            var c = selected ? multBgSelected : multBgNormal;
            graphic.color = c;

            var cb = btn.colors;
            cb.normalColor = c;
            cb.highlightedColor = c;
            cb.selectedColor = c;
            cb.pressedColor = c * 0.95f;
            cb.disabledColor = c * 0.6f;
            btn.colors = cb;
        }

        var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt) txt.color = selected ? multTextSelected : multTextNormal;
    }

    private void SyncMultiplierButtonVisuals()
    {
        if (multiplierButtons == null || multiplierValues == null) return;
        int idx = multiplierValues.IndexOf(multiplierSelected);
        for (int i = 0; i < multiplierButtons.Count; i++)
            ApplyMultiplierButtonStyle(multiplierButtons[i], i == idx);
    }

    public void SelectMultiplier(int value)
    {
        multiplierSelected = value;
        SyncMultiplierButtonVisuals();
        PropagateMultiplierToVisible();
    }

    private void PropagateMultiplierToVisible()
    {
        if (!upgradesPanel) return;
        for (int i = 0; i < upgradesPanel.childCount; i++)
        {
            var ms = upgradesPanel.GetChild(i).GetComponent<MenuSkill>();
            if (ms) ms.SetMultiplier(multiplierSelected);
        }
    }
}
