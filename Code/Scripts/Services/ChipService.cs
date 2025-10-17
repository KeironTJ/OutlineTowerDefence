using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChipService : MonoBehaviour
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
        if (playerManager?.playerData == null) return Enumerable.Empty<ChipDefinition>();
        
        var unlockedIds = playerManager.playerData.chipProgress
            .Where(p => p.unlocked)
            .Select(p => p.chipId)
            .ToHashSet();
            
        return definitions.Values.Where(d => unlockedIds.Contains(d.id));
    }
    
    // Chip Progress
    public ChipProgressData GetOrCreateProgress(string chipId)
    {
        if (playerManager?.playerData == null) return null;
        
        var progress = playerManager.playerData.chipProgress.Find(p => p.chipId == chipId);
        if (progress == null)
        {
            progress = new ChipProgressData(chipId);
            playerManager.playerData.chipProgress.Add(progress);
        }
        return progress;
    }
    
    public ChipProgressData GetProgress(string chipId)
    {
        if (playerManager?.playerData == null) return null;
        return playerManager.playerData.chipProgress.Find(p => p.chipId == chipId);
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
        if (playerManager?.playerData == null) return false;
        
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
        if (playerManager?.playerData?.chipConfig == null) return 0;
        return playerManager.playerData.chipConfig.unlockedSlots;
    }
    
    public int GetMaxSlotCount()
    {
        if (playerManager?.playerData?.chipConfig == null) return 10;
        return playerManager.playerData.chipConfig.maxSlots;
    }
    
    public int GetNextSlotCost()
    {
        if (playerManager?.playerData?.chipConfig == null) return int.MaxValue;
        int nextSlot = playerManager.playerData.chipConfig.unlockedSlots + 1;
        return playerManager.playerData.chipConfig.GetSlotUnlockCost(nextSlot);
    }
    
    public bool CanUnlockSlot()
    {
        if (playerManager?.playerData == null) return false;
        
        int current = GetUnlockedSlotCount();
        int max = GetMaxSlotCount();
        
        if (current >= max) return false;
        
        int cost = GetNextSlotCost();
        return playerManager.GetPrisms() >= cost;
    }
    
    public bool TryUnlockSlot()
    {
        if (!CanUnlockSlot()) return false;
        
        int cost = GetNextSlotCost();
        if (!playerManager.TrySpend(CurrencyType.Prisms, cost))
            return false;
        
        playerManager.playerData.chipConfig.unlockedSlots++;
        playerManager.SavePlayerData();
        
        SlotUnlocked?.Invoke(playerManager.playerData.chipConfig.unlockedSlots);
        return true;
    }
    
    // Equip Management
    public string GetEquippedChip(int slotIndex)
    {
        if (playerManager?.playerData?.equippedChips == null) return string.Empty;
        
        var slot = playerManager.playerData.equippedChips.Find(s => s.slotIndex == slotIndex);
        return slot?.equippedChipId ?? string.Empty;
    }
    
    public bool IsChipEquipped(string chipId)
    {
        if (playerManager?.playerData?.equippedChips == null) return false;
        return playerManager.playerData.equippedChips.Any(s => s.equippedChipId == chipId);
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
        if (playerManager?.playerData == null) return false;
        
        if (playerManager.playerData.equippedChips == null)
            playerManager.playerData.equippedChips = new List<ChipSlotData>();
        
        // Find or create slot
        var slot = playerManager.playerData.equippedChips.Find(s => s.slotIndex == slotIndex);
        if (slot == null)
        {
            slot = new ChipSlotData(slotIndex);
            playerManager.playerData.equippedChips.Add(slot);
        }
        
        slot.equippedChipId = chipId;
        playerManager.SavePlayerData();
        
        ChipEquipped?.Invoke(slotIndex, chipId);
        return true;
    }
    
    public bool TryUnequipChip(int slotIndex)
    {
        if (playerManager?.playerData?.equippedChips == null) return false;
        
        var slot = playerManager.playerData.equippedChips.Find(s => s.slotIndex == slotIndex);
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
        
        if (playerManager?.playerData?.equippedChips == null)
            return bonuses;
        
        foreach (var slot in playerManager.playerData.equippedChips)
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
        if (playerManager?.playerData?.chipConfig == null) return 50;
        return playerManager.playerData.chipConfig.basePurchaseCost;
    }
    
    public bool CanPurchaseChip()
    {
        if (playerManager == null) return false;
        int cost = GetChipPurchaseCost();
        return playerManager.GetPrisms() >= cost;
    }
    
    public bool TryPurchaseRandomChip(out string purchasedChipId)
    {
        purchasedChipId = null;
        
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
}
