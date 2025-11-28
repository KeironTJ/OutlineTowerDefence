public class DamageDealtEvent
{
    public string projectileId;
    public float damageAmount;
    public bool wasCritical;
    public string enemyDefinitionId;
    
    public DamageDealtEvent(string projectileId, float damageAmount, bool wasCritical, string enemyDefinitionId = "")
    {
        this.projectileId = projectileId;
        this.damageAmount = damageAmount;
        this.wasCritical = wasCritical;
        this.enemyDefinitionId = enemyDefinitionId;
    }
}
