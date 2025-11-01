using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility for loading ScriptableObject definitions from Resources.
/// Reduces duplication of Resources.Load patterns across managers.
/// </summary>
public static class DefinitionLoader
{
    /// <summary>
    /// Loads all ScriptableObjects of type T from the specified Resources path.
    /// Returns empty list if path doesn't exist or no definitions are found.
    /// </summary>
    public static List<T> LoadAll<T>(string resourcePath) where T : ScriptableObject
    {
        var loaded = Resources.LoadAll<T>(resourcePath);
        return loaded != null && loaded.Length > 0 
            ? new List<T>(loaded) 
            : new List<T>();
    }

    /// <summary>
    /// Loads definitions and merges them with an existing list, avoiding duplicates based on ID.
    /// Useful for managers that may have definitions assigned in inspector or loaded from Resources.
    /// </summary>
    public static void LoadAndMerge<T>(ref List<T> existingList, string resourcePath, System.Func<T, string> getIdFunc) 
        where T : ScriptableObject
    {
        if (existingList == null)
            existingList = new List<T>();

        var seenIds = new HashSet<string>(existingList
            .Where(def => def != null && !string.IsNullOrEmpty(getIdFunc(def)))
            .Select(getIdFunc));

        var loaded = Resources.LoadAll<T>(resourcePath);
        if (loaded != null && loaded.Length > 0)
        {
            foreach (var def in loaded)
            {
                if (def == null) continue;
                string id = getIdFunc(def);
                if (string.IsNullOrEmpty(id)) continue;
                
                if (seenIds.Add(id))
                    existingList.Add(def);
            }
        }
    }

    /// <summary>
    /// Creates a dictionary lookup from a list of definitions by ID.
    /// Handles null checks and empty ID validation.
    /// Logs a warning if duplicate IDs are encountered.
    /// </summary>
    public static Dictionary<string, T> CreateLookup<T>(IEnumerable<T> definitions, System.Func<T, string> getIdFunc) 
        where T : class
    {
        var lookup = new Dictionary<string, T>();
        if (definitions == null) return lookup;

        foreach (var def in definitions)
        {
            if (def == null) continue;
            string id = getIdFunc(def);
            if (string.IsNullOrEmpty(id)) continue;
            
            if (lookup.ContainsKey(id))
            {
                Debug.LogWarning($"[DefinitionLoader] Duplicate definition ID '{id}' found for type {typeof(T).Name}. Using first occurrence.");
                continue;
            }
            
            lookup[id] = def;
        }
        return lookup;
    }
}
