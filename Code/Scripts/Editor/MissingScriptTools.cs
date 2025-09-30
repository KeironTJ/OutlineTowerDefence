using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class MissingScriptTools
{
    [MenuItem("Tools/Missing Scripts/Report Active Scene")]
    public static void ReportScene()
    {
        int go=0, comps=0, missing=0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            Tally(root, ref go, ref comps, ref missing);
        Debug.Log($"[MissingScripts] Scene GameObjects:{go} Components:{comps} Missing:{missing}");
    }

    [MenuItem("Tools/Missing Scripts/Report All Prefabs")]
    public static void ReportPrefabs()
    {
        var paths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
        int go=0, comps=0, missing=0;
        foreach (var p in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab) Tally(prefab, ref go, ref comps, ref missing);
        }
        Debug.Log($"[MissingScripts] Prefabs GameObjects:{go} Components:{comps} Missing:{missing}");
    }

    [MenuItem("Tools/Missing Scripts/Clean Prefabs (Remove Missing Components)")]
    public static void CleanPrefabs()
    {
        var paths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
        int cleaned = 0;
        foreach (var p in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (!prefab) continue;
            if (RemoveMissingOn(prefab)) { cleaned++; EditorUtility.SetDirty(prefab); }
        }
        if (cleaned > 0) AssetDatabase.SaveAssets();
        Debug.Log($"[MissingScripts] Cleaned prefabs: {cleaned}");
    }

    [MenuItem("Tools/Missing Scripts/Select Missing In Active Scene")]
    public static void SelectMissingInScene()
    {
        var list = new List<GameObject>();
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            CollectMissing(root, list);

        if (list.Count == 0)
        {
            Debug.Log("[MissingScripts] None found.");
            return;
        }
        Selection.objects = list.ToArray();
        Debug.Log($"[MissingScripts] Selected {list.Count} objects with missing scripts.");
    }

    private static void CollectMissing(GameObject go, List<GameObject> list)
    {
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null)
            {
                list.Add(go);
                break;
            }
        }
        foreach (Transform t in go.transform)
            CollectMissing(t.gameObject, list);
    }

    static void Tally(GameObject go, ref int goCount, ref int compCount, ref int missingCount)
    {
        goCount++;
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            compCount++;
            if (c == null) missingCount++;
        }
        foreach (Transform t in go.transform)
            Tally(t.gameObject, ref goCount, ref compCount, ref missingCount);
    }

    static bool RemoveMissingOn(GameObject root)
    {
        bool changed = false;
        var stack = new Stack<Transform>();
        stack.Push(root.transform);
        while (stack.Count > 0)
        {
            var tr = stack.Pop();
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
                changed = true;
            }
            foreach (Transform c in tr) stack.Push(c);
        }
        return changed;
    }
}