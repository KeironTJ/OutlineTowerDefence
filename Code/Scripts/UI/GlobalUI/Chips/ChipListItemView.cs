using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChipListItemView : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI chipName;
    public Image icon;
    public TextMeshProUGUI rarityText;
    public GameObject lockedPanel;
    public GameObject equippedIndicator;
    public TextMeshProUGUI chipCountText;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    public void Bind(ChipDefinition definition, ChipProgressData progress, bool isEquipped, bool showEquippedIndicator = true, bool forceLocked = false)
    {
        EnsureButton();

        if (definition == null)
        {
            if (chipName != null)
                chipName.text = "Unknown";

            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (rarityText != null)
            {
                rarityText.text = "Unknown";
                rarityText.color = Color.white;
            }

            if (chipCountText != null)
                chipCountText.text = "";

            if (lockedPanel != null)
                lockedPanel.SetActive(true);

            if (equippedIndicator != null)
                equippedIndicator.SetActive(false);

            return;
        }

        bool unlocked = !forceLocked && progress != null && progress.unlocked;

        if (chipName != null)
            chipName.text = unlocked ? definition.chipName : "???";

        if (icon != null)
        {
            if (unlocked)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        if (rarityText != null)
        {
            if (unlocked)
            {
                var rarity = definition.GetRarityEnum(progress != null ? progress.rarityLevel : 0);
                rarityText.text = $"Level {((int)rarity + 1)}";
                rarityText.color = GetRarityColor(rarity);
            }
            else
            {
                rarityText.text = "Unknown";
                rarityText.color = Color.white;
            }
        }

        if (lockedPanel != null)
            lockedPanel.SetActive(!unlocked);

        if (chipCountText != null)
            chipCountText.text = BuildChipCountText(definition, progress);

        if (equippedIndicator != null)
            equippedIndicator.SetActive(showEquippedIndicator && isEquipped);
    }

    public void SetInteractable(bool interactable)
    {
        EnsureButton();
        if (button != null)
            button.interactable = interactable;
    }

    private void EnsureButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private static Color GetRarityColor(ChipRarity rarity)
    {
        return rarity switch
        {
            ChipRarity.Level1 => Color.white,
            ChipRarity.Level2 => Color.green,
            ChipRarity.Level3 => Color.blue,
            ChipRarity.Level4 => new Color(0.6f, 0f, 1f),
            ChipRarity.Level5 => new Color(1f, 0.5f, 0f),
            _ => Color.white
        };
    }

    private static string BuildChipCountText(ChipDefinition definition, ChipProgressData progress)
    {
        if (definition == null)
            return string.Empty;

        int maxRarity = definition.GetMaxRarity();
        int currentRarity = Mathf.Clamp(progress?.rarityLevel ?? 0, 0, maxRarity);

        if (currentRarity >= maxRarity)
            return "MAX";

        int nextRarity = Mathf.Clamp(currentRarity + 1, 0, maxRarity);
        int requiredForNext = definition.GetChipsNeededForRarity(nextRarity);
        int requiredForCurrent = definition.GetChipsNeededForRarity(currentRarity);

        if (requiredForNext <= 0 || requiredForNext == int.MaxValue)
            return (progress?.chipCount ?? 0).ToString();

        int tierRequirement = Mathf.Max(1, requiredForNext - requiredForCurrent);
        int currentCount = Mathf.Max(0, (progress?.chipCount ?? 0) - requiredForCurrent);
        int clampedCount = Mathf.Clamp(currentCount, 0, tierRequirement);

        return $"{clampedCount}/{tierRequirement}";
    }
}