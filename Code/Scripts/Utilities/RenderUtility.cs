#if UNITY_EDITOR
using UnityEngine;

public static class RenderUtility
{
    /// <summary>
    /// Calculate the combined bounds of all renderers in a GameObject hierarchy.
    /// </summary>
    public static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) 
            return new Bounds(go.transform.position, Vector3.zero);
        
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) 
            bounds.Encapsulate(renderers[i].bounds);
        
        return bounds;
    }
}
#endif
