using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundStatsView : MonoBehaviour
{
    [Header("Stats Table")]
    [SerializeField] private Transform statsTableContainer;
    [SerializeField] private GameObject statItemPrefab;

    [Header("Performance")]
    [SerializeField] private float minUpdateInterval = 0.2f; // seconds (5 Hz)
    private float _lastUpdateTime;

    private bool liveMode = false;
    private RoundManager boundRoundManager;
    private bool subscribed;

    private void Update()
    {
        if (!liveMode || boundRoundManager == null) return;
        if (Time.unscaledTime - _lastUpdateTime < minUpdateInterval) return;
        _lastUpdateTime = Time.unscaledTime;
        Populate(boundRoundManager.GetLiveRoundRecord());
    }

    // Call this for the in-round HUD
    public void BindLive(RoundManager roundManager)
    {
        boundRoundManager = roundManager;
        liveMode = true;
        Subscribe();
        Populate(boundRoundManager.GetLiveRoundRecord());
    }

    // Call this for history/details panels
    public void ShowRecord(RoundRecord record)
    {
        Unsubscribe();
        liveMode = false;
        boundRoundManager = null;
        Populate(record);
    }

    private void OnEnable()
    {
        if (liveMode) {
            Subscribe();
            if (boundRoundManager != null)
                Populate(boundRoundManager.GetLiveRoundRecord());
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        EventManager.StartListening(EventNames.RoundStatsUpdated, OnRoundStatsUpdated);
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        EventManager.StopListening(EventNames.RoundStatsUpdated, OnRoundStatsUpdated);
        subscribed = false;
    }

    private void OnRoundStatsUpdated(object _)
    {
        if (!liveMode || boundRoundManager == null) return;
        // throttle UI updates
        if (Time.unscaledTime - _lastUpdateTime < minUpdateInterval) return;
        _lastUpdateTime = Time.unscaledTime;

        Populate(boundRoundManager.GetLiveRoundRecord());
    }

    private void Populate(RoundRecord record)
    {
        if (record == null) return;

        // Clear previous rows
        foreach (Transform child in statsTableContainer)
            Destroy(child.gameObject);

        // Header
        AddHeaderRow("Round Summary");

        // Main stats
        AddStatRow("Duration", FormatDuration(record.durationSeconds));
        AddStatRow("Difficulty", record.difficulty.ToString());
        AddStatRow("Highest Wave", record.highestWave.ToString());
        AddStatRow("Bullets Fired", NumberManager.FormatLargeNumber(record.bulletsFired, true));

        // Currency section
        AddHeaderRow("Currency Earned");
        foreach (var c in record.currencyEarned.OrderBy(c => c.type))
            AddStatRow(c.type.ToString(), NumberManager.FormatLargeNumber(c.amount));

        // Enemy breakdown section
        AddHeaderRow("Enemies Destroyed",NumberManager.FormatLargeNumber(record.enemiesKilled, true));
        foreach (var k in record.enemyKills)
        {
            // Example line: "{definitionId} x{count}"
            AddStatRow($"{k.definitionId}", NumberManager.FormatLargeNumber(k.count, true));
        }
    }

    private static string FormatDuration(float seconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }


    private void AddStatRow(string label, string value)
    {
        GameObject statItem = Instantiate(statItemPrefab, statsTableContainer);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        labelText.text = label;
        valueText.text = value;
    }

    private void AddHeaderRow(string label, string value = "", bool isSubHeader = false)
    {
        GameObject statItem = Instantiate(statItemPrefab, statsTableContainer);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        labelText.text = label;
        valueText.text = value;

        if (!isSubHeader)
        {
            labelText.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Underline;
            labelText.fontSize *= 1.2f;
            valueText.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Underline;
            valueText.fontSize *= 1.2f;
        }
        else
        {
            labelText.fontStyle = TMPro.FontStyles.Bold;
            labelText.fontSize *= 1.1f;
            valueText.fontStyle = TMPro.FontStyles.Bold;
            valueText.fontSize *= 1.1f;
        }
    }

    private void AddLine(string line)
    {
        GameObject statItem = Instantiate(statItemPrefab, statsTableContainer);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        labelText.text = line;
        // Optionally, you can disable the value text or set it to empty
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        valueText.gameObject.SetActive(false);
    }
}
