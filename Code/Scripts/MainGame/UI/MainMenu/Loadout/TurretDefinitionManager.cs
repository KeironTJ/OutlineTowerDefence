using UnityEngine;
using System.Collections.Generic;

public class TurretDefinitionManager : MonoBehaviour
{
    public static TurretDefinitionManager Instance;

    [Tooltip("Optional: assign in inspector or leave empty to auto-load from Resources/Data/Turrets")]
    public List<TurretDefinition> allDefinitions = new List<TurretDefinition>();

    private Dictionary<string, TurretDefinition> map = new Dictionary<string, TurretDefinition>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (allDefinitions == null || allDefinitions.Count == 0)
            {
                var loaded = Resources.LoadAll<TurretDefinition>("Data/Turrets");
                if (loaded != null && loaded.Length > 0)
                    allDefinitions.AddRange(loaded);
            }
            BuildMap();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildMap()
    {
        map.Clear();
        foreach (var def in allDefinitions)
        {
            if (def == null) continue;
            if (string.IsNullOrEmpty(def.id))
            {
                Debug.LogWarning($"TurretDefinition with empty id found: {def.name}");
                continue;
            }
            if (map.ContainsKey(def.id))
            {
                Debug.LogWarning($"Duplicate TurretDefinition id '{def.id}' - skipping duplicate asset {def.name}");
                continue;
            }
            map[def.id] = def;
        }
    }

    public TurretDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        map.TryGetValue(id, out var def);
        return def;
    }

    public bool TryGet(string id, out TurretDefinition def) => map.TryGetValue(id, out def);

    public IReadOnlyList<TurretDefinition> GetAll() => allDefinitions;

}