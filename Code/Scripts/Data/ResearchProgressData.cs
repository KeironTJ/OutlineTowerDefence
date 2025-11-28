using System;

/// <summary>
/// Tracks the progress of a single research item
/// </summary>
[Serializable]
public class ResearchProgressData
{
    public string researchId;
    public int currentLevel;
    public bool isResearching;
    public string startTimeIsoUtc;
    public float durationSeconds;
    public int slotIndex;
    public bool isPaused;
    public float pausedRemainingSeconds;
    public float pausedInvestedCores;
    
    public ResearchProgressData()
    {
        researchId = "";
        currentLevel = 0;
        isResearching = false;
        startTimeIsoUtc = "";
        durationSeconds = 0f;
    slotIndex = -1;
        isPaused = false;
        pausedRemainingSeconds = 0f;
        pausedInvestedCores = 0f;
    }
    
    public ResearchProgressData(string id)
    {
        researchId = id;
        currentLevel = 0;
        isResearching = false;
        startTimeIsoUtc = "";
        durationSeconds = 0f;
        slotIndex = -1;
        isPaused = false;
        pausedRemainingSeconds = 0f;
        pausedInvestedCores = 0f;
    }
}

/// <summary>
/// System-wide research configuration
/// </summary>
[Serializable]
public class ResearchSystemConfig
{
    public const int MaxSlots = 5;
    private static readonly float[] SlotUnlockCosts = { 0f, 500f, 2000f, 4500f, 10000f };

    public int maxConcurrentResearch = 1;

    public ResearchSystemConfig()
    {
        SetUnlockedSlotCount(1);
    }

    public int GetUnlockedSlotCount()
    {
        if (maxConcurrentResearch < 1)
            maxConcurrentResearch = 1;
        if (maxConcurrentResearch > MaxSlots)
            maxConcurrentResearch = MaxSlots;
        return maxConcurrentResearch;
    }

    public void SetUnlockedSlotCount(int value)
    {
        if (value < 1)
            value = 1;
        if (value > MaxSlots)
            value = MaxSlots;
        maxConcurrentResearch = value;
    }

    public float GetUnlockCostForSlot(int slotIndex)
    {
        if (slotIndex < 0)
            return 0f;
        if (slotIndex >= SlotUnlockCosts.Length)
            return SlotUnlockCosts[SlotUnlockCosts.Length - 1];
        return SlotUnlockCosts[slotIndex];
    }

    public float GetNextUnlockCost()
    {
        int nextIndex = GetUnlockedSlotCount();
        if (nextIndex >= MaxSlots)
            return 0f;
        return GetUnlockCostForSlot(nextIndex);
    }

    public void EnsureValid()
    {
        SetUnlockedSlotCount(GetUnlockedSlotCount());
    }

    public static float GetDefaultUnlockCost(int slotIndex)
    {
        if (slotIndex < 0)
            return 0f;
        if (slotIndex >= SlotUnlockCosts.Length)
            return SlotUnlockCosts[SlotUnlockCosts.Length - 1];
        return SlotUnlockCosts[slotIndex];
    }
}
