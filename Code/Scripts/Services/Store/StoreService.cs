using System;
using System.Collections.Generic;
using UnityEngine;

public class StoreService : MonoBehaviour
{
    public static StoreService Instance { get; private set; }

    [Header("Prism Packs")]
    [Tooltip("Configure available prism packs. If empty, default packs will be generated at runtime.")]
    [SerializeField] private List<PrismPackDefinition> prismPackDefinitions = new List<PrismPackDefinition>();
    [Tooltip("Currency symbol shown alongside pack prices.")]
    [SerializeField] private string currencySymbol = "$";

    public event Action<PrismPackDefinition> PrismPackPurchased;

    private readonly Dictionary<string, PrismPackDefinition> packLookup = new Dictionary<string, PrismPackDefinition>();
    private PlayerManager playerManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheDefinitions();
    }

    private void Start()
    {
        playerManager = PlayerManager.main;
    }

    public IReadOnlyList<PrismPackDefinition> GetPrismPacks()
    {
        if (prismPackDefinitions == null || prismPackDefinitions.Count == 0)
        {
            prismPackDefinitions = GenerateDefaultPacks();
            CacheDefinitions();
        }

        return prismPackDefinitions;
    }

    public string GetCurrencySymbol() => currencySymbol;

    public bool TryPurchasePrismPack(string packId)
    {
        if (string.IsNullOrEmpty(packId)) return false;

        if (!packLookup.TryGetValue(packId, out var pack))
        {
            Debug.LogWarning($"[StoreService] Unknown prism pack id '{packId}'.");
            return false;
        }

        if (playerManager == null)
            playerManager = PlayerManager.main;

        if (playerManager == null)
        {
            Debug.LogError("[StoreService] PlayerManager not available. Cannot grant prisms.");
            return false;
        }

        playerManager.AddCurrency(prisms: pack.prismAmount);
        playerManager.SavePlayerData();

    Debug.Log($"[StoreService] Granted {pack.prismAmount} prisms for pack '{pack.displayName}'.");
    PrismPackPurchased?.Invoke(pack);
    EventManager.TriggerEvent(EventNames.CurrencyEarned, new CurrencyEarnedEvent(0f, 0f, pack.prismAmount, 0f, "PrismStore"));

        return true;
    }

    private void CacheDefinitions()
    {
        packLookup.Clear();

        if (prismPackDefinitions == null || prismPackDefinitions.Count == 0)
            return;

        foreach (var def in prismPackDefinitions)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;

            if (packLookup.ContainsKey(def.id))
            {
                Debug.LogWarning($"[StoreService] Duplicate pack id '{def.id}' detected. Using the first occurrence.");
                continue;
            }

            packLookup.Add(def.id, def);
        }
    }

    private List<PrismPackDefinition> GenerateDefaultPacks()
    {
        var defaultPacks = new List<PrismPackDefinition>
        {
            CreatePack("pack_prisms_100", "Prism Pack (100)", 100, 1.99f, 0f),
            CreatePack("pack_prisms_250", "Prism Pack (250)", 250, 4.49f, 5f),
            CreatePack("pack_prisms_1000", "Prism Pack (1000)", 1000, 14.99f, 10f),
            CreatePack("pack_prisms_2500", "Prism Pack (2500)", 2500, 29.99f, 15f),
            CreatePack("pack_prisms_5000", "Prism Pack (5000)", 5000, 49.99f, 20f),
            CreatePack("pack_prisms_9999", "Prism Pack (9999)", 9999, 79.99f, 25f)
        };

        return defaultPacks;
    }

    private PrismPackDefinition CreatePack(string id, string name, int amount, float basePrice, float discount)
    {
        return new PrismPackDefinition
        {
            id = id,
            displayName = name,
            prismAmount = amount,
            basePrice = basePrice,
            discountPercent = discount,
            description = $"Grants {amount} prisms instantly.",
            icon = null,
            productId = string.Empty
        };
    }
}
