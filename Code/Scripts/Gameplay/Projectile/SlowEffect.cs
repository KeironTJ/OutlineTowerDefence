using UnityEngine;

// Movement speed reduction effect for Slow trait
public class SlowEffect : MonoBehaviour
{
    private float slowMultiplier;
    private float duration;
    private Enemy targetEnemy;
    private float elapsed = 0f;

    public void Initialize(float slowMultiplier, float duration, Enemy enemy)
    {
        this.slowMultiplier = slowMultiplier;
        this.duration = duration;
        this.targetEnemy = enemy;
    }

    private void Update()
    {
        if (targetEnemy == null)
        {
            Destroy(this);
            return;
        }

        // Note: This is a simplified implementation
        // A full implementation would need to modify the enemy's speed stat
        // and stack/manage multiple slow effects properly
        // For now, we just track the effect duration
        
        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(this);
        }
    }
    
    // TODO: Integrate with Enemy class to actually modify movement speed
    // This would require adding a status effect system to Enemy
}
