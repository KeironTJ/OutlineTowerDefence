using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyDetailPanelUI : MonoBehaviour
{
    [Serializable]
    public struct EnemyPreviewStats
    {
        public string definitionId;
        public EnemyTier tier;
        public string family;
        public EnemyTrait traits;
        public float health;
        public float speed;
        public float damage;
        public float damageInterval;
        public int fragments;
        public int cores;
        public int prisms;
        public int loops;

        public float DamagePerSecond => damageInterval > 0.001f ? damage / damageInterval : damage;
    }

    [Header("Data Sources")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private RoundManager roundManager;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private EnemyDetailRowUI rowPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private TextMeshProUGUI waveLabel;

    [Header("Behaviour")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private EnemyDetailRowUI headerRow;

    private readonly List<EnemyDetailRowUI> rowPool = new();
    private readonly Dictionary<string, int> killBuffer = new(StringComparer.Ordinal);

    private void OnEnable()
    {
        EventManager.StartListening(EventNames.RoundStatsUpdated, HandleStatsUpdated);
        EventManager.StartListening(EventNames.NewWaveStarted, HandleWaveChanged);

        if (refreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventNames.RoundStatsUpdated, HandleStatsUpdated);
        EventManager.StopListening(EventNames.NewWaveStarted, HandleWaveChanged);
    }

    private void HandleStatsUpdated(object _)
    {
        Refresh();
    }

    private void HandleWaveChanged(object _)
    {
        Refresh();
    }

    public void ShowPanel()
    {
        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);
        Refresh();
    }

    public void HidePanel()
    {
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panel == null)
        {
            Debug.LogWarning("[EnemyDetailPanelUI] Toggle requested but no panel assigned.");
            return;
        }

        if (panel.activeSelf)
            HidePanel();
        else
            ShowPanel();
    }

    [ContextMenu("Force Refresh")]
    public void Refresh()
    {
        if (rowPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[EnemyDetailPanelUI] Row prefab or content parent not assigned.");
            return;
        }

        if (waveManager == null)
        {
            Debug.LogWarning("[EnemyDetailPanelUI] WaveManager reference missing.");
            return;
        }

        var definitions = waveManager.EnemyDefinitions;
        if (definitions == null || definitions.Length == 0)
        {
            ClearRows();
            UpdateWaveLabel();
            return;
        }

        var ctx = waveManager.GetCurrentWaveContext();
        var kills = GetKillSnapshot();

        if (headerRow != null)
            headerRow.BindHeader();

        int rowIndex = 0;
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var def in definitions)
        {
            if (def == null) continue;

            string id = string.IsNullOrEmpty(def.id) ? def.name : def.id;
            if (!seenIds.Add(id))
                continue; // avoid duplicates if definition array reuses entries

            var stats = BuildPreview(def, ctx);
            stats.definitionId = id;

            int killCount = 0;
            if (kills != null && kills.TryGetValue(id, out var storedKills))
                killCount = storedKills;

            var row = GetOrCreateRow(rowIndex++);
            row.Bind(def, stats, killCount);
        }

        DisableExtraRows(rowIndex);
        UpdateWaveLabel();
    }

    private EnemyPreviewStats BuildPreview(EnemyTypeDefinition def, WaveContext ctx)
    {
        var preview = new PreviewRuntime();
        def.ApplyToRuntime(ctx, preview);
        return new EnemyPreviewStats
        {
            definitionId = preview.DefinitionId,
            tier = def.tier,
            family = def.family,
            traits = def.traits,
            health = preview.Health,
            speed = preview.Speed,
            damage = preview.Damage,
            damageInterval = preview.DamageInterval,
            fragments = preview.Fragments,
            cores = preview.Cores,
            prisms = preview.Prisms,
            loops = preview.Loops
        };
    }

    private EnemyDetailRowUI GetOrCreateRow(int index)
    {
        while (rowPool.Count <= index)
        {
            var instance = Instantiate(rowPrefab, contentParent);
            rowPool.Add(instance);
        }

        var row = rowPool[index];
        if (!row.gameObject.activeSelf)
            row.gameObject.SetActive(true);
        return row;
    }

    private void DisableExtraRows(int activeCount)
    {
        for (int i = activeCount; i < rowPool.Count; i++)
        {
            if (rowPool[i])
                rowPool[i].gameObject.SetActive(false);
        }
    }

    private void ClearRows()
    {
        foreach (var row in rowPool)
            if (row) row.gameObject.SetActive(false);
    }

    private void UpdateWaveLabel()
    {
        if (waveLabel == null || waveManager == null) return;

        int wave = Mathf.Max(1, waveManager.GetCurrentWave());
        waveLabel.text = $"Enemy Roster — Wave {wave}";
    }

    private IReadOnlyDictionary<string, int> GetKillSnapshot()
    {
        if (roundManager == null)
            return null;

        killBuffer.Clear();
        var source = roundManager.GetEnemyKillsByDefinition();
        if (source == null) return killBuffer;

        foreach (var kvp in source)
            killBuffer[kvp.Key] = kvp.Value;

        return killBuffer;
    }

    private sealed class PreviewRuntime : IEnemyRuntime
    {
        public float Health { get; private set; }
        public float Speed { get; private set; }
        public float Damage { get; private set; }
        public float DamageInterval { get; private set; }
        public int Fragments { get; private set; }
        public int Cores { get; private set; }
        public int Prisms { get; private set; }
        public int Loops { get; private set; }
        public string DefinitionId { get; private set; }

        public void InitStats(float health, float speed, float damage, float damageInterval)
        {
            Health = health;
            Speed = speed;
            Damage = damage;
            DamageInterval = damageInterval;
        }

        public void SetRewards(int fragments, int cores, int prisms, int loops)
        {
            Fragments = fragments;
            Cores = cores;
            Prisms = prisms;
            Loops = loops;
        }

        public void SetTarget(Tower tower)
        {
            // Not required for preview data.
        }

        public void SetDefinitionId(string id)
        {
            DefinitionId = id;
        }

        public void CacheDefinitionMeta(EnemyTier tier, string family, EnemyTrait traits)
        {
            // Metadata already available through the definition and not required here.
        }
    }
}
