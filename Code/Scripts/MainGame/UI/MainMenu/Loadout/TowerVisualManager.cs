using UnityEngine;
using System.Collections.Generic;

public class TowerVisualManager : MonoBehaviour
{
    public static TowerVisualManager Instance;

    public List<TowerVisualData> allVisuals = new List<TowerVisualData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load all TowerVisualData assets from Resources or assign in Inspector
            TowerVisualData[] loadedVisuals = Resources.LoadAll<TowerVisualData>("Data/TowerVisuals");
            allVisuals.AddRange(loadedVisuals);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public TowerVisualData GetVisualById(string id)
    {
        return allVisuals.Find(v => v.id == id);
    }
}
