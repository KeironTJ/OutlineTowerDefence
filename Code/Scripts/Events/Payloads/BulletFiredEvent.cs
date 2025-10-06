using UnityEngine;

public class BulletFiredEvent
{
    public Bullet bullet;
    public string turretId;
    public string projectileId;
    public float baseDamage;
    
    public BulletFiredEvent(Bullet bullet, string turretId, string projectileId, float baseDamage)
    {
        this.bullet = bullet;
        this.turretId = turretId;
        this.projectileId = projectileId;
        this.baseDamage = baseDamage;
    }
}
