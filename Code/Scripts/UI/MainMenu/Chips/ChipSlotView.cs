using UnityEngine;
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

    private void Reset()
    {
        button = GetComponent<Button>();
    }
}