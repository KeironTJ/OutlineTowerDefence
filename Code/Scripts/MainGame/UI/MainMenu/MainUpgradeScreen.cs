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

    private PlayerManager playerManager;
    private SkillCategory currentCategory = SkillCategory.Attack;

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
}
