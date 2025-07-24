using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject towerPrefab; // Reference to the tower prefab
    [SerializeField] private Transform spawnPoint; // Reference to the spawn point

    private GameObject activeTowerInstance; // Reference to the active tower instance

    private void Start()
    {
        
    }

    public Tower SpawnTower()
    {
        if (towerPrefab != null && spawnPoint != null)
        {
            activeTowerInstance = Instantiate(towerPrefab, spawnPoint.position, spawnPoint.rotation);
            return activeTowerInstance.GetComponent<Tower>(); // Return the Tower component
        }
        else
        {
            Debug.LogError("TowerPrefab or SpawnPoint is not assigned.");
            return null;
        }
    }

    public GameObject GetActiveTowerInstance()
    {
        return activeTowerInstance;
    }

    public void DestroyTower()
    {
        if (activeTowerInstance != null)
        {
            Destroy(activeTowerInstance);
        }
    }
}
