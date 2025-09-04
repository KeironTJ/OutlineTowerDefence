using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundStatsView : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI highestWaveText;
    [SerializeField] private TextMeshProUGUI bulletsFiredText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;

    [Header("Currency List")]
    [SerializeField] private Transform currencyContainer;
    [SerializeField] private GameObject rowTextPrefab; // prefab with a TextMeshProUGUI component

    [Header("Enemy Breakdown List")]
    [SerializeField] private Transform enemyBreakdownContainer;

    [Header("Stats Table")]
    [SerializeField] private Transform statsTableContainer;
    [SerializeField] private GameObject statItemPrefab;

    [Header("Performance")]
    [SerializeField] private float minUpdateInterval = 0.2f; // seconds (5 Hz)
    private float _lastUpdateTime;

    private bool liveMode = false;
    private RoundManager boundRoundManager;
    private bool subscribed;

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
        foreach (var type in record.enemyBreakdown)
        {
            if (type.total <= 0) continue;
            AddHeaderRow(type.type.ToString(), NumberManager.FormatLargeNumber(type.total, true), true);
            foreach (var sub in type.subtypes)
            {
                if (sub.count <= 0) continue;
                AddStatRow(sub.subtype.ToString(), NumberManager.FormatLargeNumber(sub.count, true));
            }
        }
    }

    // Update existing children when counts match; rebuild only if needed
    private void PopulateList(Transform container, IList<string> lines)
    {
      if (container == null || rowTextPrefab == null) return;

      // If same count: update text in-place (no layout churn)
      if (container.childCount == lines.Count)
      {
          for (int i = 0; i < lines.Count; i++)
          {
              var child = container.GetChild(i);
              var txt = child.GetComponent<TextMeshProUGUI>() ?? child.GetComponentInChildren<TextMeshProUGUI>(true);
              if (txt != null) txt.text = lines[i];
          }
          return;
      }

      // Otherwise rebuild (count changed)
      for (int i = container.childCount - 1; i >= 0; i--)
          Destroy(container.GetChild(i).gameObject);

      for (int i = 0; i < lines.Count; i++)
      {
          var go = Instantiate(rowTextPrefab, container, false);
          var txt = go.GetComponent<TextMeshProUGUI>() ?? go.GetComponentInChildren<TextMeshProUGUI>(true);
          if (txt != null) txt.text = lines[i];
      }
    }

    private static string FormatDuration(float seconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private void Awake()
    {
        ValidateBindings();
    }

    [ContextMenu("Validate bindings")]
    private void ValidateBindings()
    {
        if (durationText == null) Debug.LogWarning("RoundStatsView: durationText not assigned.", this);
        if (difficultyText == null) Debug.LogWarning("RoundStatsView: difficultyText not assigned.", this);
        if (highestWaveText == null) Debug.LogWarning("RoundStatsView: highestWaveText not assigned.", this);
        if (bulletsFiredText == null) Debug.LogWarning("RoundStatsView: bulletsFiredText not assigned.", this);
        if (enemiesKilledText == null) Debug.LogWarning("RoundStatsView: enemiesKilledText not assigned.", this);

        if (currencyContainer == null) Debug.LogWarning("RoundStatsView: currencyContainer not assigned (drag from Hierarchy).", this);
        if (enemyBreakdownContainer == null) Debug.LogWarning("RoundStatsView: enemyBreakdownContainer not assigned (drag from Hierarchy).", this);
        if (statsTableContainer == null) Debug.LogWarning("RoundStatsView: statsTableContainer not assigned (drag from Hierarchy).", this);

        if (rowTextPrefab == null)
        {
            Debug.LogWarning("RoundStatsView: rowTextPrefab not assigned.", this);
        }
        else if (rowTextPrefab.GetComponentInChildren<TextMeshProUGUI>(true) == null)
        {
            Debug.LogWarning("RoundStatsView: rowTextPrefab has no TextMeshProUGUI component (add one on the root or a child).", rowTextPrefab);
        }
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
}
