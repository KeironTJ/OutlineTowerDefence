using UnityEngine;
using TMPro;

/// <summary>
/// Debug component for testing the Chips system in play mode.
/// Attach to a GameObject in your scene for easy testing.
/// </summary>
public class ChipDebugger : MonoBehaviour
{
    [Header("Debug Actions")]
    [SerializeField] private bool showDebugUI = true;
    
    [Header("Quick Actions")]
    [Tooltip("Add this many Prisms for testing")]
    [SerializeField] private int testPrismsAmount = 1000;
    
    [Tooltip("Chip ID to add (leave empty for random)")]
    [SerializeField] private string testChipId = "";
    
    [Tooltip("Number of chips to add")]
    [SerializeField] private int testChipCount = 1;
    
    private ChipService chipService;
    private PlayerManager playerManager;
    private Rect windowRect = new Rect(20, 20, 300, 400);
    
    private void Start()
    {
        chipService = ChipService.Instance;
        playerManager = PlayerManager.main;
    }
    
    private void OnGUI()
    {
        if (!showDebugUI) return;
        if (chipService == null || playerManager == null) return;
        
        windowRect = GUI.Window(0, windowRect, DrawDebugWindow, "Chip Debugger");
    }
    
    private void DrawDebugWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Currency Info
        GUILayout.Label("=== Currency ===", GUI.skin.box);
        GUILayout.Label($"Prisms: {playerManager.GetPrisms():F0}");
        GUILayout.Label($"Cores: {playerManager.GetCores():F0}");
        
        if (GUILayout.Button($"Add {testPrismsAmount} Prisms"))
        {
            playerManager.AddCurrency(prisms: testPrismsAmount);
            Debug.Log($"[ChipDebugger] Added {testPrismsAmount} Prisms");
        }
        
        GUILayout.Space(10);
        
        // Slot Info
        GUILayout.Label("=== Chip Slots ===", GUI.skin.box);
        int unlockedSlots = chipService.GetUnlockedSlotCount();
        int maxSlots = chipService.GetMaxSlotCount();
        GUILayout.Label($"Slots: {unlockedSlots}/{maxSlots}");
        
        if (unlockedSlots < maxSlots)
        {
            int cost = chipService.GetNextSlotCost();
            if (GUILayout.Button($"Unlock Slot ({cost} Prisms)"))
            {
                if (chipService.TryUnlockSlot())
                    Debug.Log("[ChipDebugger] Unlocked new slot");
                else
                    Debug.LogWarning("[ChipDebugger] Failed to unlock slot");
            }
        }
        else
        {
            GUILayout.Label("All slots unlocked!");
        }
        
        GUILayout.Space(10);
        
        // Purchase
        GUILayout.Label("=== Purchase ===", GUI.skin.box);
        int purchaseCost = chipService.GetChipPurchaseCost();
        if (GUILayout.Button($"Purchase Random Chip ({purchaseCost} Prisms)"))
        {
            if (chipService.TryPurchaseRandomChip(out string chipId))
                Debug.Log($"[ChipDebugger] Purchased chip: {chipId}");
            else
                Debug.LogWarning("[ChipDebugger] Failed to purchase chip");
        }
        
        GUILayout.Space(10);
        
        // Add Specific Chip
        GUILayout.Label("=== Test Add Chip ===", GUI.skin.box);
        if (GUILayout.Button($"Add {testChipCount}x '{testChipId}'"))
        {
            if (string.IsNullOrEmpty(testChipId))
            {
                // Add random chip
                var allChips = chipService.GetAllDefinitions();
                var chipArray = new System.Collections.Generic.List<ChipDefinition>(allChips);
                if (chipArray.Count > 0)
                {
                    var randomChip = chipArray[Random.Range(0, chipArray.Count)];
                    chipService.TryAddChip(randomChip.id, testChipCount);
                    Debug.Log($"[ChipDebugger] Added {testChipCount}x {randomChip.id}");
                }
            }
            else
            {
                chipService.TryAddChip(testChipId, testChipCount);
                Debug.Log($"[ChipDebugger] Added {testChipCount}x {testChipId}");
            }
        }
        
        GUILayout.Space(10);
        
        // Active Bonuses
        GUILayout.Label("=== Active Bonuses ===", GUI.skin.box);
        var bonuses = chipService.GetActiveChipBonuses();
        if (bonuses.Count == 0)
        {
            GUILayout.Label("No chips equipped");
        }
        else
        {
            foreach (var bonus in bonuses)
            {
                GUILayout.Label($"{bonus.Key}: +{bonus.Value:F1}");
            }
        }
        
        GUILayout.Space(10);
        
        // Equipped Chips
        GUILayout.Label("=== Equipped Chips ===", GUI.skin.box);
        bool hasEquipped = false;
        for (int i = 0; i < unlockedSlots; i++)
        {
            string chipId = chipService.GetEquippedChip(i);
            if (!string.IsNullOrEmpty(chipId))
            {
                var def = chipService.GetDefinition(chipId);
                var progress = chipService.GetProgress(chipId);
                if (def != null && progress != null)
                {
                    GUILayout.Label($"Slot {i}: {def.chipName} ({def.GetRarityEnum(progress.rarityLevel)})");
                    hasEquipped = true;
                }
            }
        }
        if (!hasEquipped)
        {
            GUILayout.Label("No chips equipped");
        }
        
        GUILayout.Space(10);
        
        // Unlocked Chips Count
        GUILayout.Label("=== Collection ===", GUI.skin.box);
        var unlockedChips = chipService.GetUnlockedDefinitions();
        var allChipsCount = 0;
        foreach (var _ in chipService.GetAllDefinitions()) allChipsCount++;
        var unlockedCount = 0;
        foreach (var _ in unlockedChips) unlockedCount++;
        GUILayout.Label($"Unlocked: {unlockedCount}/{allChipsCount} chips");
        
        GUILayout.EndVertical();
        GUI.DragWindow();
    }
    
    // Keyboard shortcuts
    private void Update()
    {
        if (!showDebugUI) return;
        
        // P = Add Prisms
        if (Input.GetKeyDown(KeyCode.P))
        {
            playerManager.AddCurrency(prisms: testPrismsAmount);
            Debug.Log($"[ChipDebugger] Hotkey: Added {testPrismsAmount} Prisms");
        }
        
        // U = Unlock Slot
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (chipService.TryUnlockSlot())
                Debug.Log("[ChipDebugger] Hotkey: Unlocked new slot");
        }
        
        // B = Buy chip
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (chipService.TryPurchaseRandomChip(out string chipId))
                Debug.Log($"[ChipDebugger] Hotkey: Purchased chip: {chipId}");
        }
        
        // H = Toggle UI
        if (Input.GetKeyDown(KeyCode.H))
        {
            showDebugUI = !showDebugUI;
            Debug.Log($"[ChipDebugger] Debug UI: {showDebugUI}");
        }
    }
}
