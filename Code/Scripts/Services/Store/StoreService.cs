using System;
using System.Collections.Generic;
using UnityEngine;

public class StoreService : SingletonMonoBehaviour<StoreService>
{
    public struct PrismPackAvailability
    {
        public bool isAvailable;
        public bool isDailyFree;
        public bool isOnCooldown;
        public TimeSpan cooldownRemaining;

        public static PrismPackAvailability Unavailable() => new PrismPackAvailability
        {
            isAvailable = false,
            isDailyFree = false,
            isOnCooldown = false,
            cooldownRemaining = TimeSpan.Zero
        };
    }

    [Header("Prism Packs")]
    [Tooltip("Configure available prism packs. If empty, default packs will be generated at runtime.")]
    [SerializeField] private List<PrismPackDefinition> prismPackDefinitions = new List<PrismPackDefinition>();
    [Tooltip("Currency symbol shown alongside pack prices.")]
    [SerializeField] private string currencySymbol = "$";

    public event Action<PrismPackDefinition> PrismPackPurchased;

    private readonly Dictionary<string, PrismPackDefinition> packLookup = new Dictionary<string, PrismPackDefinition>();
    private PlayerManager playerManager;

    protected override void OnAwakeAfterInit()
    {
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

    public PrismPackAvailability GetPrismPackAvailability(string packId)
    {
        GetPrismPacks();

        if (string.IsNullOrEmpty(packId) || !packLookup.TryGetValue(packId, out var pack))
            return PrismPackAvailability.Unavailable();

        return GetPrismPackAvailability(pack);
    }

    public PrismPackAvailability GetPrismPackAvailability(PrismPackDefinition pack)
    {
        var availability = new PrismPackAvailability
        {
            isAvailable = true,
            isDailyFree = pack != null && pack.isDailyFree,
            isOnCooldown = false,
            cooldownRemaining = TimeSpan.Zero
        };

        if (pack == null)
        {
            availability.isAvailable = false;
            return availability;
        }

        if (playerManager == null)
            playerManager = PlayerManager.main;

        if (pack.isDailyFree && playerManager != null)
        {
            int cooldownHours = Mathf.Max(1, pack.dailyClaimCooldownHours);
            var cooldown = TimeSpan.FromHours(cooldownHours);

            var lastClaim = playerManager.GetLastStorePackClaimTime(pack.id);
            if (lastClaim.HasValue)
            {
                var elapsed = DateTime.UtcNow - lastClaim.Value;
                if (elapsed < cooldown)
                {
                    availability.isAvailable = false;
                    availability.isOnCooldown = true;
                    availability.cooldownRemaining = cooldown - elapsed;
                }
            }
        }

        return availability;
    }

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

        var availability = GetPrismPackAvailability(pack);
        if (!availability.isAvailable)
        {
            if (availability.isOnCooldown)
                Debug.LogWarning($"[StoreService] Pack '{pack.displayName}' is on cooldown. Available in {availability.cooldownRemaining:g}.");
            else
                Debug.LogWarning($"[StoreService] Pack '{pack.displayName}' is currently unavailable.");
            return false;
        }

        playerManager.AddCurrency(prisms: pack.prismAmount);

        if (pack.isDailyFree)
            playerManager.RecordStorePackClaim(pack.id, DateTime.UtcNow);

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
            CreateDailyPack("pack_prisms_daily", "Daily Free Prisms", 50, 24),
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

    private PrismPackDefinition CreateDailyPack(string id, string name, int amount, int cooldownHours)
    {
        return new PrismPackDefinition
        {
            id = id,
            displayName = name,
            prismAmount = amount,
            basePrice = 0f,
            discountPercent = 0f,
            description = $"Claim {amount} prisms for free once per day.",
            icon = null,
            productId = string.Empty,
            isDailyFree = true,
            dailyClaimCooldownHours = Mathf.Max(1, cooldownHours)
        };
    }
}
