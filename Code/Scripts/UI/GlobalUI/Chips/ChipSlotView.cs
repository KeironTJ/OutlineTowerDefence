using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ChipSlotView : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI chipName;
    public Image icon;
    public GameObject lockedPanel;
    public GameObject emptyPanel;
    public Outline selectionOutline;
    public RectTransform chipContentRoot;
    public bool disableChipCardRaycasts = true;

    private ChipListItemView chipCardView;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    public void ConfigureButton(UnityAction onClick)
    {
        EnsureButton();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        button.interactable = onClick != null;
    }

    public void BindLocked()
    {
        SetSelection(false);
        ToggleActive(chipName, false);
        ToggleActive(icon, false);
        if (lockedPanel != null) lockedPanel.SetActive(true);
        if (emptyPanel != null) emptyPanel.SetActive(false);
        HideChipCard();
    }

    public void BindEmpty(bool isSelected)
    {
        SetSelection(isSelected);
        ToggleActive(chipName, true);
        if (chipName != null)
            chipName.text = "Empty";

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (lockedPanel != null) lockedPanel.SetActive(false);
        if (emptyPanel != null) emptyPanel.SetActive(true);
        HideChipCard();
    }

    public void BindChip(ChipDefinition definition, ChipProgressData progress, bool isSelected, GameObject chipCardPrefab)
    {
        SetSelection(isSelected);
        if (lockedPanel != null) lockedPanel.SetActive(false);
        if (emptyPanel != null) emptyPanel.SetActive(false);
        ToggleActive(chipName, false);
        ToggleActive(icon, false);

        if (definition == null)
        {
            BindEmpty(isSelected);
            return;
        }

        if (chipCardPrefab != null && chipContentRoot != null)
        {
            EnsureChipCard(chipCardPrefab);
            if (chipCardView != null)
            {
                chipCardView.gameObject.SetActive(true);
                chipCardView.Bind(definition, progress, isEquipped: true, showEquippedIndicator: false);
            }
        }
        else
        {
            ToggleActive(chipName, true);
            ToggleActive(icon, true);
            if (chipName != null)
                chipName.text = definition.chipName;
            if (icon != null)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }
        }
    }

    public void SetSelection(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    private void EnsureButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void EnsureChipCard(GameObject prefab)
    {
        if (chipCardView != null)
            return;

        var instance = Instantiate(prefab, chipContentRoot);
        chipCardView = instance.GetComponent<ChipListItemView>() ?? instance.GetComponentInChildren<ChipListItemView>();
        if (chipCardView == null)
        {
            Debug.LogError("[ChipSlotView] Chip card prefab is missing ChipListItemView. Destroying instance.");
            Destroy(instance);
            return;
        }

        var controller = chipCardView.GetComponent<ChipListItemController>();
        if (controller != null)
            Destroy(controller);

        if (disableChipCardRaycasts)
        {
            var graphics = chipCardView.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
                graphic.raycastTarget = false;
        }
    }

    private void HideChipCard()
    {
        if (chipCardView != null)
            chipCardView.gameObject.SetActive(false);
    }

    private static void ToggleActive(Behaviour behaviour, bool enabled)
    {
        if (behaviour == null) return;

        switch (behaviour)
        {
            case TextMeshProUGUI tmp:
                tmp.gameObject.SetActive(enabled);
                break;
            case Image image:
                image.gameObject.SetActive(enabled);
                break;
            default:
                behaviour.gameObject.SetActive(enabled);
                break;
        }
    }
}