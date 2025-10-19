using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform packListContainer;
    [SerializeField] private GameObject packItemPrefab;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private Button closeButton;

    private readonly List<PrismPackView> packViews = new List<PrismPackView>();

    private StoreService storeService;
    private PlayerManager playerManager;

    private void Awake()
    {
        storeService = StoreService.Instance;
        playerManager = PlayerManager.main;

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        if (storeService == null) storeService = StoreService.Instance;
        if (playerManager == null) playerManager = PlayerManager.main;

        PopulateStore();
        UpdateBalanceText();

        if (storeService != null)
            storeService.PrismPackPurchased += OnPackPurchased;
    }

    private void OnDisable()
    {
        if (storeService != null)
            storeService.PrismPackPurchased -= OnPackPurchased;
    }

    private void PopulateStore()
    {
        ClearExistingViews();

        if (storeService == null || packItemPrefab == null || packListContainer == null)
        {
            Debug.LogWarning("[StoreUI] Missing references. Cannot populate store.");
            return;
        }

        var packs = storeService.GetPrismPacks();
        string currencySymbol = storeService.GetCurrencySymbol();

        foreach (var pack in packs)
        {
            if (pack == null) continue;

            var instance = Instantiate(packItemPrefab, packListContainer);
            var view = instance.GetComponent<PrismPackView>() ?? instance.GetComponentInChildren<PrismPackView>();
            if (view == null)
            {
                Debug.LogError("[StoreUI] Pack prefab is missing a PrismPackView component. Destroying instance to avoid issues.");
                Destroy(instance);
                continue;
            }

            view.Apply(pack, currencySymbol, () => OnPurchaseClicked(pack.id));
            packViews.Add(view);
        }
    }

    private void OnPurchaseClicked(string packId)
    {
        if (storeService == null)
        {
            Debug.LogWarning("[StoreUI] StoreService not available.");
            return;
        }

        if (!storeService.TryPurchasePrismPack(packId))
        {
            Debug.LogWarning($"[StoreUI] Failed to purchase prism pack '{packId}'.");
        }
    }

    private void OnPackPurchased(PrismPackDefinition pack)
    {
        UpdateBalanceText();
    }

    private void UpdateBalanceText()
    {
        if (balanceText == null || playerManager == null) return;

        float prisms = playerManager.GetPrisms();
        balanceText.text = $"{prisms:N0}";
    }

    private void ClearExistingViews()
    {
        foreach (var view in packViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        packViews.Clear();
    }
}
