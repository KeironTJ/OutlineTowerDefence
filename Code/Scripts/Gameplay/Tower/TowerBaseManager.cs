using UnityEngine;
using System.Collections.Generic;

public class TowerBaseManager : MonoBehaviour
{
    public static TowerBaseManager Instance;

    public List<TowerBaseData> allBases = new List<TowerBaseData>();

    private Dictionary<string, TowerBaseData> towerBaseMap = new Dictionary<string, TowerBaseData>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load all TowerBaseData assets from Resources or assign in Inspector
            TowerBaseData[] loadedBases = Resources.LoadAll<TowerBaseData>("Data/TowerBases");
            allBases.AddRange(loadedBases);

            BuildMap();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildMap()
    {
        towerBaseMap.Clear();
        foreach (var baseData in allBases)
        {
            if (baseData == null) continue;
            if (string.IsNullOrEmpty(baseData.id))
            {
                Debug.LogWarning($"TowerBaseData with empty id found: {baseData.name}");
                continue;
            }
            if (towerBaseMap.ContainsKey(baseData.id))
            {
                Debug.LogWarning($"Duplicate TowerBaseData id '{baseData.id}' - skipping duplicate asset {baseData.name}");
                continue;
            }
            towerBaseMap[baseData.id] = baseData;
        }
    }



    public TowerBaseData GetBaseById(string id)
    {
        towerBaseMap.TryGetValue(id, out var baseData);
        return baseData;
    }

    public List<TowerBaseData> GetAllBases()
    {
        return new List<TowerBaseData>(allBases);
    }

    public bool TryGetBaseById(string id, out TowerBaseData baseData)
    {
        baseData = towerBaseMap.TryGetValue(id, out var foundBase) ? foundBase : null;
        return baseData != null;
    }
}