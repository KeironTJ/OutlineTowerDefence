using UnityEngine;
using System.Collections.Generic;

public class TurretDefinitionManager : SingletonMonoBehaviour<TurretDefinitionManager>
{
    [Tooltip("Optional: assign in inspector or leave empty to auto-load from Resources/Data/Turrets")]
    public List<TurretDefinition> allDefinitions = new List<TurretDefinition>();

    private Dictionary<string, TurretDefinition> map = new Dictionary<string, TurretDefinition>();

    protected override void OnAwakeAfterInit()
    {
        DefinitionLoader.LoadAndMerge(ref allDefinitions, "Data/Turrets", def => def.id);
        map = DefinitionLoader.CreateLookup(allDefinitions, def => def.id);
    }

    public TurretDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        map.TryGetValue(id, out var def);
        return def;
    }

    public List<TurretDefinition> GetAllTurrets() => allDefinitions;

    public bool TryGet(string id, out TurretDefinition def) => map.TryGetValue(id, out def);

    public IReadOnlyList<TurretDefinition> GetAll() => allDefinitions;

}