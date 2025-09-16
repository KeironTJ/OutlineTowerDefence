using System.Collections.Generic;
using UnityEngine;

public class LoadoutManager : MonoBehaviour
{
    public static LoadoutManager Instance { get; private set; }
    public LoadoutDefinition[] allLoadouts;

    private Dictionary<string, LoadoutDefinition> index = new Dictionary<string, LoadoutDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (var l in allLoadouts) if (l != null && !string.IsNullOrEmpty(l.id)) index[l.id] = l;
    }

    public LoadoutDefinition Get(string id) => index.TryGetValue(id, out var l) ? l : null;
}