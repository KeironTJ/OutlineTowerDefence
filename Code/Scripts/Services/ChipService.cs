using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChipService : MonoBehaviour, IStatContributor
{
    public static ChipService Instance { get; private set; }
    
    [SerializeField] private ChipDefinition[] loadedDefinitions;
    
    private readonly Dictionary<string, ChipDefinition> definitions = new Dictionary<string, ChipDefinition>();
    
    // Events
    public event Action<string> ChipUnlocked;
    public event Action<string, int> ChipUpgraded;
    public event Action<int, string> ChipEquipped;
    public event Action<int> ChipUnequipped;
    public event Action<int> SlotUnlocked;
    
    private PlayerManager playerManager;

    private bool EnsurePlayerManager()
    {
        if (playerManager == null)
            playerManager = PlayerManager.main;
        return playerManager != null;
    }

    private ChipSystemConfig GetChipConfigInternal()
    {
        return EnsurePlayerManager() ? playerManager.GetChipConfig() : null;
    }

    private List<ChipProgressData> GetChipProgressInternal()
    {
        return EnsurePlayerManager() ? playerManager.GetChipProgress() : null;
    }

    private List<ChipSlotData> GetEquippedSlotsInternal()
    {
        return EnsurePlayerManager() ? playerManager.GetEquippedChips() : null;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        IndexDefinitions();
    }
    
    private void Start()
    {
        playerManager = PlayerManager.main;
        
        // Hook up events to EventManager
        ChipUnlocked += OnChipUnlockedInternal;
        ChipUpgraded += OnChipUpgradedInternal;
        ChipEquipped += OnChipEquippedInternal;
        ChipUnequipped += OnChipUnequippedInternal;
        SlotUnlocked += OnSlotUnlockedInternal;
    }
    
    private void OnDestroy()
    {
        ChipUnlocked -= OnChipUnlockedInternal;
        ChipUpgraded -= OnChipUpgradedInternal;
        ChipEquipped -= OnChipEquippedInternal;
        ChipUnequipped -= OnChipUnequippedInternal;
        SlotUnlocked -= OnSlotUnlockedInternal;
    }
    
    private void OnChipUnlockedInternal(string chipId)
    {
        var def = GetDefinition(chipId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ChipUnlocked, 
                new ChipUnlockedEvent(chipId, def.chipName));
        }
    }
    
    private void OnChipUpgradedInternal(string chipId, int rarityLevel)
    {
        var def = GetDefinition(chipId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ChipUpgraded, 
                new ChipUpgradedEvent(chipId, def.chipName, rarityLevel, def.GetRarityEnum(rarityLevel)));
        }
    }
    
    private void OnChipEquippedInternal(int slotIndex, string chipId)
    {
        var def = GetDefinition(chipId);
        if (def != null)
        {
            EventManager.TriggerEvent(EventNames.ChipEquipped, 
                new ChipEquippedEvent(slotIndex, chipId, def.chipName));
        }
    }
    
    private void OnChipUnequippedInternal(int slotIndex)
    {
        EventManager.TriggerEvent(EventNames.ChipUnequipped, slotIndex);
    }
    
    private void OnSlotUnlockedInternal(int slotIndex)
    {
        EventManager.TriggerEvent(EventNames.ChipSlotUnlocked, slotIndex);
    }
    
    private void IndexDefinitions()
    {
        definitions.Clear();
        if (loadedDefinitions == null) return;
        
        foreach (var def in loadedDefinitions)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) continue;
            if (!definitions.ContainsKey(def.id))
                definitions.Add(def.id, def);
            else
                Debug.LogWarning($"[ChipService] Duplicate chip id: {def.id}");
        }
        
        Debug.Log($"[ChipService] Indexed {definitions.Count} chip definitions");
    }
    
    // Definition Access
    public ChipDefinition GetDefinition(string chipId)
    {
        return definitions.TryGetValue(chipId, out var def) ? def : null;
    }
    
    public IEnumerable<ChipDefinition> GetAllDefinitions()
    {
        return definitions.Values;
    }
    
    public IEnumerable<ChipDefinition> GetUnlockedDefinitions()
    {
        var progressList = GetChipProgressInternal();
        if (progressList == null) return Enumerable.Empty<ChipDefinition>();

        var unlockedIds = progressList
            .Where(p => p != null && p.unlocked)
            .Select(p => p.chipId)
            .ToHashSet();

        return definitions.Values.Where(d => unlockedIds.Contains(d.id));
    }
    
    // Chip Progress
    public ChipProgressData GetOrCreateProgress(string chipId)
    {
        var progressList = GetChipProgressInternal();
        if (progressList == null) return null;

        var progress = progressList.Find(p => p.chipId == chipId);
        if (progress == null)
        {
            progress = new ChipProgressData(chipId);
            progressList.Add(progress);
            if (EnsurePlayerManager())
                playerManager.SavePlayerData();
        }
        return progress;
    }
    
    public ChipProgressData GetProgress(string chipId)
    {
        var progressList = GetChipProgressInternal();
        if (progressList == null) return null;
        return progressList.Find(p => p.chipId == chipId);
    }
    
    public bool IsChipUnlocked(string chipId)
    {
        var progress = GetProgress(chipId);
        return progress != null && progress.unlocked;
    }
    
    // Chip Collection & Upgrade
    public bool TryAddChip(string chipId, int count = 1)
    {
        if (string.IsNullOrEmpty(chipId) || count <= 0) return false;
        if (!definitions.ContainsKey(chipId)) return false;
    if (!EnsurePlayerManager()) return false;
        
        var progress = GetOrCreateProgress(chipId);
        var def = definitions[chipId];
        
        bool wasUnlocked = progress.unlocked;
        progress.chipCount += count;
        
        // First chip unlocks it
        if (!wasUnlocked && progress.chipCount > 0)
        {
            progress.unlocked = true;
            ChipUnlocked?.Invoke(chipId);
        }
        
        // Check for rarity upgrade
        int previousRarity = progress.rarityLevel;
        int maxRarity = def.GetMaxRarity();
        
        for (int rarity = previousRarity + 1; rarity <= maxRarity; rarity++)
        {
            int requiredCount = def.GetChipsNeededForRarity(rarity);
            if (progress.chipCount >= requiredCount)
            {
                progress.rarityLevel = rarity;
                ChipUpgraded?.Invoke(chipId, rarity);
            }
            else
            {
                break;
            }
        }
        
        playerManager.SavePlayerData();
        return true;
    }
    
    // Slot Management
    public int GetUnlockedSlotCount()
    {
        var config = GetChipConfigInternal();
        if (config == null) return 0;

        int unlocked = Mathf.Clamp(config.unlockedSlots, 0, config.maxSlots);
        if (unlocked < 1)
            unlocked = 1;

        var equipped = GetEquippedSlotsInternal();
        if (equipped != null)
        {
            foreach (var slot in equipped)
            {
                if (slot == null) continue;
                int desiredCount = slot.slotIndex + 1;
                if (desiredCount > unlocked)
                    unlocked = Mathf.Min(desiredCount, config.maxSlots);
            }
        }

        if (config.unlockedSlots != unlocked)
        {
            config.unlockedSlots = Mathf.Min(unlocked, config.maxSlots);
            if (EnsurePlayerManager())
                playerManager.SavePlayerData();
        }

        return Mathf.Min(unlocked, config.maxSlots);
    }
    
    public int GetMaxSlotCount()
    {
        var config = GetChipConfigInternal();
        return config?.maxSlots ?? 10;
    }
    
    public int GetNextSlotCost()
    {
        var config = GetChipConfigInternal();
        if (config == null) return int.MaxValue;

        int unlocked = GetUnlockedSlotCount();
        if (unlocked >= config.maxSlots) return int.MaxValue;

        int nextSlot = Mathf.Clamp(unlocked + 1, 1, config.maxSlots);
        return config.GetSlotUnlockCost(nextSlot);
    }
    
    public bool CanUnlockSlot()
    {
        if (!EnsurePlayerManager()) return false;
        
        var config = GetChipConfigInternal();
        if (config == null) return false;
        
        int unlocked = GetUnlockedSlotCount();
        if (unlocked >= config.maxSlots) return false;
        
        int cost = GetNextSlotCost();
        if (cost == int.MaxValue) return false;
        
        return playerManager.GetPrisms() >= cost;
    }
    
    public bool TryUnlockSlot()
    {
        if (!CanUnlockSlot()) return false;

        var config = GetChipConfigInternal();
        if (config == null) return false;

        int cost = GetNextSlotCost();
        if (cost == int.MaxValue) return false;
        if (!playerManager.TrySpend(CurrencyType.Prisms, cost))
            return false;

        int unlockedBefore = GetUnlockedSlotCount();
        config.unlockedSlots = Mathf.Min(unlockedBefore + 1, config.maxSlots);
        playerManager.SavePlayerData();
        
        SlotUnlocked?.Invoke(config.unlockedSlots);
        return true;
    }
    
    // Equip Management
    public string GetEquippedChip(int slotIndex)
    {
        var equipped = GetEquippedSlotsInternal();
        if (equipped == null) return string.Empty;

        var slot = equipped.Find(s => s.slotIndex == slotIndex);
        return slot?.equippedChipId ?? string.Empty;
    }
    
    public int GetSlotIndexForChip(string chipId)
    {
        if (string.IsNullOrEmpty(chipId)) return -1;
        var equipped = GetEquippedSlotsInternal();
        if (equipped == null) return -1;

        var slot = equipped.Find(s => s.equippedChipId == chipId);
        return slot?.slotIndex ?? -1;
    }

    public bool IsChipEquipped(string chipId)
    {
        var equipped = GetEquippedSlotsInternal();
        if (equipped == null) return false;
        return equipped.Any(s => s.equippedChipId == chipId);
    }
    
    public bool CanEquipChip(string chipId, int slotIndex)
    {
        if (string.IsNullOrEmpty(chipId)) return false;
        if (slotIndex < 0 || slotIndex >= GetUnlockedSlotCount()) return false;
        if (!IsChipUnlocked(chipId)) return false;
        
        // Check if already equipped in another slot
        if (IsChipEquipped(chipId)) return false;
        
        return true;
    }
    
    public bool TryEquipChip(string chipId, int slotIndex)
    {
        if (!CanEquipChip(chipId, slotIndex)) return false;
        if (!EnsurePlayerManager()) return false;
        
        var equipped = GetEquippedSlotsInternal();
        if (equipped == null) return false;
        
        // Find or create slot
        var slot = equipped.Find(s => s.slotIndex == slotIndex);
        if (slot == null)
        {
            slot = new ChipSlotData(slotIndex);
            equipped.Add(slot);
        }
        
        slot.equippedChipId = chipId;
        playerManager.SavePlayerData();
        
        ChipEquipped?.Invoke(slotIndex, chipId);
        return true;
    }
    
    public bool TryUnequipChip(int slotIndex)
    {
    var equipped = GetEquippedSlotsInternal();
    if (equipped == null) return false;
        
    var slot = equipped.Find(s => s.slotIndex == slotIndex);
        if (slot == null || string.IsNullOrEmpty(slot.equippedChipId)) return false;
        
        slot.equippedChipId = string.Empty;
        playerManager.SavePlayerData();
        
        ChipUnequipped?.Invoke(slotIndex);
        return true;
    }
    
    // Bonus Calculation
    public Dictionary<ChipBonusType, float> GetActiveChipBonuses()
    {
        var bonuses = new Dictionary<ChipBonusType, float>();
        
        var equipped = GetEquippedSlotsInternal();
        if (equipped == null)
            return bonuses;
        
        foreach (var slot in equipped)
        {
            if (string.IsNullOrEmpty(slot.equippedChipId)) continue;
            
            var def = GetDefinition(slot.equippedChipId);
            if (def == null) continue;
            
            var progress = GetProgress(slot.equippedChipId);
            if (progress == null || !progress.unlocked) continue;
            
            float bonus = def.GetBonusAtRarity(progress.rarityLevel);
            
            if (!bonuses.ContainsKey(def.bonusType))
                bonuses[def.bonusType] = 0f;
            
            bonuses[def.bonusType] += bonus;
        }
        
        return bonuses;
    }
    
    public float GetBonusValue(ChipBonusType bonusType)
    {
        var bonuses = GetActiveChipBonuses();
        return bonuses.TryGetValue(bonusType, out float value) ? value : 0f;
    }
    
    // Purchase System
    public int GetChipPurchaseCost()
    {
        var config = GetChipConfigInternal();
        return config?.basePurchaseCost ?? 50;
    }
    
    public bool CanPurchaseChip()
    {
        if (!EnsurePlayerManager()) return false;
        int cost = GetChipPurchaseCost();
        return playerManager.GetPrisms() >= cost;
    }
    
    public bool TryPurchaseRandomChip(out string purchasedChipId)
    {
        purchasedChipId = null;
        
        if (!EnsurePlayerManager()) return false;
        if (!CanPurchaseChip()) return false;
        
        int cost = GetChipPurchaseCost();
        if (!playerManager.TrySpend(CurrencyType.Prisms, cost))
            return false;
        
        // Get eligible chips (not maxed out)
        var eligibleChips = new List<ChipDefinition>();
        foreach (var def in definitions.Values)
        {
            var progress = GetProgress(def.id);
            if (progress == null || progress.rarityLevel < def.GetMaxRarity())
            {
                eligibleChips.Add(def);
            }
        }
        
        if (eligibleChips.Count == 0)
        {
            Debug.LogWarning("[ChipService] No eligible chips for purchase (all maxed)");
            // Refund
            playerManager.AddCurrency(prisms: cost);
            return false;
        }
        
        // Random selection
        var selected = eligibleChips[UnityEngine.Random.Range(0, eligibleChips.Count)];
        purchasedChipId = selected.id;
        
        TryAddChip(selected.id, 1);
        return true;
    }

    public void Contribute(StatCollector collector)
    {
        if (collector == null) return;

        var bonuses = GetActiveChipBonuses();
        if (bonuses == null || bonuses.Count == 0)
            return;

        const float PercentToScalar = 0.01f;

        foreach (var entry in bonuses)
        {
            float bonus = entry.Value;
            switch (entry.Key)
            {
                case ChipBonusType.AttackDamageMultiplier:
                    collector.AddPercentage(StatId.AttackDamage, bonus * PercentToScalar);
                    break;
                case ChipBonusType.AttackSpeed:
                    collector.AddPercentage(StatId.AttackSpeed, bonus * PercentToScalar);
                    break;
                case ChipBonusType.CoresPerKillMultiplier:
                    collector.AddPercentage(StatId.CoresPerKillMultiplier, bonus * PercentToScalar);
                    break;
                case ChipBonusType.Health:
                    collector.AddPercentage(StatId.MaxHealth, bonus * PercentToScalar);
                    break;
                case ChipBonusType.HealthRecoverySpeed:
                    collector.AddPercentage(StatId.HealPerSecond, bonus * PercentToScalar);
                    break;
                case ChipBonusType.FragmentsBoost:
                    collector.AddPercentage(StatId.FragmentMultiplier, bonus * PercentToScalar);
                    break;
                case ChipBonusType.CriticalChance:
                {
                    float critAdd = Mathf.Clamp01(Mathf.Max(0f, bonus * PercentToScalar));
                    collector.AddPercentage(StatId.CritChance, critAdd);
                    break;
                }
                case ChipBonusType.CriticalDamage:
                    collector.AddPercentage(StatId.CritMultiplier, bonus * PercentToScalar);
                    break;
                case ChipBonusType.ProjectileSpeed:
                    collector.AddPercentage(StatId.BulletSpeed, bonus * PercentToScalar);
                    break;
                case ChipBonusType.TurretRange:
                    collector.AddPercentage(StatId.TargetingRange, bonus * PercentToScalar);
                    break;
                default:
                    break;
            }
        }
    }
}
