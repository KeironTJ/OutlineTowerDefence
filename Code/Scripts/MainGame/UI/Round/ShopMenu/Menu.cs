using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Menu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;

    [SerializeField] private GameObject buttonPrefab;
    private ICurrencyWallet roundWallet;

    [Header("Category Parents (Panels)")]
    [SerializeField] private Transform attackCategoryParent;
    [SerializeField] private Transform defenceCategoryParent;
    [SerializeField] private Transform supportCategoryParent;
    [SerializeField] private Transform specialCategoryParent;

    [Header("Button Grids (Content Roots)")]
    [SerializeField] private Transform attackCategoryButtonGrid;
    [SerializeField] private Transform defenceCategoryButtonGrid;
    [SerializeField] private Transform supportCategoryButtonGrid;
    [SerializeField] private Transform specialCategoryButtonGrid;

    [Header("Category Toggle Buttons")]
    [SerializeField] private Button attackToggleButton;
    [SerializeField] private Button defenceToggleButton;
    [SerializeField] private Button supportToggleButton;
    [SerializeField] private Button specialToggleButton;

    [Header("Services")]
    [SerializeField] private SkillService skillService;

    private bool isMenuOpen = true;
    private Tower tower;
    private PlayerManager playerManager;
    private RoundManager roundManager;

    private bool inRound = true;

    // ---------------- INITIALIZE ----------------
    public void Initialize(RoundManager roundManager, WaveManager waveManager, Tower tower, ICurrencyWallet roundWallet, bool inRound = true)
    {
        this.roundManager = roundManager;
        this.tower = tower;
        this.roundWallet = roundWallet;
        this.inRound = inRound;

        if (!skillService) skillService = SkillService.Instance;
        playerManager = PlayerManager.main;

        if (!skillService)
        {
            Debug.LogError("Menu: SkillService missing.");
            return;
        }

        if (roundWallet == null)
        {
            Debug.LogWarning("Menu: Round wallet not provided (buttons will be non‑interactive).");
        }

        BuildShop();
    }

    // ---------------- BUILD SHOP ----------------
    private void BuildShop()
    {
        ClearGrid(attackCategoryButtonGrid);
        ClearGrid(defenceCategoryButtonGrid);
        ClearGrid(supportCategoryButtonGrid);
        ClearGrid(specialCategoryButtonGrid);

        int attackCount  = BuildButtonsForCategory(SkillCategory.Attack,  attackCategoryButtonGrid);
        int defenceCount = BuildButtonsForCategory(SkillCategory.Defence, defenceCategoryButtonGrid);
        int supportCount = BuildButtonsForCategory(SkillCategory.Support, supportCategoryButtonGrid);
        int specialCount = BuildButtonsForCategory(SkillCategory.Special, specialCategoryButtonGrid);

        attackCategoryParent.gameObject.SetActive(attackCount  > 0);
        defenceCategoryParent.gameObject.SetActive(false);
        supportCategoryParent.gameObject.SetActive(false);
        specialCategoryParent.gameObject.SetActive(false);
    }

    private void ClearGrid(Transform grid)
    {
        if (!grid) return;
        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);
    }

    private int BuildButtonsForCategory(SkillCategory category, Transform grid)
    {
        if (!grid) return 0;
        int built = 0;

        foreach (var def in skillService.GetByCategory(category))
        {
            // If you later add unlock flags, filter here (e.g., if (!skillService.IsUnlocked(def.id)) continue;)

            GameObject go = Instantiate(buttonPrefab, grid);
            if (!go) continue;

            var btn = go.GetComponent<Button>();
            if (!btn)
            {
                Debug.LogError("Menu: Button prefab missing Button component.");
                Destroy(go);
                continue;
            }

            var menuSkill = go.GetComponent<MenuSkill>() ?? go.AddComponent<MenuSkill>();
            menuSkill.Bind(def.id, inRound ? roundWallet : playerManager.Wallet, inRound);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(menuSkill.OnClickUpgrade);

            built++;
        }

        return built;
    }

    // ---------------- UI TOGGLING ----------------
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        anim.SetBool("MenuOpen", isMenuOpen);
    }

    public void ToggleCategory(Transform categoryParent)
    {
        if (!isMenuOpen)
        {
            ToggleMenu();
        }

        if (categoryParent.gameObject.activeSelf)
        {
            ToggleMenu();
            attackCategoryParent.gameObject.SetActive(false);
            defenceCategoryParent.gameObject.SetActive(false);
            supportCategoryParent.gameObject.SetActive(false);
            specialCategoryParent.gameObject.SetActive(false);
        }
        else
        {
            attackCategoryParent.gameObject.SetActive(false);
            defenceCategoryParent.gameObject.SetActive(false);
            supportCategoryParent.gameObject.SetActive(false);
            specialCategoryParent.gameObject.SetActive(false);
            
            categoryParent.gameObject.SetActive(true);
        }
    }

    private void OnGUI()
    {
        if (tower == null || roundManager == null)
            return;
    }
}