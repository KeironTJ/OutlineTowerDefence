using UnityEngine;
using UnityEngine.EventSystems;

public class ChipListItemController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float holdThreshold = 0.35f;

    private ChipSelectorUI selector;
    private string chipId;
    private bool pointerDown;
    private float pointerDownTime;
    private bool holdTriggered;

    public void Initialize(ChipSelectorUI chipSelectorUI, string id)
    {
        selector = chipSelectorUI;
        chipId = id;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selector == null || string.IsNullOrEmpty(chipId))
            return;

        pointerDown = true;
        holdTriggered = false;
        pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selector == null || string.IsNullOrEmpty(chipId))
            return;

        if (!pointerDown)
            return;

        pointerDown = false;

        if (!holdTriggered)
            selector?.HandleChipTap(chipId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerDown = false;
    }

    private void Update()
    {
        if (selector == null || string.IsNullOrEmpty(chipId))
            return;

        if (!pointerDown || holdTriggered)
            return;

        if (Time.unscaledTime - pointerDownTime >= holdThreshold)
        {
            holdTriggered = true;
            selector?.HandleChipHold(chipId);
        }
    }

    private void OnDisable()
    {
        pointerDown = false;
        holdTriggered = false;
    }
}
