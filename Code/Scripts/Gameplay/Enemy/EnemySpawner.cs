using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Area (Off-Screen)")]
    [Tooltip("Extra world units beyond the camera bounds to spawn enemies.")]
    [SerializeField] private float offscreenPadding = 2f;
    [Tooltip("Fallback ring distance if camera not found.")]
    [SerializeField] private float fallbackRadius = 25f;
    [Tooltip("Minimum distance from the target tower (to avoid popping on edge). 0 to disable.")]
    [SerializeField] private float minDistanceFromTower = 8f;
    [Tooltip("Clamp spawn Y (2D top-down) to this Z if needed.")]
    [SerializeField] private float fixedZ = 0f;
    [Tooltip("Visualize last spawn points in editor.")]
    [SerializeField] private bool debugGizmos = false;
    private readonly Queue<Vector3> debugPoints = new Queue<Vector3>();

    public GameObject SpawnEnemy(GameObject prefab, Tower tower)
    {
        if (!prefab) return null;
        Vector3 pos = GetRandomSpawnPosition(tower ? tower.transform.position : transform.position);
        return Instantiate(prefab, pos, Quaternion.identity);
    }

    private Vector3 GetRandomSpawnPosition(Vector3 center)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // Fallback: random ring around center
            Vector2 dir2 = Random.insideUnitCircle.normalized;
            Vector3 p = center + new Vector3(dir2.x, dir2.y, 0f) * fallbackRadius;
            p.z = fixedZ;
            RecordDebugPoint(p, Color.red);
            return p;
        }

        if (cam.orthographic)
        {
            float vertSize = cam.orthographicSize;
            float horSize = vertSize * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // Decide a side: 0=left 1=right 2=top 3=bottom
            int side = Random.Range(0, 4);
            float x = 0, y = 0;
            switch (side)
            {
                case 0: // left
                    x = camPos.x - horSize - offscreenPadding;
                    y = Random.Range(camPos.y - vertSize - offscreenPadding, camPos.y + vertSize + offscreenPadding);
                    break;
                case 1: // right
                    x = camPos.x + horSize + offscreenPadding;
                    y = Random.Range(camPos.y - vertSize - offscreenPadding, camPos.y + vertSize + offscreenPadding);
                    break;
                case 2: // top
                    y = camPos.y + vertSize + offscreenPadding;
                    x = Random.Range(camPos.x - horSize - offscreenPadding, camPos.x + horSize + offscreenPadding);
                    break;
                default: // bottom
                    y = camPos.y - vertSize - offscreenPadding;
                    x = Random.Range(camPos.x - horSize - offscreenPadding, camPos.x + horSize + offscreenPadding);
                    break;
            }
            Vector3 pos = new Vector3(x, y, fixedZ);

            // Ensure minimum distance from tower/center if requested
            if (minDistanceFromTower > 0f)
            {
                Vector3 flatCenter = new Vector3(center.x, center.y, pos.z);
                if (Vector3.Distance(pos, flatCenter) < minDistanceFromTower)
                {
                    Vector3 dir = (pos - flatCenter).normalized;
                    pos = flatCenter + dir * minDistanceFromTower;
                }
            }
            RecordDebugPoint(pos, Color.green);
            return pos;
        }
        else
        {
            // Perspective: pick a viewport point slightly outside [0,1]
            // Choose one axis to push out
            float u = Random.value;
            float v = Random.value;
            int edge = Random.Range(0, 4);
            const float pad = 0.08f;
            switch (edge)
            {
                case 0: u = -pad; break;       // left
                case 1: u = 1f + pad; break;   // right
                case 2: v = -pad; break;       // bottom
                case 3: v = 1f + pad; break;   // top
            }
            // Raycast from camera to world plane at Z = fixedZ (assuming XY plane)
            Ray r = cam.ViewportPointToRay(new Vector3(u, v, 0f));
            float t;
            Vector3 pos;
            if (Mathf.Abs(r.direction.z) > 0.0001f)
            {
                t = (fixedZ - r.origin.z) / r.direction.z;
                pos = r.origin + r.direction * Mathf.Max(t, 0f);
            }
            else
            {
                pos = r.origin + r.direction * 50f;
            }

            if (minDistanceFromTower > 0f)
            {
                Vector3 flatCenter = new Vector3(center.x, center.y, pos.z);
                if (Vector3.Distance(pos, flatCenter) < minDistanceFromTower)
                {
                    Vector3 dir = (pos - flatCenter).normalized;
                    pos = flatCenter + dir * minDistanceFromTower;
                }
            }
            RecordDebugPoint(pos, Color.cyan);
            return pos;
        }
    }

    private struct DebugSpawn
    {
        public Vector3 pos;
        public Color color;
    }
    private readonly Queue<DebugSpawn> recentSpawns = new Queue<DebugSpawn>();
    private void RecordDebugPoint(Vector3 p, Color c)
    {
        if (!debugGizmos) return;
        recentSpawns.Enqueue(new DebugSpawn { pos = p, color = c });
        while (recentSpawns.Count > 40) recentSpawns.Dequeue();
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.matrix = Matrix4x4.identity;
        foreach (var s in recentSpawns)
        {
            Gizmos.color = s.color;
            Gizmos.DrawWireSphere(s.pos, 0.6f);
        }
    }
}
