using UnityEngine;
using System.Collections.Generic;

public class TowerBaseManager : MonoBehaviour
{
    public static TowerBaseManager Instance;

    public List<TowerBaseData> allBases = new List<TowerBaseData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load all TowerBaseData assets from Resources or assign in Inspector
            TowerBaseData[] loadedBases = Resources.LoadAll<TowerBaseData>("Data/TowerBases");
            allBases.AddRange(loadedBases);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public TowerBaseData GetBaseById(string id)
    {
        return allBases.Find(b => b.id == id);
    }
}
