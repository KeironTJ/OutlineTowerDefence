#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TurretPreviewGenerator
{
    [MenuItem("Assets/Turrets/Create Preview (256px)", true)]
    private static bool ValidateSelection() => Selection.activeObject is GameObject;

    [MenuItem("Assets/Turrets/Create Preview (256px)")]
    private static void CreatePreview256() => CreatePreviewForSelected(256);

    [MenuItem("Assets/Turrets/Create Preview (512px)")]
    private static void CreatePreview512() => CreatePreviewForSelected(512);

    private static void CreatePreviewForSelected(int size)
    {
        var prefab = Selection.activeObject as GameObject;
        if (prefab == null) { Debug.LogWarning("Select a turret prefab in Project"); return; }

        var tex = RenderPrefabToTexture(prefab, size, new Color(0, 0, 0, 0)); // transparent bg
        if (tex == null) { Debug.LogError("Failed to render preview"); return; }

        // save PNG next to prefab
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string dir = Path.GetDirectoryName(prefabPath);
        string file = Path.GetFileNameWithoutExtension(prefabPath);
        string outPath = Path.Combine(dir ?? "Assets", $"{file}_preview_{size}.png").Replace("\\", "/");

        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(outPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 100;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outPath);
        Selection.activeObject = sprite;
        Debug.Log($"Preview saved: {outPath}");
    }

    // Renders a prefab into a square Texture2D using PreviewRenderUtility
    private static Texture2D RenderPrefabToTexture(GameObject prefab, int size, Color bg)
    {
        var pru = new PreviewRenderUtility();
        pru.camera.clearFlags = CameraClearFlags.Color;
        pru.camera.backgroundColor = bg;
        pru.camera.orthographic = true;

        var lightGO = new GameObject("PreviewLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        pru.AddSingleGO(lightGO);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        pru.AddSingleGO(instance);

        Bounds b = RenderUtility.CalculateBounds(instance);
        if (b.size == Vector3.zero) b = new Bounds(instance.transform.position, Vector3.one);
        float half = Mathf.Max(b.extents.x, b.extents.y);
        pru.camera.orthographicSize = half * 1.2f;
        pru.camera.transform.position = b.center + new Vector3(0, 0, -10f);
        pru.camera.transform.LookAt(b.center);
        pru.camera.nearClipPlane = 0.01f;
        pru.camera.farClipPlane = 1000f;

        // FIX: use BeginStaticPreview/EndStaticPreview
        pru.BeginStaticPreview(new Rect(0, 0, size, size));
        pru.camera.Render();
        Texture2D tex = pru.EndStaticPreview();

        // cleanup
        if (instance != null) Object.DestroyImmediate(instance);
        if (lightGO != null) Object.DestroyImmediate(lightGO);
        pru.Cleanup();
        return tex;
    }
}
#endif