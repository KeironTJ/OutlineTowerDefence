using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChipSelectorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chipListContainer;
    [SerializeField] private GameObject chipButtonPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI purchaseCostText;
    
    [Header("Slot Management")]
    [SerializeField] private Transform chipSlotsContainer;
    [SerializeField] private GameObject chipSlotPrefab;
    [SerializeField] private Button unlockSlotButton;
    [SerializeField] private TextMeshProUGUI unlockSlotCostText;
    [SerializeField] private TextMeshProUGUI slotsInfoText;
    
    [Header("Chip Details Panel")]
    [SerializeField] private GameObject chipDetailsPanel;
    [SerializeField] private TextMeshProUGUI chipNameText;
    [SerializeField] private TextMeshProUGUI chipDescriptionText;
    [SerializeField] private Image chipIconImage;
    [SerializeField] private TextMeshProUGUI chipRarityText;
    [SerializeField] private TextMeshProUGUI chipBonusText;
    [SerializeField] private TextMeshProUGUI chipProgressText;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    
    private ChipService chipService;
    private PlayerManager playerManager;
    private LoadoutScreen loadoutScreen;
    
    private readonly List<ChipListItemView> chipItemViews = new List<ChipListItemView>();
    private readonly List<ChipSlotView> slotViews = new List<ChipSlotView>();
    
    private int selectedSlotIndex = -1;
    private string selectedChipId = null;
    
    private void Awake()
    {
        chipService = ChipService.Instance;
        playerManager = PlayerManager.main;
        
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseChipClicked);
        
        if (unlockSlotButton != null)
            unlockSlotButton.onClick.AddListener(OnUnlockSlotClicked);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);
        
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipClicked);
    }
    
    private void OnEnable()
    {
        if (chipService == null) chipService = ChipService.Instance;
        if (playerManager == null) playerManager = PlayerManager.main;
        
        RefreshUI();
    }
    
    public void SetLoadoutScreen(LoadoutScreen screen)
    {
        loadoutScreen = screen;
    }
    
    public void RefreshUI()
    {
        EnsureSelectedSlotIndex();
        RefreshChipSlots();
        RefreshChipList();
        RefreshPurchaseButton();
        RefreshSlotUnlockButton();
        RefreshDetailsPanel();
    }
    
    private void EnsureSelectedSlotIndex()
    {
        if (chipService == null) return;

        int unlockedCount = chipService.GetUnlockedSlotCount();
        if (unlockedCount <= 0)
        {
            selectedSlotIndex = -1;
            return;
        }

        if (selectedSlotIndex >= unlockedCount)
            selectedSlotIndex = unlockedCount - 1;

        if (selectedSlotIndex < 0 && !string.IsNullOrEmpty(selectedChipId))
            selectedSlotIndex = 0;
    }

    private void EnsureSlotViewCapacity(int desiredCount)
    {
        if (chipSlotsContainer == null) return;

        slotViews.RemoveAll(view => view == null);

        foreach (Transform child in chipSlotsContainer)
        {
            if (slotViews.Count >= desiredCount) break;

            var view = child.GetComponent<ChipSlotView>();
            if (view != null && !slotViews.Contains(view))
                slotViews.Add(view);
        }

        while (slotViews.Count < desiredCount && chipSlotPrefab != null)
        {
            var instance = Instantiate(chipSlotPrefab, chipSlotsContainer);
            var view = instance.GetComponent<ChipSlotView>();
            if (view == null)
            {
                Debug.LogError("[ChipSelectorUI] Chip slot prefab is missing a ChipSlotView component. Destroying the instance to avoid an infinite loop.");
                Destroy(instance);
                break;
            }

            slotViews.Add(view);
        }
    }

    private void RefreshChipSlots()
    {
        if (chipService == null || chipSlotsContainer == null) return;
        
        int unlockedCount = chipService.GetUnlockedSlotCount();
        int maxCount = chipService.GetMaxSlotCount();
        
        // Update slots info text
        if (slotsInfoText != null)
            slotsInfoText.text = $"Chip Slots: {unlockedCount}/{maxCount}";

        EnsureSlotViewCapacity(unlockedCount);

        for (int i = 0; i < slotViews.Count; i++)
        {
            var view = slotViews[i];
            if (view == null) continue;

            bool withinUnlocked = i < unlockedCount;
            if (view.gameObject != null)
                view.gameObject.SetActive(withinUnlocked);

            int slotIndex = i;
            view.ConfigureButton(withinUnlocked ? () => OnSlotClicked(slotIndex) : (UnityEngine.Events.UnityAction)null);

            if (!withinUnlocked)
            {
                view.BindLocked();
                continue;
            }

            string equippedChipId = chipService.GetEquippedChip(slotIndex);
            bool hasChip = !string.IsNullOrEmpty(equippedChipId);

            if (!hasChip)
            {
                view.BindEmpty(slotIndex == selectedSlotIndex);
                continue;
            }

            var def = chipService.GetDefinition(equippedChipId);
            var progress = chipService.GetProgress(equippedChipId);
            view.BindChip(def, progress, slotIndex == selectedSlotIndex, chipButtonPrefab);
        }
    }
    
    private void RefreshChipList()
    {
        if (chipService == null || chipListContainer == null) return;
        
        // Clear existing buttons
        foreach (var view in chipItemViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        chipItemViews.Clear();
        
        // Get all chip definitions
        var allChips = chipService.GetAllDefinitions();
        
        foreach (var def in allChips)
        {
            if (chipButtonPrefab == null) continue;
            
            var progress = chipService.GetProgress(def.id);
            
            var instance = Instantiate(chipButtonPrefab, chipListContainer);
            var view = instance.GetComponent<ChipListItemView>() ?? instance.GetComponentInChildren<ChipListItemView>();
            if (view == null)
            {
                Debug.LogError("[ChipSelectorUI] Chip button prefab is missing a ChipListItemView component. Destroying the instance to avoid an infinite loop.");
                Destroy(instance);
                continue;
            }

            view.Bind(def, progress, chipService.IsChipEquipped(def.id));

            string chipId = def.id;

            var controller = instance.GetComponent<ChipListItemController>();
            if (controller == null)
                controller = instance.AddComponent<ChipListItemController>();
            controller.Initialize(this, chipId);

            chipItemViews.Add(view);
        }
    }
    
    private void RefreshPurchaseButton()
    {
        if (chipService == null || purchaseButton == null) return;
        
        int cost = chipService.GetChipPurchaseCost();
        bool canPurchase = chipService.CanPurchaseChip();
        
        purchaseButton.interactable = canPurchase;
        
        if (purchaseCostText != null)
            purchaseCostText.text = $"{cost} Prisms";
    }
    
    private void RefreshSlotUnlockButton()
    {
        if (chipService == null || unlockSlotButton == null) return;
        
        int current = chipService.GetUnlockedSlotCount();
        int max = chipService.GetMaxSlotCount();
        
        if (current >= max)
        {
            unlockSlotButton.interactable = false;
            if (unlockSlotCostText != null)
                unlockSlotCostText.text = "Max Slots";
        }
        else
        {
            int cost = chipService.GetNextSlotCost();
            bool canUnlock = chipService.CanUnlockSlot();
            
            unlockSlotButton.interactable = canUnlock;
            if (unlockSlotCostText != null)
                unlockSlotCostText.text = $"{cost} Prisms";
        }
    }
    
    private void RefreshDetailsPanel()
    {
        if (chipDetailsPanel == null) return;
        
        bool hasSelection = !string.IsNullOrEmpty(selectedChipId);
        chipDetailsPanel.SetActive(hasSelection);
        
        if (!hasSelection) return;
        
        var def = chipService.GetDefinition(selectedChipId);
        if (def == null) return;
        
        var progress = chipService.GetProgress(selectedChipId);
        bool isUnlocked = progress != null && progress.unlocked;

        if (!isUnlocked)
        {
            selectedChipId = null;
            chipDetailsPanel.SetActive(false);
            return;
        }
        
        if (chipNameText != null)
            chipNameText.text = def.chipName;
        
        if (chipDescriptionText != null)
            chipDescriptionText.text = def.description;
        
        if (chipIconImage != null)
        {
            chipIconImage.sprite = def.icon;
            chipIconImage.enabled = def.icon != null;
        }

        var rarity = def.GetRarityEnum(progress.rarityLevel);
        if (chipRarityText != null)
        {
            chipRarityText.text = $"Rarity: {rarity}";
            chipRarityText.color = GetRarityColor(rarity);
        }
        
        if (chipBonusText != null)
            chipBonusText.text = $"Bonus: {def.GetFormattedBonus(progress.rarityLevel)} {def.bonusType}";
        
        if (chipProgressText != null)
        {
            int current = progress.chipCount;
            int nextRarity = progress.rarityLevel + 1;
            if (nextRarity <= def.GetMaxRarity())
            {
                int needed = def.GetChipsNeededForRarity(nextRarity);
                chipProgressText.text = $"Progress: {current}/{needed} (Next: {def.GetRarityEnum(nextRarity)})";
            }
            else
            {
                chipProgressText.text = "Progress: MAX RARITY";
            }
        }
        
        // Equip/Unequip buttons
        bool isEquipped = chipService.IsChipEquipped(selectedChipId);
        bool canEquip = selectedSlotIndex >= 0 && isUnlocked && chipService.CanEquipChip(selectedChipId, selectedSlotIndex);
        
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isEquipped && isUnlocked);
            equipButton.interactable = canEquip;
        }
        
        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEquipped);
            unequipButton.interactable = true;
        }
    }
    
    private void OnSlotClicked(int slotIndex)
    {
        if (chipService == null) return;

        string equippedChipId = chipService.GetEquippedChip(slotIndex);
        if (!string.IsNullOrEmpty(equippedChipId))
        {
            if (chipService.TryUnequipChip(slotIndex))
            {
                if (selectedChipId == equippedChipId)
                    selectedChipId = null;

                if (selectedSlotIndex == slotIndex)
                    selectedSlotIndex = -1;

                RefreshUI();
            }
            return;
        }

        selectedSlotIndex = slotIndex;
        RefreshChipSlots();
        RefreshDetailsPanel();
    }

    public void HandleChipTap(string chipId)
    {
        if (chipService == null || string.IsNullOrEmpty(chipId)) return;

        if (!chipService.IsChipUnlocked(chipId))
        {
            HandleChipHold(chipId);
            return;
        }

        int currentlyEquippedSlot = chipService.GetSlotIndexForChip(chipId);
        if (currentlyEquippedSlot >= 0)
        {
            if (chipService.TryUnequipChip(currentlyEquippedSlot))
            {
                if (selectedSlotIndex == currentlyEquippedSlot)
                    selectedSlotIndex = -1;

                if (selectedChipId == chipId)
                    selectedChipId = null;

                RefreshUI();
            }
            else
            {
                Debug.LogWarning($"[ChipSelectorUI] Failed to unequip chip '{chipId}' from slot {currentlyEquippedSlot}.");
            }
            return;
        }

        int targetSlot = FindFirstEmptySlot();
        if (targetSlot < 0)
        {
            selectedChipId = chipId;
            selectedSlotIndex = chipService.GetSlotIndexForChip(chipId);
            RefreshChipSlots();
            RefreshDetailsPanel();
            Debug.LogWarning("[ChipSelectorUI] No available chip slots. Please free a slot before equipping a new chip.");
            return;
        }

        if (chipService.TryEquipChip(chipId, targetSlot))
        {
            selectedChipId = null;
            selectedSlotIndex = -1;
            RefreshUI();
        }
        else
        {
            Debug.LogWarning($"[ChipSelectorUI] Failed to auto-equip chip '{chipId}' to slot {targetSlot}.");
        }
    }

    public void HandleChipHold(string chipId)
    {
        if (chipService == null || string.IsNullOrEmpty(chipId)) return;

        if (!chipService.IsChipUnlocked(chipId))
        {
            Debug.LogWarning("[ChipSelectorUI] Chip is locked. Unlock it before viewing details.");
            CloseDetailsPanel();
            return;
        }

        selectedChipId = chipId;

        int equippedSlot = chipService.GetSlotIndexForChip(chipId);
        if (equippedSlot >= 0)
            selectedSlotIndex = equippedSlot;
        else
        {
            int firstEmpty = FindFirstEmptySlot();
            selectedSlotIndex = firstEmpty >= 0 ? firstEmpty : -1;
        }

        RefreshChipSlots();
        RefreshDetailsPanel();
    }
    
    private void OnPurchaseChipClicked()
    {
        if (chipService.TryPurchaseRandomChip(out string chipId))
        {
            Debug.Log($"[ChipSelectorUI] Purchased chip: {chipId}");
            RefreshUI();
            
            // Select the purchased chip
            selectedChipId = chipId;
            RefreshDetailsPanel();
        }
        else
        {
            Debug.LogWarning("[ChipSelectorUI] Failed to purchase chip");
        }
    }
    
    private void OnUnlockSlotClicked()
    {
        if (chipService.TryUnlockSlot())
        {
            Debug.Log("[ChipSelectorUI] Unlocked new chip slot");
            selectedSlotIndex = Mathf.Max(0, chipService.GetUnlockedSlotCount() - 1);
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("[ChipSelectorUI] Failed to unlock slot");
        }
    }
    
    private void OnEquipClicked()
    {
        if (selectedSlotIndex < 0 || string.IsNullOrEmpty(selectedChipId))
            return;
        
        if (chipService.TryEquipChip(selectedChipId, selectedSlotIndex))
        {
            Debug.Log($"[ChipSelectorUI] Equipped {selectedChipId} to slot {selectedSlotIndex}");
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("[ChipSelectorUI] Failed to equip chip");
        }
    }
    
    private void OnUnequipClicked()
    {
        if (string.IsNullOrEmpty(selectedChipId)) return;
        
        // Find which slot has this chip
        for (int i = 0; i < chipService.GetUnlockedSlotCount(); i++)
        {
            if (chipService.GetEquippedChip(i) == selectedChipId)
            {
                if (chipService.TryUnequipChip(i))
                {
                    Debug.Log($"[ChipSelectorUI] Unequipped {selectedChipId} from slot {i}");
                    RefreshUI();
                }
                break;
            }
        }
    }
    
    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
        
        // Refresh loadout screen if available
        if (loadoutScreen != null)
            loadoutScreen.UpdateSlotButtons();
    }

    private Color GetRarityColor(ChipRarity rarity)
    {
        return rarity switch
        {
            ChipRarity.Common => Color.white,
            ChipRarity.Uncommon => Color.green,
            ChipRarity.Rare => Color.blue,
            ChipRarity.Epic => new Color(0.6f, 0f, 1f), // Purple
            ChipRarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
            _ => Color.white
        };
    }

    private int FindFirstEmptySlot()
    {
        if (chipService == null) return -1;

        int unlocked = chipService.GetUnlockedSlotCount();
        for (int i = 0; i < unlocked; i++)
        {
            if (string.IsNullOrEmpty(chipService.GetEquippedChip(i)))
                return i;
        }

        return -1;
    }
    
    public void CloseDetailsPanel()
    {
        selectedChipId = null;
        if (chipDetailsPanel != null)
            chipDetailsPanel.SetActive(false);
    }
}
