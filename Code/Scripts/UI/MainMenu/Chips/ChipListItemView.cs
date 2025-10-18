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

    private void Reset()
    {
        button = GetComponent<Button>();
    }
}