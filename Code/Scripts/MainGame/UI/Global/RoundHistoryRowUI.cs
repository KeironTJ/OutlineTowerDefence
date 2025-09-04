using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RoundHistoryRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI waveText;

    public void Bind(RoundRecord rec, Action onClick = null)
    {
        if (rec == null) return;

        var ended = TryParseUtc(rec.endedAtIsoUtc, out var dtUtc) ? dtUtc.ToLocalTime() : (DateTime?)null;
        dateText?.SetText(ended.HasValue ? ended.Value.ToString("dd-MM-yyy") : "—");
        timeText?.SetText(ended.HasValue ? ended.Value.ToString("HH:mm") : "—");

        difficultyText?.SetText($"D{rec.difficulty}");
        waveText?.SetText($"W{rec.highestWave}");

        var btn = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
        }
    }

    private static bool TryParseUtc(string iso, out DateTime dt)
        => DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out dt);

    private void Reset()
    {
        // Auto-wire if children are named exactly: Date, Time, Difficulty, Wave
        dateText       = transform.Find("Date")?.GetComponent<TextMeshProUGUI>();
        timeText       = transform.Find("Time")?.GetComponent<TextMeshProUGUI>();
        difficultyText = transform.Find("Difficulty")?.GetComponent<TextMeshProUGUI>();
        waveText       = transform.Find("Wave")?.GetComponent<TextMeshProUGUI>();
    }
}