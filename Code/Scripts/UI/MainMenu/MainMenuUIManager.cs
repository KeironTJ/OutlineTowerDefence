using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Main Menu Header Reference")]
    [SerializeField] private CurrencyDisplayUI coresUI;
    [SerializeField] private CurrencyDisplayUI prismsUI;
    [SerializeField] private CurrencyDisplayUI loopsUI;
    [SerializeField] private CurrencyDefinition coresDef;
    [SerializeField] private CurrencyDefinition prismsDef;
    [SerializeField] private CurrencyDefinition loopsDef;

    [Header("Main Menu Footer Screens")]
    [SerializeField] private GameObject mainScreenUI;
    [SerializeField] private GameObject loadoutScreenUI;
    [SerializeField] private GameObject upgradeScreenUI;
    [SerializeField] private GameObject rewardScreenUI;
    [SerializeField] private GameObject researchScreenUI;
    [SerializeField] private GameObject settingsScreenUI;

    [Header("Main Menu Footer Buttons")]
    [SerializeField] private GameObject mainButton;
    [SerializeField] private GameObject loadoutButton;
    [SerializeField] private GameObject upgradeButton;
    [SerializeField] private GameObject rewardButton;
    [SerializeField] private GameObject researchButton;
    [SerializeField] private GameObject settingsButton;

    [Header("Services")]
    [SerializeField] private SkillService skillService;

    private PlayerManager playerManager;


    private void Start()
    {
        playerManager = PlayerManager.main;
        if (!skillService) skillService = SkillService.Instance;
        if (skillService) skillService.ClearRoundStates();

        DisplayCurrency();

        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged += OnBalanceChanged;

        // (MenuSkill instances will self-refresh on SkillUpgraded; we don't need to track entries)
        SelectScreen(ScreenType.Main);
    }

    private void OnDestroy()
    {
        if (playerManager?.Wallet != null)
            playerManager.Wallet.BalanceChanged -= OnBalanceChanged;
    }

    // ================= CURRENCY / HEADER =================
    private void OnBalanceChanged(CurrencyType type, float _)
    {
        if (type == CurrencyType.Cores || type == CurrencyType.Prisms || type == CurrencyType.Loops)
            DisplayCurrency();
    }

    public void DisplayCurrency()
    {
        if (playerManager?.Wallet == null) return;
        coresUI.SetCurrency(coresDef, playerManager.Wallet.Get(CurrencyType.Cores));
        prismsUI.SetCurrency(prismsDef, playerManager.Wallet.Get(CurrencyType.Prisms));
        loopsUI.SetCurrency(loopsDef, playerManager.Wallet.Get(CurrencyType.Loops));
    }

    // ================= FOOTER NAV =================
    public enum ScreenType { Main, Loadout, Upgrade, Reward, Research, Settings }

    public void SelectScreen(ScreenType screenType)
    {
        mainScreenUI.SetActive(screenType == ScreenType.Main);
        loadoutScreenUI.SetActive(screenType == ScreenType.Loadout);
        upgradeScreenUI.SetActive(screenType == ScreenType.Upgrade);
        rewardScreenUI.SetActive(screenType == ScreenType.Reward);
        researchScreenUI.SetActive(screenType == ScreenType.Research);
        settingsScreenUI.SetActive(screenType == ScreenType.Settings);

        ResetScreenButtonColors();
        switch (screenType)
        {
            case ScreenType.Main:      ChangeButtonColor(mainButton, Color.black); break;
            case ScreenType.Loadout:   ChangeButtonColor(loadoutButton, Color.black); break;
            case ScreenType.Upgrade:   ChangeButtonColor(upgradeButton, Color.black); break;
            case ScreenType.Reward:    ChangeButtonColor(rewardButton, Color.black); break;
            case ScreenType.Research:  ChangeButtonColor(researchButton, Color.black); break;
            case ScreenType.Settings:  ChangeButtonColor(settingsButton, Color.black); break;
        }
    }

    private void ResetScreenButtonColors()
    {
        ChangeButtonColor(mainButton, Color.blue);
        ChangeButtonColor(loadoutButton, Color.blue);
        ChangeButtonColor(upgradeButton, Color.blue);
        ChangeButtonColor(rewardButton, Color.blue);
        ChangeButtonColor(researchButton, Color.blue);
        ChangeButtonColor(settingsButton, Color.blue);
    }

    public void SelectMainScreen()     => SelectScreen(ScreenType.Main);
    public void SelectLoadoutScreen()  => SelectScreen(ScreenType.Loadout);
    public void SelectUpgradeScreen()  => SelectScreen(ScreenType.Upgrade);
    public void SelectRewardScreen()   => SelectScreen(ScreenType.Reward);
    public void SelectResearchScreen() => SelectScreen(ScreenType.Research);
    public void SelectSettingsScreen() => SelectScreen(ScreenType.Settings);

    // ================= SCENE / QUIT =================

    public void QuitGame() => Application.Quit();

    // ================= UTIL =================
    public void ChangeButtonColor(GameObject button, Color color)
    {
        if (!button) return;
        var img = button.GetComponent<Image>();
        if (img) img.color = color;
    }
}
