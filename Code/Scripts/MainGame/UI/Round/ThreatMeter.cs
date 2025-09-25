using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreatMeter : MonoBehaviour
{
    [SerializeField] private Slider threatSlider;
    [SerializeField] private TextMeshProUGUI threatText;
    [SerializeField] private float decayRate = 3f; // how quickly the slider decays when threat drops
    [SerializeField] private Color safeColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.25f);
    [SerializeField] private Color dangerColor = new Color(0.85f, 0.25f, 0.25f);

    private float displayedValue;

    private Image sliderFill;
    private float vel; // add near displayedValue

    private void Awake()
    {
        if (threatSlider != null)
            sliderFill = threatSlider.fillRect ? threatSlider.fillRect.GetComponent<Image>() : null;
    }

    private void Update()
    {
        if (WaveManager.Instance == null)
        {
            ApplyValue(0f, 0f, 0f);
            return;
        }

        WaveManager.Instance.GetThreatSnapshot(out float active, out float scheduled, out float total);
        float target = total > 0f ? Mathf.Clamp01((active + scheduled) / total) : 0f;

        displayedValue = Mathf.SmoothDamp(displayedValue, target, ref vel, 0.15f, Mathf.Infinity, Time.deltaTime);

        ApplyValue(displayedValue, active, scheduled);
    }

    private void ApplyValue(float normalized, float active, float scheduled)
    {
        if (threatSlider != null)
            threatSlider.value = normalized;

        if (sliderFill != null)
        {
            sliderFill.color = normalized > 0.66f ? dangerColor
                                : normalized > 0.33f ? warningColor
                                : safeColor;
        }

        if (threatText != null)
        {
            float percent = normalized * 100f;
            threatText.text = $"Threat {percent:0}%";
        }
    }
}