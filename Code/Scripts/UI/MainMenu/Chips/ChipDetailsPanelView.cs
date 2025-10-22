using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChipDetailsPanelView : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI chipNameText;
    [SerializeField] private TextMeshProUGUI chipDescriptionText;
    [SerializeField] private Image chipIconImage;
    [SerializeField] private TextMeshProUGUI chipRarityText;
    [SerializeField] private TextMeshProUGUI chipBonusText;
    [SerializeField] private TextMeshProUGUI chipProgressText;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;

    public void Show(ChipDefinition definition, ChipProgressData progress, bool isEquipped, bool canEquip, UnityAction onEquip, UnityAction onUnequip)
    {
        if (definition == null || progress == null)
        {
            Hide();
            return;
        }

        SetRootActive(true);

        if (chipNameText != null)
            chipNameText.text = definition.chipName;

        if (chipDescriptionText != null)
            chipDescriptionText.text = definition.description;

        if (chipIconImage != null)
        {
            chipIconImage.sprite = definition.icon;
            chipIconImage.enabled = definition.icon != null;
        }

        var rarityEnum = definition.GetRarityEnum(progress.rarityLevel);
        if (chipRarityText != null)
        {
            chipRarityText.text = $"Rarity: {rarityEnum}";
            chipRarityText.color = GetRarityColor(rarityEnum);
        }

        if (chipBonusText != null)
            chipBonusText.text = $"Bonus: {definition.GetFormattedBonus(progress.rarityLevel)} {definition.bonusType}";

        if (chipProgressText != null)
        {
            int nextRarity = progress.rarityLevel + 1;
            if (nextRarity <= definition.GetMaxRarity())
            {
                int needed = definition.GetChipsNeededForRarity(nextRarity);
                chipProgressText.text = $"Progress: {progress.chipCount}/{needed} (Next: {definition.GetRarityEnum(nextRarity)})";
            }
            else
            {
                chipProgressText.text = "Progress: MAX RARITY";
            }
        }

        ConfigureButton(equipButton, !isEquipped, canEquip, onEquip);
        ConfigureButton(unequipButton, isEquipped, true, onUnequip);
    }

    public void Hide()
    {
        ConfigureButton(equipButton, false, false, null);
        ConfigureButton(unequipButton, false, false, null);
        SetRootActive(false);
    }

    private void ConfigureButton(Button button, bool active, bool interactable, UnityAction handler)
    {
        if (button == null) return;

        button.gameObject.SetActive(active);
        button.onClick.RemoveAllListeners();

        if (active && handler != null)
            button.onClick.AddListener(handler);

        button.interactable = active && interactable;
    }

    private void SetRootActive(bool value)
    {
        var target = panelRoot != null ? panelRoot : gameObject;
        if (target.activeSelf != value)
            target.SetActive(value);
    }

    private static Color GetRarityColor(ChipRarity rarity)
    {
        return rarity switch
        {
            ChipRarity.Common => Color.white,
            ChipRarity.Uncommon => Color.green,
            ChipRarity.Rare => Color.blue,
            ChipRarity.Epic => new Color(0.6f, 0f, 1f),
            ChipRarity.Legendary => new Color(1f, 0.5f, 0f),
            _ => Color.white
        };
    }
}