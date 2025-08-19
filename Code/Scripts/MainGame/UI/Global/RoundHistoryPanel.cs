using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundHistoryPanel : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerManager playerManager;

    [Header("List (ScrollView)")]
    [SerializeField] private Transform listContent;   // ScrollView/Viewport/Content
    [SerializeField] private GameObject rowPrefab;    // prefab with Button + RoundHistoryRowUI

    [Header("Panels")]
    [SerializeField] private GameObject listPanel;    // parent that contains the ScrollView (History list)
    [SerializeField] private GameObject detailsPanel; // parent of detailsView

    [Header("Details")]
    [SerializeField] private RoundStatsView detailsView;    // RoundStatsView on this panel

    [Header("Empty State")]
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Options")]
    [SerializeField] private int maxRows = 50;
    [SerializeField] private bool showNewestFirst = true;

    private void OnEnable()
    {
        if (playerManager == null) playerManager = PlayerManager.main ?? FindObjectOfType<PlayerManager>(true);

        if (detailsView == null) detailsView = GetComponentInChildren<RoundStatsView>(true);
        if (detailsPanel == null && detailsView != null) detailsPanel = detailsView.gameObject;

        // Ensure details starts hidden
        if (detailsPanel != null) detailsPanel.SetActive(false);

        Subscribe();
        RefreshList();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        EventManager.StartListening(EventNames.RoundRecordCreated, OnRoundRecordCreated);
    }

    private void Unsubscribe()
    {
        EventManager.StopListening(EventNames.RoundRecordCreated, OnRoundRecordCreated);
    }

    private void OnRoundRecordCreated(object payload)
    {
        if (!(payload is RoundRecord record)) return;
        AddRow(record, insertAtTop: showNewestFirst);
    }

    [ContextMenu("Refresh List")]
    public void RefreshList()
    {
        if (listContent == null || rowPrefab == null)
        {
            Debug.LogWarning("RoundHistoryPanel: listContent or rowPrefab not set.", this);
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var history = playerManager?.playerData?.RoundHistory;
        bool hasAny = history != null && history.Count > 0;

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(!hasAny);
        if (!hasAny) return;

        IEnumerable<RoundRecord> ordered = showNewestFirst
            ? history.OrderByDescending(r => ParseEndedAt(r))
            : history.OrderBy(r => ParseEndedAt(r));

        int count = 0;
        foreach (var rec in ordered)
        {
            AddRow(rec, insertAtTop: false);
            count++;
            if (count >= maxRows) break;
        }
    }

    private void AddRow(RoundRecord rec, bool insertAtTop)
    {
        if (listContent == null || rowPrefab == null || rec == null) return;

        var go = Instantiate(rowPrefab);
        go.transform.SetParent(listContent, false);
        if (insertAtTop) go.transform.SetSiblingIndex(0);

        var row = go.GetComponent<RoundHistoryRowUI>() ?? go.GetComponentInChildren<RoundHistoryRowUI>(true);
        if (row != null)
            row.Bind(rec, () => ShowDetails(rec));

        var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"RoundHistory: row clicked -> {rec.endedAtIsoUtc}");
                ShowDetails(rec);
            });
        }
        else
        {
            Debug.LogWarning("RoundHistoryPanel: Row prefab has no Button.", go);
        }
    }

    private void ShowDetails(RoundRecord record)
    {
        if (detailsPanel == null || detailsView == null)
        {
            Debug.LogError("RoundHistoryPanel: detailsPanel/detailsView not assigned.");
            return;
        }

        // Show overlay and ensure it's on top
        detailsPanel.SetActive(true);
        detailsPanel.transform.SetAsLastSibling();

        var cg = detailsPanel.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

        detailsView.ShowRecord(record);
    }

    // Call this from the Close button on RoundStatViewer
    public void HideDetails()
    {
        if (detailsPanel == null) return;

        var cg = detailsPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.alpha = 1f; // keep visuals normal; SetActive handles visibility
        }
        detailsPanel.SetActive(false);
    }

    private static DateTime ParseEndedAt(RoundRecord r)
    {
        if (!string.IsNullOrEmpty(r?.endedAtIsoUtc) && DateTime.TryParse(r.endedAtIsoUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;
        return DateTime.MinValue;
    }
}