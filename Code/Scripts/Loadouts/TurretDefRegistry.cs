using System.Collections.Generic;
using UnityEngine;

public class TurretDefRegistry : MonoBehaviour
{
    public static TurretDefRegistry Instance { get; private set; }

    [Tooltip("Assign all TurretDefinition assets here (or load from Resources).")]
    public TurretDefinition[] allTurretDefinitions;

    private Dictionary<string, TurretDefinition> index = new Dictionary<string, TurretDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (var d in allTurretDefinitions)
            if (d != null && !string.IsNullOrEmpty(d.id))
                index[d.id] = d;
    }

    public TurretDefinition GetById(string id) => index.TryGetValue(id, out var d) ? d : null;

    public string[] DebugListIds()
    {
        var keys = new List<string>(index.Keys);
        return keys.ToArray();
    }
}