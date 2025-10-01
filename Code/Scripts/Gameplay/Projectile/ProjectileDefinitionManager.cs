using UnityEngine;
using System.Collections.Generic;

public class ProjectileDefinitionManager : MonoBehaviour
{
    public static ProjectileDefinitionManager Instance;

    [Tooltip("Optional: assign in inspector or leave empty to auto-load from Resources/Data/Projectiles")]
    public List<ProjectileDefinition> allDefinitions = new List<ProjectileDefinition>();

    private Dictionary<string, ProjectileDefinition> map = new Dictionary<string, ProjectileDefinition>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (allDefinitions == null || allDefinitions.Count == 0)
            {
                var loaded = Resources.LoadAll<ProjectileDefinition>("Data/Projectiles");
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
                Debug.LogWarning($"ProjectileDefinition with empty id found: {def.name}");
                continue;
            }
            if (map.ContainsKey(def.id))
            {
                Debug.LogWarning($"Duplicate ProjectileDefinition id '{def.id}' - skipping duplicate asset {def.name}");
                continue;
            }
            map[def.id] = def;
        }
    }

    public ProjectileDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        map.TryGetValue(id, out var def);
        return def;
    }

    public List<ProjectileDefinition> GetAllProjectiles() => allDefinitions;

    public bool TryGet(string id, out ProjectileDefinition def) => map.TryGetValue(id, out def);

    public IReadOnlyList<ProjectileDefinition> GetAll() => allDefinitions;

    // Get projectiles matching a specific type
    public List<ProjectileDefinition> GetByType(ProjectileType type)
    {
        var result = new List<ProjectileDefinition>();
        foreach (var def in allDefinitions)
        {
            if (def != null && def.projectileType == type)
                result.Add(def);
        }
        return result;
    }

    // Get projectiles with specific trait
    public List<ProjectileDefinition> GetByTrait(ProjectileTrait trait)
    {
        var result = new List<ProjectileDefinition>();
        foreach (var def in allDefinitions)
        {
            if (def != null && def.HasTrait(trait))
                result.Add(def);
        }
        return result;
    }
}
