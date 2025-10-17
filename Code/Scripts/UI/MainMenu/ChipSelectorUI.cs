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
    
    private readonly List<Button> chipButtons = new List<Button>();
    private readonly List<Button> slotButtons = new List<Button>();
    
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
        RefreshChipSlots();
        RefreshChipList();
        RefreshPurchaseButton();
        RefreshSlotUnlockButton();
        RefreshDetailsPanel();
    }
    
    private void RefreshChipSlots()
    {
        if (chipService == null || chipSlotsContainer == null) return;
        
        int unlockedCount = chipService.GetUnlockedSlotCount();
        int maxCount = chipService.GetMaxSlotCount();
        
        // Update slots info text
        if (slotsInfoText != null)
            slotsInfoText.text = $"Chip Slots: {unlockedCount}/{maxCount}";
        
        // Ensure we have enough slot buttons
        while (slotButtons.Count < maxCount && chipSlotPrefab != null)
        {
            var instance = Instantiate(chipSlotPrefab, chipSlotsContainer);
            var button = instance.GetComponent<Button>();
            if (button != null)
            {
                slotButtons.Add(button);
                int index = slotButtons.Count - 1;
                button.onClick.AddListener(() => OnSlotClicked(index));
            }
        }
        
        // Update slot visuals
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int slotIndex = i;
            bool isUnlocked = i < unlockedCount;
            
            var button = slotButtons[i];
            button.interactable = isUnlocked;
            
            // Find UI elements in the slot
            var nameText = button.transform.Find("ChipName")?.GetComponent<TextMeshProUGUI>();
            var iconImage = button.transform.Find("Icon")?.GetComponent<Image>();
            var lockedPanel = button.transform.Find("LockedPanel")?.gameObject;
            var emptyPanel = button.transform.Find("EmptyPanel")?.gameObject;
            
            if (!isUnlocked)
            {
                if (nameText) nameText.text = "Locked";
                if (iconImage) iconImage.enabled = false;
                if (lockedPanel) lockedPanel.SetActive(true);
                if (emptyPanel) emptyPanel.SetActive(false);
            }
            else
            {
                string equippedChipId = chipService.GetEquippedChip(slotIndex);
                bool hasChip = !string.IsNullOrEmpty(equippedChipId);
                
                if (hasChip)
                {
                    var def = chipService.GetDefinition(equippedChipId);
                    if (def != null)
                    {
                        if (nameText) nameText.text = def.chipName;
                        if (iconImage)
                        {
                            iconImage.sprite = def.icon;
                            iconImage.enabled = def.icon != null;
                        }
                    }
                    if (lockedPanel) lockedPanel.SetActive(false);
                    if (emptyPanel) emptyPanel.SetActive(false);
                }
                else
                {
                    if (nameText) nameText.text = "Empty";
                    if (iconImage) iconImage.enabled = false;
                    if (lockedPanel) lockedPanel.SetActive(false);
                    if (emptyPanel) emptyPanel.SetActive(true);
                }
            }
            
            // Highlight selected slot
            var outline = button.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = (slotIndex == selectedSlotIndex);
        }
    }
    
    private void RefreshChipList()
    {
        if (chipService == null || chipListContainer == null) return;
        
        // Clear existing buttons
        foreach (var button in chipButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        chipButtons.Clear();
        
        // Get all chip definitions
        var allChips = chipService.GetAllDefinitions();
        
        foreach (var def in allChips)
        {
            if (chipButtonPrefab == null) continue;
            
            var progress = chipService.GetProgress(def.id);
            
            var instance = Instantiate(chipButtonPrefab, chipListContainer);
            var button = instance.GetComponent<Button>();
            if (button == null) continue;
            
            // Setup UI elements
            var nameText = instance.transform.Find("ChipName")?.GetComponent<TextMeshProUGUI>();
            var iconImage = instance.transform.Find("Icon")?.GetComponent<Image>();
            var rarityText = instance.transform.Find("Rarity")?.GetComponent<TextMeshProUGUI>();
            var lockedPanel = instance.transform.Find("LockedPanel")?.gameObject;
            var equippedIndicator = instance.transform.Find("EquippedIndicator")?.gameObject;
            
            bool isUnlocked = progress != null && progress.unlocked;
            
            if (isUnlocked)
            {
                if (nameText) nameText.text = def.chipName;
                if (iconImage)
                {
                    iconImage.sprite = def.icon;
                    iconImage.enabled = def.icon != null;
                }
                if (rarityText)
                {
                    var rarity = def.GetRarityEnum(progress.rarityLevel);
                    rarityText.text = rarity.ToString();
                    rarityText.color = GetRarityColor(rarity);
                }
                if (lockedPanel) lockedPanel.SetActive(false);
                if (equippedIndicator) equippedIndicator.SetActive(chipService.IsChipEquipped(def.id));
            }
            else
            {
                if (nameText) nameText.text = "???";
                if (iconImage) iconImage.enabled = false;
                if (rarityText) rarityText.text = "Unknown";
                if (lockedPanel) lockedPanel.SetActive(true);
                if (equippedIndicator) equippedIndicator.SetActive(false);
            }
            
            string chipId = def.id;
            button.onClick.AddListener(() => OnChipClicked(chipId));
            
            chipButtons.Add(button);
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
        
        if (chipNameText != null)
            chipNameText.text = isUnlocked ? def.chipName : "???";
        
        if (chipDescriptionText != null)
            chipDescriptionText.text = isUnlocked ? def.description : "Unlock this chip to see its details.";
        
        if (chipIconImage != null)
        {
            chipIconImage.sprite = isUnlocked ? def.icon : null;
            chipIconImage.enabled = isUnlocked && def.icon != null;
        }
        
        if (isUnlocked)
        {
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
        selectedSlotIndex = slotIndex;
        RefreshChipSlots();
        RefreshDetailsPanel();
    }
    
    private void OnChipClicked(string chipId)
    {
        selectedChipId = chipId;
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
}
