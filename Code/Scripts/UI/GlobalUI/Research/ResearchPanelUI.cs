using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main UI panel for the Research system
/// Manages display and interaction with research items
/// </summary>
public class ResearchPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject researchSlotsPanel;
    [SerializeField] private Transform researchSlotContainer;
    [SerializeField] private GameObject researchSlotPrefab;

    [SerializeField] private GameObject researchCardPanel;
    [SerializeField] private Transform researchCardContainer;
    [SerializeField] private GameObject researchCardPrefab;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button researchListBackButton;
    
    [Header("Filter Buttons")]
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterTowerBaseButton;
    [SerializeField] private Button filterTurretButton;
    [SerializeField] private Button filterProjectileButton;
    [SerializeField] private Button filterBaseStatButton;
    
    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI activeResearchText;
    [SerializeField] private TextMeshProUGUI coresBalanceText;
    [SerializeField] private TextMeshProUGUI loopsBalanceText;
    [SerializeField] private TextMeshProUGUI prismsBalanceText;
    
    private ResearchType? currentFilter = null;
    private List<ResearchCardView> cardViews = new List<ResearchCardView>();
    private readonly List<ResearchSlotView> slotViews = new List<ResearchSlotView>();
    private bool isListVisible;
    
    private void OnEnable()
    {
        ShowSlotsPanel(false);
        RefreshUI();
        
        // Subscribe to research events
        if (ResearchService.Instance != null)
        {
            ResearchService.Instance.ResearchStarted += OnResearchEvent;
            ResearchService.Instance.ResearchCompleted += OnResearchEvent;
            ResearchService.Instance.ResearchSpedUp += OnResearchEvent;
            ResearchService.Instance.ResearchSlotsChanged += OnResearchSlotsChanged;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from research events
        if (ResearchService.Instance != null)
        {
            ResearchService.Instance.ResearchStarted -= OnResearchEvent;
            ResearchService.Instance.ResearchCompleted -= OnResearchEvent;
            ResearchService.Instance.ResearchSpedUp -= OnResearchEvent;
            ResearchService.Instance.ResearchSlotsChanged -= OnResearchSlotsChanged;
        }
    }
    
    private void Start()
    {
        SetupButtons();
        ShowSlotsPanel(false);
        RefreshUI();
    }
    
    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        
        if (researchListBackButton != null)
            researchListBackButton.onClick.AddListener(() => ShowSlotsPanel());
        
        if (filterAllButton != null)
            filterAllButton.onClick.AddListener(() => SetFilter(null));
        
        if (filterTowerBaseButton != null)
            filterTowerBaseButton.onClick.AddListener(() => SetFilter(ResearchType.TowerBase));
        
        if (filterTurretButton != null)
            filterTurretButton.onClick.AddListener(() => SetFilter(ResearchType.Turret));
        
        if (filterProjectileButton != null)
            filterProjectileButton.onClick.AddListener(() => SetFilter(ResearchType.Projectile));
        
        if (filterBaseStatButton != null)
            filterBaseStatButton.onClick.AddListener(() => SetFilter(ResearchType.BaseStat));
    }
    
    private void SetFilter(ResearchType? filter)
    {
        currentFilter = filter;
        RefreshUI();
    }
    
    public void RefreshUI()
    {
        if (ResearchService.Instance == null)
        {
            Debug.LogWarning("[ResearchPanelUI] ResearchService not available");
            return;
        }
        
        ApplyListPanelState();
        UpdateInfoDisplay();
        UpdateResearchSlots();
        if (isListVisible)
            UpdateResearchCards();
    }
    
    private void UpdateInfoDisplay()
    {
        // Active research count
        if (activeResearchText != null)
        {
            int activeCount = ResearchService.Instance != null ? ResearchService.Instance.GetActiveResearchCount() : 0;
            int unlockedSlots = ResearchService.Instance != null
                ? ResearchService.Instance.GetUnlockedSlotCount()
                : (PlayerManager.main?.GetResearchConfig()?.GetUnlockedSlotCount() ?? 1);
            activeResearchText.text = $"Active Research: {activeCount}/{unlockedSlots}";
        }
        
        // Currency balances
        if (PlayerManager.main != null)
        {
            if (coresBalanceText != null)
                coresBalanceText.text = $"{PlayerManager.main.GetCores():F0}";
            
            if (loopsBalanceText != null)
                loopsBalanceText.text = $"{PlayerManager.main.GetLoops():F0}";
            
            if (prismsBalanceText != null)
                prismsBalanceText.text = $"{PlayerManager.main.GetPrisms():F0}";
        }
    }
    
    private void UpdateResearchCards()
    {
        if (researchCardContainer == null || researchCardPrefab == null)
        {
            Debug.LogWarning("[ResearchPanelUI] Missing card container or prefab");
            return;
        }
        
        // Get filtered research definitions
        IEnumerable<ResearchDefinition> definitions;
        if (currentFilter.HasValue)
            definitions = ResearchService.Instance.GetByType(currentFilter.Value);
        else
            definitions = ResearchService.Instance.GetAllDefinitions();
        
        var sortedDefinitions = definitions.OrderBy(d => d.researchType).ThenBy(d => d.displayName).ToList();
        
        // Clear or reuse existing cards
        ClearExcessCards(sortedDefinitions.Count);
        
        // Create or update cards
        for (int i = 0; i < sortedDefinitions.Count; i++)
        {
            ResearchCardView card;
            
            if (i < cardViews.Count)
            {
                card = cardViews[i];
            }
            else
            {
                GameObject cardObj = Instantiate(researchCardPrefab, researchCardContainer);
                card = cardObj.GetComponent<ResearchCardView>();
                if (card == null)
                    card = cardObj.AddComponent<ResearchCardView>();
                cardViews.Add(card);
            }
            
            card.gameObject.SetActive(true);
            card.Bind(sortedDefinitions[i].id);
        }
    }
    
    private void ClearExcessCards(int requiredCount)
    {
        // Deactivate excess cards rather than destroying them for performance
        for (int i = requiredCount; i < cardViews.Count; i++)
        {
            if (cardViews[i] != null)
                cardViews[i].gameObject.SetActive(false);
        }
    }
    
    private void OnResearchEvent(string researchId)
    {
        RefreshUI();
    }
    
    private void OnResearchEvent(string researchId, int level)
    {
        RefreshUI();
    }

    private void OnResearchSlotsChanged()
    {
        RefreshUI();
    }
    
    private void OnCloseClicked()
    {
        ShowSlotsPanel(false);
        if (OptionsUIManager.Instance != null)
        {
            OptionsUIManager.Instance.HideSubPanels();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Periodically refresh info display for real-time updates
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            UpdateInfoDisplay();
            UpdateSlotProgress();
        }
    }

    private void UpdateResearchSlots()
    {
        if (researchSlotContainer == null)
            return;

        EnsureSlotViews();

        if (ResearchService.Instance == null)
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                var view = slotViews[i];
                if (view == null) continue;
                bool shouldShow = i == 0;
                view.gameObject.SetActive(shouldShow);
                if (!shouldShow) continue;
                view.Initialize(i, OnSlotUnlockRequested, OnEmptySlotSelected);
                view.Bind(new ResearchSlotDisplayData
                {
                    SlotIndex = i,
                    IsUnlocked = true,
                    IsActive = false,
                    AllowSelection = true
                });
            }
            return;
        }

        var service = ResearchService.Instance;

        int unlockedSlots = service.GetUnlockedSlotCount();

        var activeBySlot = new Dictionary<int, ResearchProgressData>();
        foreach (var progress in service.GetActiveResearch())
        {
            if (progress == null || !progress.isResearching)
                continue;
            if (progress.slotIndex < 0 || progress.slotIndex >= ResearchSystemConfig.MaxSlots)
                continue;
            if (!activeBySlot.ContainsKey(progress.slotIndex))
                activeBySlot.Add(progress.slotIndex, progress);
        }

        for (int i = 0; i < slotViews.Count; i++)
        {
            var view = slotViews[i];
            if (view == null)
                continue;

            bool isUnlocked = i < unlockedSlots;
            bool isNextUnlock = !isUnlocked && i == unlockedSlots && unlockedSlots < ResearchSystemConfig.MaxSlots;
            bool shouldShow = isUnlocked || isNextUnlock;
            view.gameObject.SetActive(shouldShow);
            if (!shouldShow)
                continue;

            view.Initialize(i, OnSlotUnlockRequested, OnEmptySlotSelected);

            bool isActive = activeBySlot.TryGetValue(i, out var progress);
            var definition = isActive ? service.GetDefinition(progress.researchId) : null;

            float unlockCost = 0f;
            bool canAfford = false;
            if (isNextUnlock)
            {
                canAfford = service.CanUnlockSlot(i, out unlockCost);
            }
            else
            {
                unlockCost = service.GetSlotUnlockCost(i);
            }

            float remaining = isActive ? service.GetRemainingTime(progress.researchId) : 0f;
            float duration = progress?.durationSeconds ?? 0f;

            var data = new ResearchSlotDisplayData
            {
                SlotIndex = i,
                IsUnlocked = isUnlocked,
                IsActive = isActive,
                IsNextUnlock = isNextUnlock,
                CanAffordUnlock = canAfford,
                UnlockCost = unlockCost,
                RemainingSeconds = remaining,
                DurationSeconds = duration,
                Progress = progress,
                Definition = definition,
                AllowSelection = isUnlocked && !isActive
            };

            view.Bind(data);
        }
    }

    private void UpdateSlotProgress()
    {
        foreach (var view in slotViews)
        {
            if (view == null || !view.gameObject.activeInHierarchy)
                continue;
            view.RefreshProgress();
        }
    }

    private void OnSlotUnlockRequested(int slotIndex)
    {
        if (ResearchService.Instance == null)
            return;

        if (!ResearchService.Instance.TryUnlockSlot(slotIndex))
        {
            Debug.Log($"[ResearchPanelUI] Unable to unlock research slot {slotIndex + 1}");
            return;
        }

        UpdateResearchSlots();
        UpdateInfoDisplay();
    }

    private void OnEmptySlotSelected(int slotIndex)
    {
        ShowResearchListPanel();
    }

    private void EnsureSlotViews()
    {
        if (slotViews.Count >= ResearchSystemConfig.MaxSlots)
            return;

        if (researchSlotContainer == null)
            return;

        // Use existing children first
        if (slotViews.Count == 0)
        {
            int index = 0;
            foreach (Transform child in researchSlotContainer)
            {
                if (index >= ResearchSystemConfig.MaxSlots)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                var view = child.GetComponent<ResearchSlotView>();
                if (view == null)
                {
                    Debug.LogWarning("[ResearchPanelUI] Research slot child is missing ResearchSlotView component");
                    continue;
                }

                view.Initialize(index, OnSlotUnlockRequested, OnEmptySlotSelected);
                slotViews.Add(view);
                index++;
            }
        }

        while (slotViews.Count < ResearchSystemConfig.MaxSlots)
        {
            if (researchSlotPrefab == null)
            {
                Debug.LogWarning("[ResearchPanelUI] Missing research slot prefab");
                return;
            }

            var slotObj = Instantiate(researchSlotPrefab, researchSlotContainer);
            slotObj.name = $"ResearchSlot_{slotViews.Count + 1}";
            var view = slotObj.GetComponent<ResearchSlotView>();
            if (view == null)
            {
                Debug.LogWarning("[ResearchPanelUI] Research slot prefab needs a ResearchSlotView component attached");
                Destroy(slotObj);
                return;
            }

            view.Initialize(slotViews.Count, OnSlotUnlockRequested, OnEmptySlotSelected);
            slotViews.Add(view);
        }
    }

    private void ShowSlotsPanel(bool updateCards = true)
    {
        isListVisible = false;
        ApplyListPanelState();
        if (!updateCards)
            return;
        ClearExcessCards(0);
    }

    private void ShowResearchListPanel()
    {
        if (isListVisible)
            return;

        isListVisible = true;
        ApplyListPanelState();
        UpdateResearchCards();
    }

    private void ApplyListPanelState()
    {
        if (researchSlotsPanel != null && !researchSlotsPanel.activeSelf)
            researchSlotsPanel.SetActive(true);

        if (researchCardPanel != null)
            researchCardPanel.SetActive(isListVisible);

        if (researchListBackButton != null)
            researchListBackButton.gameObject.SetActive(isListVisible);
    }
}
