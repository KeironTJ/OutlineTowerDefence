using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint; // Reference to the spawn point

    private GameObject activeTowerInstance; // Reference to the active tower instance

    public Tower SpawnTower()
    {
        var playerData = PlayerManager.main.playerData;
        var visualManager = TowerVisualManager.Instance;
        var visualData = visualManager.GetVisualById(playerData.selectedTowerVisualId);

        if (visualData != null && visualData.visualPrefab != null && spawnPoint != null)
        {
            activeTowerInstance = Instantiate(visualData.visualPrefab, spawnPoint.position, spawnPoint.rotation);
            return activeTowerInstance.GetComponent<Tower>();
        }
        else
        {
            Debug.LogError("Selected tower visual or spawn point is missing.");
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
