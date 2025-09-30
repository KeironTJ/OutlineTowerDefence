using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2 lastScreenSize = Vector2.zero;

    void Start()
    {
        ApplySafeArea();
    }

    void Update()
    {
        #if UNITY_EDITOR
                // Always update in editor for simulator support
                ApplySafeArea();
        #else
                if (Screen.safeArea != lastSafeArea || 
                    Screen.width != lastScreenSize.x || 
                    Screen.height != lastScreenSize.y)
                {
                    ApplySafeArea();
                }
        #endif
    }

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        RectTransform t = GetComponent<RectTransform>();
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        t.anchorMin = anchorMin;
        t.anchorMax = anchorMax;
    }
}