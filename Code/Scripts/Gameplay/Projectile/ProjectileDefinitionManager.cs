using UnityEngine;
using System.Collections.Generic;

public class ProjectileDefinitionManager : SingletonMonoBehaviour<ProjectileDefinitionManager>
{
    [Tooltip("Optional: assign in inspector or leave empty to auto-load from Resources/Data/Projectiles")]
    public List<ProjectileDefinition> allDefinitions = new List<ProjectileDefinition>();

    private Dictionary<string, ProjectileDefinition> map = new Dictionary<string, ProjectileDefinition>();

    protected override void OnAwakeAfterInit()
    {
        DefinitionLoader.LoadAndMerge(ref allDefinitions, "Data/Projectiles", def => def.id);
        map = DefinitionLoader.CreateLookup(allDefinitions, def => def.id);
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
