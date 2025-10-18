using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChipProgressData
{
    public string chipId;
    public int chipCount; // Total chips collected
    public int rarityLevel; // Current rarity level (0-4)
    public bool unlocked; // Has the player unlocked this chip (collected at least 1)?
    
    public ChipProgressData(string chipId)
    {
        this.chipId = chipId;
        this.chipCount = 0;
        this.rarityLevel = 0;
        this.unlocked = false;
    }
}

[Serializable]
public class ChipSlotData
{
    public int slotIndex;
    public string equippedChipId; // Empty string if no chip equipped
    
    public ChipSlotData(int slotIndex)
    {
        this.slotIndex = slotIndex;
        this.equippedChipId = string.Empty;
    }
}

[Serializable]
public class ChipSystemConfig
{
    public int unlockedSlots; // Number of chip slots the player has unlocked
    public int maxSlots = 10; // Maximum number of slots that can be unlocked
    public int baseSlotCost = 50; // Base cost in Prisms to unlock a slot
    public float slotCostMultiplier = 1.5f; // Multiplier for each additional slot
    public int basePurchaseCost = 20; // Base cost in Prisms to purchase a chip pack
    
    public ChipSystemConfig()
    {
        unlockedSlots = 0;
        maxSlots = 10;
        baseSlotCost = 100;
        slotCostMultiplier = 1.5f;
        basePurchaseCost = 50;
    }
    
    public int GetSlotUnlockCost(int nextSlotIndex)
    {
        if (nextSlotIndex <= 0) return 0;
        return Mathf.RoundToInt(baseSlotCost * Mathf.Pow(slotCostMultiplier, nextSlotIndex - 1));
    }
}
