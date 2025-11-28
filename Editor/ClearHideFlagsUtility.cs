using UnityEditor;
using UnityEngine;

public static class ClearHideFlagsUtility
{
    [MenuItem("Tools/Clear DontSave HideFlags")] 
    public static void ClearDontSaveFlags()
    {
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();
        int clearedCount = 0;

        foreach (string path in assetPaths)
        {
            if (!path.StartsWith("Assets"))
                continue;

            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                continue;

            if (asset.hideFlags != HideFlags.None)
            {
                Debug.Log($"[ClearHideFlagsUtility] Resetting hideFlags on {path} (was {asset.hideFlags})");
                asset.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(asset);
                clearedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ClearHideFlagsUtility] Cleared hideFlags on {clearedCount} asset(s)");
    }
}
