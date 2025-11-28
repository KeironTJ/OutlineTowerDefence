using UnityEngine;
using System.Collections.Generic;

public class TowerBaseManager : SingletonMonoBehaviour<TowerBaseManager>
{
    public List<TowerBaseData> allBases = new List<TowerBaseData>();

    private Dictionary<string, TowerBaseData> towerBaseMap = new Dictionary<string, TowerBaseData>();

    protected override void OnAwakeAfterInit()
    {
        // Load all TowerBaseData assets from Resources or assign in Inspector
        DefinitionLoader.LoadAndMerge(ref allBases, "Data/TowerBases", def => def.id);
        towerBaseMap = DefinitionLoader.CreateLookup(allBases, def => def.id);
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