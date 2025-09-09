using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Menu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;

    [Header("Upgrade Button Root")]
    [SerializeField] private Transform upgradesPanel;
    [SerializeField] private Image panelBackgroundImage;

    [Header("Category Buttons")]
    [SerializeField] private Button attackToggleButton;
    [SerializeField] private Button defenceToggleButton;
    [SerializeField] private Button supportToggleButton;
    [SerializeField] private Button specialToggleButton;

    [Header("Prefabs / UI")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TextMeshProUGUI upgradesTitleText;

    [Header("Services")]
    [SerializeField] private SkillService skillService;

    [Header("Multiplier Buttons")]
    [SerializeField] private List<Button> multiplierButtons;
    [SerializeField] private List<int> multiplierValues = new List<int> { 1, 5, 10, 50, 100, -1 }; // -1 for Max

    [Header("Multiplier Button Styling")]
    [SerializeField] private Color multBgNormal   = new Color(0.16f, 0.18f, 0.22f, 1f);
    [SerializeField] private Color multBgSelected = new Color(0.95f, 0.85f, 0.30f, 1f);
    [SerializeField] private Color multTextNormal = Color.white;
    [SerializeField] private Color multTextSelected = Color.black;

    private ICurrencyWallet roundWallet;
    private PlayerManager playerManager;
    private RoundManager roundManager;
    private Tower tower;
    private bool inRound = true;
    private bool isMenuOpen = false;
    private SkillCategory? openCategory = null;
    private int multiplierSelected = 1;

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

        if (roundWallet == null && inRound)
        {
            Debug.LogWarning("Menu: Round wallet not provided (buttons will be non‑interactive).");
        }

        ShowCategory(SkillCategory.Attack);
    }

    // ---------------- CATEGORY PANEL LOGIC ----------------
    public void ShowCategory(SkillCategory category)
    {
        // CASE 1: Menu closed, any click opens menu to that category
        if (!isMenuOpen)
        {
            isMenuOpen = true;
            openCategory = category;
            if (anim) anim.SetBool("MenuOpen", true);
            BuildCategoryUI(category);
            return;
        }

        // CASE 2: Menu open, clicking a different category switches content (no anim)
        if (openCategory != category)
        {
            openCategory = category;
            BuildCategoryUI(category);
            return;
        }

        // CASE 3: Menu open, clicking same category closes menu
        isMenuOpen = false;
        openCategory = null;
        if (anim) anim.SetBool("MenuOpen", false);
        // Optionally clear UI or leave as is
    }

    private void BuildCategoryUI(SkillCategory category)
    {
        ClearGrid(upgradesPanel);

        // Update title if present
        if (upgradesTitleText)
            upgradesTitleText.text = category + " Upgrades";

        // Change panel background colour (optional)
        if (panelBackgroundImage)
        {
            Color panelColor = Color.white;
            switch (category)
            {
                case SkillCategory.Attack: panelColor = new Color(1f, 0.5f, 0.5f, 0.3f); break;
                case SkillCategory.Defence: panelColor = new Color(0.5f, 0.7f, 1f, 0.3f); break;
                case SkillCategory.Support: panelColor = new Color(1f, 1f, 0.6f, 0.3f); break;
                case SkillCategory.Special: panelColor = new Color(0.9f, 0.7f, 1f, 0.3f); break;
            }
            panelBackgroundImage.color = panelColor;
        }

        BuildButtonsForCategory(category, upgradesPanel);
        SetCategoryButtonColours(category);
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

            var go = Instantiate(buttonPrefab, root);
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

            // ensure the current multiplier is applied to new buttons
            menuSkill.SetMultiplier(multiplierSelected);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(menuSkill.OnClickUpgrade);

            built++;
        }

        return built;
    }

    // CATEGORY BUTTON HOOKS
    public void OnAttackCategory() => ShowCategory(SkillCategory.Attack);
    public void OnDefenceCategory() => ShowCategory(SkillCategory.Defence);
    public void OnSupportCategory() => ShowCategory(SkillCategory.Support);
    public void OnSpecialCategory() => ShowCategory(SkillCategory.Special);

    public void SetCategoryButtonColours(SkillCategory category)
    {
        ChangeButtonColor(attackToggleButton?.gameObject, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(defenceToggleButton?.gameObject, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(supportToggleButton?.gameObject, new Color(0, 0, 0.5f, 1f));
        ChangeButtonColor(specialToggleButton?.gameObject, new Color(0, 0, 0.5f, 1f));

        switch (category)
        {
            case SkillCategory.Attack: ChangeButtonColor(attackToggleButton?.gameObject, new Color(1f, 0.5f, 0.5f)); break;
            case SkillCategory.Defence: ChangeButtonColor(defenceToggleButton?.gameObject, new Color(0.5f, 0.7f, 1f)); break;
            case SkillCategory.Support: ChangeButtonColor(supportToggleButton?.gameObject, new Color(1f, 1f, 0.6f)); break;
            case SkillCategory.Special: ChangeButtonColor(specialToggleButton?.gameObject, new Color(0.9f, 0.7f, 1f)); break;
        }
    }

    public void ChangeButtonColor(GameObject button, Color color)
    {
        if (!button) return;
        var img = button.GetComponent<Image>();
        if (img) img.color = color;
    }

    // ---------------- UI TOGGLING ----------------
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (anim) anim.SetBool("MenuOpen", isMenuOpen);
    }

    private void Awake()
    {
        // Optional: ensure buttons are wired consistently at runtime
        InitMultiplierButtons();
        // Sync visuals with default multiplierSelected
        SyncMultiplierButtonVisuals();
    }

    private void InitMultiplierButtons()
    {
        if (multiplierButtons == null || multiplierValues == null) return;

        int count = Mathf.Min(multiplierButtons.Count, multiplierValues.Count);
        if (multiplierButtons.Count != multiplierValues.Count)
            Debug.LogWarning($"Menu: multiplierButtons ({multiplierButtons.Count}) and multiplierValues ({multiplierValues.Count}) size mismatch.");

        for (int i = 0; i < count; i++)
        {
            var btn = multiplierButtons[i];
            if (!btn) continue;
            int value = multiplierValues[i]; // capture per-iteration
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectMultiplier(value));
            // Ensure transition is ColorTint (or None) so we can control colors
            if (btn.transition == Selectable.Transition.SpriteSwap)
                btn.transition = Selectable.Transition.ColorTint;
        }
    }

    private void ApplyMultiplierButtonStyle(Button btn, bool selected)
    {
        if (!btn) return;

        // Background (targetGraphic)
        var graphic = btn.targetGraphic;
        if (graphic)
        {
            var c = selected ? multBgSelected : multBgNormal;
            graphic.color = c;

            // Also push into ColorBlock so highlight/pressed/selected don’t fight our choice
            var cb = btn.colors;
            cb.normalColor = c;
            cb.highlightedColor = c;
            cb.selectedColor = c;
            cb.pressedColor = c * 0.95f;
            cb.disabledColor = c * 0.6f;
            btn.colors = cb;
        }

        // Label color
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt) txt.color = selected ? multTextSelected : multTextNormal;
    }

    private void SyncMultiplierButtonVisuals()
    {
        int idx = multiplierValues.IndexOf(multiplierSelected);
        for (int i = 0; i < multiplierButtons.Count; i++)
            ApplyMultiplierButtonStyle(multiplierButtons[i], i == idx);
    }

    // Select multiplier for upgrades
    public void SelectMultiplier(int multiplier)
    {
        multiplierSelected = multiplier;

        // Update selected state visuals
        SyncMultiplierButtonVisuals();

        // Push the new multiplier to visible MenuSkill items
        if (upgradesPanel)
        {
            foreach (Transform child in upgradesPanel)
            {
                var menuSkill = child.GetComponent<MenuSkill>();
                if (menuSkill != null)
                    menuSkill.SetMultiplier(multiplierSelected);
            }
        }
    }
}