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

        durationText.text = FormatDuration(record.durationSeconds);
        difficultyText.text = $"Difficulty: {record.difficulty}";
        highestWaveText.text = $"Highest Wave: {record.highestWave}";
        bulletsFiredText.text = $"Bullets: {NumberManager.FormatLargeNumber(record.bulletsFired, true)}";
        enemiesKilledText.text = $"Kills: {NumberManager.FormatLargeNumber(record.enemiesKilled, true)}";

        // Build line lists once, then update rows in-place (no destroy/recreate)
        var currencyLines = record.currencyEarned
            .OrderBy(c => c.type) // stable order
            .Select(c => $"{c.type}: {NumberManager.FormatLargeNumber(c.amount)}")
            .ToList();

        var enemyLines = new List<string>();
        foreach (var type in record.enemyBreakdown)
        {
            if (type.total <= 0) continue;
            enemyLines.Add($"Enemy Type: {type.type} ({NumberManager.FormatLargeNumber(type.total, true)})");
            foreach (var sub in type.subtypes)
            {
                if (sub.count <= 0) continue;
                enemyLines.Add($"  SubType: {sub.subtype} ({NumberManager.FormatLargeNumber(sub.count, true)})");
            }
        }

        PopulateList(currencyContainer, currencyLines);
        PopulateList(enemyBreakdownContainer, enemyLines);
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

        if (rowTextPrefab == null)
        {
            Debug.LogWarning("RoundStatsView: rowTextPrefab not assigned.", this);
        }
        else if (rowTextPrefab.GetComponentInChildren<TextMeshProUGUI>(true) == null)
        {
            Debug.LogWarning("RoundStatsView: rowTextPrefab has no TextMeshProUGUI component (add one on the root or a child).", rowTextPrefab);
        }
    }

}
