// Categorizes projectiles by their base type - turrets can restrict which types they support
public enum ProjectileType
{
    Standard,      // Basic bullets
    Shard,         // Sharp projectiles (like MicroshardBlaster)
    Energy,        // Laser/energy-based
    Missile,       // Guided/explosive
    Bolt,          // Crossbow/heavy projectiles
    Plasma,        // Advanced energy
    Shell          // Artillery/cannon rounds
}
