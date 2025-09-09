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
    [SerializeField] private GameObject specialUpgradesButton;

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
            case SkillCategory.Special:  panelColor = new Color(0.9f, 0.7f, 1f, 0.3f); break;    // soft magenta, low alpha
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
            menuSkill.SetMultiplier(multiplierSelected); // ensure current multiplier is applied

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(menuSkill.OnClickUpgrade);

            built++;
        }

        return built;
    }

    // CATEGORY BUTTON HOOKS
    public void OnAttackCategory()  => ShowCategory(SkillCategory.Attack);
    public void OnDefenceCategory() => ShowCategory(SkillCategory.Defence);
    public void OnSupportCategory() => ShowCategory(SkillCategory.Support);
    public void OnSpecialCategory() => ShowCategory(SkillCategory.Special);

    public void SetCategoryButtonColours(SkillCategory category)
    {
        ChangeButtonColor(attackUpgradesButton,   new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(defenceUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(supportUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));
        ChangeButtonColor(specialUpgradesButton,  new Color(0f, 0f, 0.5f, 1f));

        switch (category)
        {
            case SkillCategory.Attack:   ChangeButtonColor(attackUpgradesButton, new Color(1f, 0.5f, 0.5f)); break;
            case SkillCategory.Defence:  ChangeButtonColor(defenceUpgradesButton, new Color(0.5f, 0.7f, 1f)); break;
            case SkillCategory.Support:  ChangeButtonColor(supportUpgradesButton, new Color(1f, 1f, 0.6f)); break;
            case SkillCategory.Special:  ChangeButtonColor(specialUpgradesButton, new Color(0.9f, 0.7f, 1f)); break;
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
