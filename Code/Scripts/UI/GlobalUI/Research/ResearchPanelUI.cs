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
    [SerializeField] private Transform researchCardContainer;
    [SerializeField] private GameObject researchCardPrefab;
    [SerializeField] private Button closeButton;
    
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
    
    private void OnEnable()
    {
        RefreshUI();
        
        // Subscribe to research events
        if (ResearchService.Instance != null)
        {
            ResearchService.Instance.ResearchStarted += OnResearchEvent;
            ResearchService.Instance.ResearchCompleted += OnResearchEvent;
            ResearchService.Instance.ResearchSpedUp += OnResearchEvent;
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
        }
    }
    
    private void Start()
    {
        SetupButtons();
        RefreshUI();
    }
    
    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        
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
        
        UpdateInfoDisplay();
        UpdateResearchCards();
    }
    
    private void UpdateInfoDisplay()
    {
        // Active research count
        if (activeResearchText != null)
        {
            int activeCount = ResearchService.Instance.GetActiveResearchCount();
            var config = PlayerManager.main?.GetResearchConfig();
            int maxConcurrent = config?.maxConcurrentResearch ?? 1;
            activeResearchText.text = $"Active Research: {activeCount}/{maxConcurrent}";
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
    
    private void OnCloseClicked()
    {
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
        }
    }
}
