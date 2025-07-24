using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Menu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI basicCreditsUI;
    [SerializeField] private TextMeshProUGUI premiumCreditsUI;
    [SerializeField] private TextMeshProUGUI luxuryCreditsUI;

    [SerializeField] private Animator anim;

    [SerializeField] private GameObject buttonPrefab; // Reference to the button prefab

    [SerializeField] private Transform attackCategoryParent;
    [SerializeField] private Transform defenceCategoryParent;
    [SerializeField] private Transform supportCategoryParent;
    [SerializeField] private Transform specialCategoryParent;

    [SerializeField] private Transform attackCategoryButtonGrid;
    [SerializeField] private Transform defenceCategoryButtonGrid;
    [SerializeField] private Transform supportCategoryButtonGrid;
    [SerializeField] private Transform specialCategoryButtonGrid;

    [SerializeField] private Button attackToggleButton;
    [SerializeField] private Button defenceToggleButton;
    [SerializeField] private Button supportToggleButton;
    [SerializeField] private Button specialToggleButton;

    private bool isMenuOpen = true;
    private Tower tower;
    private PlayerManager playermanager = PlayerManager.main;
    private RoundManager roundManager;
    private SkillManager skillManager;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Ensure PlayerManager is initialized
        playermanager = PlayerManager.main;
        if (playermanager == null)
        {
            Debug.LogError("PlayerManager is not initialized.");
        }

        // Ensure RoundManager is initialized
        roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null)
        {
            Debug.LogError("RoundManager is not found in the scene.");
        }
        else
        {
            skillManager = roundManager.GetComponent<SkillManager>();
            if (skillManager == null)
            {
                Debug.LogError("SkillManager is not attached to RoundManager.");
            }
        }

        // Ensure UI elements are assigned
        if (basicCreditsUI == null || premiumCreditsUI == null || luxuryCreditsUI == null)
        {
            Debug.LogError("One or more UI elements are not assigned in the inspector.");
        }

        // Start the coroutine to initialize the shop if all dependencies are ready
        if (roundManager != null && skillManager != null)
        {
            StartCoroutine(WaitForSkillsToInitialize());
        }
    }

    private IEnumerator WaitForSkillsToInitialize()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (skillManager.GetSkillsByCategory("Attack").Count == 0 ||
               skillManager.GetSkillsByCategory("Defence").Count == 0 ||
               skillManager.GetSkillsByCategory("Support").Count == 0 ||
               skillManager.GetSkillsByCategory("Special").Count == 0)
        {
            yield return wait;
        }

        InitializeShop();
    }

    private void InitializeShop()
    {
        Debug.Log("Initializing Shop Categories");

        if (skillManager.GetSkillsByCategory("Attack").Count == 0 ||
            skillManager.GetSkillsByCategory("Defence").Count == 0 ||
            skillManager.GetSkillsByCategory("Support").Count == 0 ||
            skillManager.GetSkillsByCategory("Special").Count == 0)
        {
            Debug.LogWarning("One or more skill categories are empty. Delaying shop initialization.");
            return;
        }

        CreateButtonsForCategory(skillManager.GetSkillsByCategory("Attack"), attackCategoryButtonGrid);
        CreateButtonsForCategory(skillManager.GetSkillsByCategory("Defence"), defenceCategoryButtonGrid);
        CreateButtonsForCategory(skillManager.GetSkillsByCategory("Support"), supportCategoryButtonGrid);
        CreateButtonsForCategory(skillManager.GetSkillsByCategory("Special"), specialCategoryButtonGrid);

        attackCategoryParent.gameObject.SetActive(true);
        defenceCategoryParent.gameObject.SetActive(false);
        supportCategoryParent.gameObject.SetActive(false);
        specialCategoryParent.gameObject.SetActive(false);
    }

    private void CreateButtonsForCategory(Dictionary<string, Skill> skills, Transform parent)
    {
        Debug.Log($"Creating buttons for category: {parent.name}");
        Debug.Log($"Number of skills in category: {skills.Count}");

        foreach (var skill in skills.Values)
        {
            Debug.Log($"Skill Details - Name: {skill.skillName}, Active: {skill.skillActive}, Unlocked: {skill.roundSkillUnlocked}, Base Value: {skill.baseValue}, Level: {skill.level}, Max Level: {skill.maxLevel}");

            if (skill.skillActive && skill.roundSkillUnlocked)
            {
                Debug.Log($"Skill passed filtering: {skill.skillName}");
                GameObject button = Instantiate(buttonPrefab, parent);
                if (button == null)
                {
                    Debug.LogError("Button prefab instantiation failed.");
                    continue;
                }

                Button buttonComponent = button.GetComponent<Button>();
                if (buttonComponent == null)
                {
                    Debug.LogError("Button component is missing on the prefab.");
                    continue;
                }

                TextMeshProUGUI[] textFields = button.GetComponentsInChildren<TextMeshProUGUI>();
                Debug.Log($"Text fields found: {textFields.Length}");

                if (textFields.Length >= 3)
                {
                    textFields[0].text = skill.skillName; // Assign the skill name to the first text field
                    textFields[1].text = NumberManager.FormatLargeNumber(skillManager.GetSkillCost(skill)); // Assign the cost to the second text field
                    textFields[2].text = NumberManager.FormatLargeNumber(skillManager.GetSkillValue(skill)); // Assign the Value to the third text field
                    Debug.Log($"Button text set for skill: {skill.skillName}");
                }
                else
                {
                    Debug.LogError("Not enough text fields on the button prefab.");
                }

                buttonComponent.onClick.AddListener(() => OnSkillButtonClicked(skill, buttonComponent));
                Debug.Log($"Button created and listener added for skill: {skill.skillName}");
            }
            else
            {
                Debug.Log($"Skill did not pass filtering: {skill.skillName}");
            }
        }
    }


    private void OnSkillButtonClicked(Skill skill, Button button)
    {
        if (roundManager.SpendBasicCredits(skillManager.GetSkillCost(skill)))
        {
            skillManager.UpgradeSkill(skill, 1);
            UpdateUI();
            UpdateButtonText(skill, button);
        }
    }

    private void UpdateButtonText(Skill skill, Button button)
    {
        TextMeshProUGUI[] textFields = button.GetComponentsInChildren<TextMeshProUGUI>();
        if (textFields.Length >= 3)
        {
            textFields[0].text = skill.skillName;
            textFields[1].text = NumberManager.FormatLargeNumber(skillManager.GetSkillCost(skill));
            textFields[2].text = NumberManager.FormatLargeNumber(skillManager.GetSkillValue(skill));
        }
    }

    private void UpdateUI()
    {
        basicCreditsUI.text = $"BC: {NumberManager.FormatLargeNumber(roundManager.tempBasicCredits)}";
        premiumCreditsUI.text = $"PC: {NumberManager.FormatLargeNumber(playermanager.premiumCredits)}";
        luxuryCreditsUI.text = $"LC: {NumberManager.FormatLargeNumber(playermanager.luxuryCredits)}";
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        anim.SetBool("MenuOpen", isMenuOpen);
    }

    private void OnGUI()
    {
        if (tower == null || roundManager == null)
        {
            return;
        }

        UpdateUI();
    }

    public void ToggleCategory(Transform categoryParent)
    {
        if (!isMenuOpen)
        {
            ToggleMenu();
        }

        if(categoryParent.gameObject.activeSelf)
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
}