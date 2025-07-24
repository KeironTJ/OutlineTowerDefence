using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] bossEnemyPrefabs; // Boss enemy prefab

    [System.Serializable]
    public class BasicEnemyType
    {
        public string typeName; // Name of the enemy type (e.g., Simple, Fast, Tank)
        public GameObject prefab; // Prefab for this type
        public float spawnWeight; // Weight for spawning this type
    }

    [Header("Basic Enemy Types")]
    [SerializeField] private List<BasicEnemyType> basicEnemyTypes; // List of basic enemy types
    [SerializeField] private int bossWaveInterval = 10; // Interval for boss waves

    private float totalBasicSpawnWeight;

    private void UpdateTotalBasicSpawnWeight()
    {
        totalBasicSpawnWeight = 0;
        foreach (var type in basicEnemyTypes)
        {
            totalBasicSpawnWeight += type.spawnWeight;
        }
    }

    public void AdjustBasicSpawnWeights(int currentWave, int maxWaves)
    {
        Debug.Log($"Adjusting spawn weights for wave {currentWave} out of {maxWaves}");

        float simpleRatio = Mathf.Lerp(1f, 0.33f, (float)currentWave / maxWaves);
        float fastRatio = Mathf.Lerp(0f, 0.33f, (float)currentWave / (maxWaves * 0.5f)); // Fast enemies phase in sooner
        float tankRatio = Mathf.Lerp(0f, 0.33f, (float)currentWave / maxWaves); // Tank enemies phase in later

        foreach (var type in basicEnemyTypes)
        {
            if (type.typeName == "Simple")
            {
                type.spawnWeight = simpleRatio;
                Debug.Log($"Simple spawn weight updated to: {type.spawnWeight}");
            }
            else if (type.typeName == "Fast")
            {
                type.spawnWeight = fastRatio;
                Debug.Log($"Fast spawn weight updated to: {type.spawnWeight}");
            }
            else if (type.typeName == "Tank")
            {
                type.spawnWeight = tankRatio;
                Debug.Log($"Tank spawn weight updated to: {type.spawnWeight}");
            }
        }
        UpdateTotalBasicSpawnWeight();
        Debug.Log($"Total basic spawn weight updated to: {totalBasicSpawnWeight}");
    }

    public void SpawnBasicEnemy(Tower tower, float healthModifier, float moveSpeedModifier, float attackDamageModifier, float rewardModifier)
    {
        UpdateTotalBasicSpawnWeight();

        float randomValue = Random.Range(0, totalBasicSpawnWeight);
        BasicEnemyType selectedType = null;

        foreach (var type in basicEnemyTypes)
        {
            if (randomValue < type.spawnWeight)
            {
                selectedType = type;
                break;
            }
            randomValue -= type.spawnWeight;
        }

        if (selectedType != null)
        {
            Debug.Log($"Spawning basic enemy of type: {selectedType.typeName}");
            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject enemyObject = Instantiate(selectedType.prefab, spawnPosition, Quaternion.identity);
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(tower, healthModifier, moveSpeedModifier, attackDamageModifier, rewardModifier);
            }
            else
            {
                Debug.LogError("Spawned object does not have an Enemy component attached.");
            }
        }
        else
        {
            Debug.LogError("No enemy type selected for spawning.");
        }
    }

    public void SpawnBossEnemy(Tower tower, float healthModifier, float moveSpeedModifier, float attackDamageModifier)
    {
        if (bossEnemyPrefabs.Length == 0)
        {
            Debug.LogError("No boss enemy prefabs assigned.");
            return;
        }

        int enemyIndex = Random.Range(0, bossEnemyPrefabs.Length);
        Debug.Log($"Spawning boss enemy from prefab index: {enemyIndex}");
        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject enemyObject = Instantiate(bossEnemyPrefabs[enemyIndex], spawnPosition, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(tower, healthModifier, moveSpeedModifier, attackDamageModifier);
        }
        else
        {
            Debug.LogError("Spawned boss object does not have an Enemy component attached.");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Spawn enemies off-screen, randomly choosing a side and position along that side
        float screenWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float screenHeight = Camera.main.orthographicSize;

        int side = Random.Range(0, 4); // 0 = top, 1 = bottom, 2 = left, 3 = right
        switch (side)
        {
            case 0: // Top
                return new Vector3(Random.Range(-screenWidth, screenWidth), screenHeight + 1, 0);
            case 1: // Bottom
                return new Vector3(Random.Range(-screenWidth, screenWidth), -screenHeight - 1, 0);
            case 2: // Left
                return new Vector3(-screenWidth - 1, Random.Range(-screenHeight, screenHeight), 0);
            case 3: // Right
                return new Vector3(screenWidth + 1, Random.Range(-screenHeight, screenHeight), 0);
            default:
                return Vector3.zero; // Fallback, should not happen
        }
    }

}
