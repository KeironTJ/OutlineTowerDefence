using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public float timePerWave = 30f;
    public float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyScalingFactor = 1.5f;

    [Header("Debugging")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private bool isWaveActive = false;
    [SerializeField] private float waveEndTime;

    [Header("References")]
    [SerializeField] private int bossSpawnRate = 10;

    [Header("Enemy Factors")]
    [SerializeField] private int maxRatioWaves = 500; // Maximum number of waves for scaling
    [SerializeField] private float healthScalingFactor = 0.1f; // Scaling factor for health modifier
    [SerializeField] private float moveSpeedScalingFactor = 0.005f; // Scaling factor for move speed modifier
    [SerializeField] private float rewardScalingFactor = 0.1f; // Scaling factor for reward modifier
    [SerializeField] private float attackDamageScalingFactor = 0.1f; // Scaling factor for attack damage modifier;

    [Header("Spawn Frequency")]
    [SerializeField] private int maxSpawnWaves = 1000; // Maximum number of waves
    [SerializeField] private float minSpawnInterval = 0.2f; // Minimum time between spawns (allows up to 5 enemies per second)
    [SerializeField] private float maxSpawnInterval = 1f; // Maximum time between spawns (ensures at least 1 enemy per second)

    private Coroutine waveRoutine;
    private Tower subscribedTower; // Weak reference to the subscribed Tower

    public void StartWave(EnemySpawner enemySpawner, Tower tower)
    {
        waveRoutine = StartCoroutine(WaveRoutine(enemySpawner, tower));
        subscribedTower = tower; // Store the reference to the subscribed Tower
        tower.TowerDestroyed += EndWaveProgression; // Subscribe to the TowerDestroyed event
    }

    public bool IsBetweenWaves()
    {
        return !isWaveActive;
    }

    private IEnumerator WaveRoutine(EnemySpawner enemySpawner, Tower tower)
    {
        currentWave++;
        isWaveActive = true;
        waveEndTime = Time.time + timePerWave;

        EventManager.TriggerEvent(EventNames.NewWaveStarted, currentWave);

        // Adjust spawn weights for basic enemies
        enemySpawner.AdjustBasicSpawnWeights(currentWave, maxRatioWaves);

        // Calculate modifiers based on current wave with exponential scaling
        float healthModifier = Mathf.Pow(1 + healthScalingFactor, currentWave);
        float moveSpeedModifier = Mathf.Pow(1 + moveSpeedScalingFactor, currentWave);
        float attackDamageModifier = Mathf.Pow(1 + attackDamageScalingFactor, currentWave);
        float rewardModifier = Mathf.Pow(1 + rewardScalingFactor, currentWave);

        if (currentWave % bossSpawnRate == 0)
        {
            enemySpawner.SpawnBossEnemy(tower, healthModifier, moveSpeedModifier, attackDamageModifier);
        }

        // Spawn enemies during the wave
        while (Time.time < waveEndTime)
        {
            enemySpawner.SpawnBasicEnemy(tower, healthModifier, moveSpeedModifier, attackDamageModifier, rewardModifier);

            // Calculate dynamic spawn interval
            float spawnInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, (float)currentWave / maxSpawnWaves);
            yield return new WaitForSeconds(spawnInterval);
        }

        isWaveActive = false;
        yield return new WaitForSeconds(timeBetweenWaves);

        // Increment the wave counter and start the next wave
        StartWave(enemySpawner, tower); // Start the next wave
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public float GetWaveTimeRemaining()
    {
        if (isWaveActive)
        {
            float remainingTime = Mathf.Max(0, waveEndTime - Time.time);
            return remainingTime;
        }
        return 0;
    }

    public float GetTimeBetweenWavesRemaining()
    {
        if (isWaveActive)
        {
            return 0; // No time between waves if a wave is active
        }

        float timeSinceWaveEnded = Time.time - waveEndTime;
        float remainingTime = Mathf.Max(0, timeBetweenWaves - timeSinceWaveEnded);
        return remainingTime;
    }

    public int GetEnemiesLeftToSpawn()
    {
        // This method should return the number of enemies left to spawn in the current wave
        // For simplicity, we can assume a fixed number of enemies per wave
        return Mathf.RoundToInt(currentWave * difficultyScalingFactor);
    }

    public bool IsWaveActive()
    {
        return isWaveActive;
    }

    public int EnemiesPerWave()
    {
        return Mathf.RoundToInt(currentWave * difficultyScalingFactor);
    }

    public void EndWaveProgression()
    {
        isWaveActive = false;
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine); // Stop the current wave routine
            waveRoutine = null;
        }

        if (subscribedTower != null)
        {
            subscribedTower.TowerDestroyed -= EndWaveProgression; // Unsubscribe from the TowerDestroyed event
            subscribedTower = null; // Clear the reference
        }
    }
    
    private void OnDisable()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine); // Stop the wave routine when the WaveManager is disabled
        }

        if (subscribedTower != null)
        {
            subscribedTower.TowerDestroyed -= EndWaveProgression; // Unsubscribe from the TowerDestroyed event
            subscribedTower = null; // Clear the reference
        }
    }

}
