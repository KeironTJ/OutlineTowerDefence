using System;

// Flags so one projectile can have multiple traits (e.g., Piercing + Explosive)
[Flags]
public enum ProjectileTrait
{
    None        = 0,
    Penetrate   = 1 << 0,  // Passes through enemies
    Piercing    = 1 << 1,  // Damage over time effect
    Explosive   = 1 << 2,  // Area of effect damage
    Slow        = 1 << 3,  // Slows enemy movement
    IncoreCores = 1 << 4,  // Increases core rewards on kill
    IncFragment = 1 << 5,  // Increases fragment rewards on kill
    Homing      = 1 << 6,  // Tracks target after firing
    Chain       = 1 << 7   // Chains to nearby enemies
}

public static class ProjectileTraitExt
{
    public static bool Has(this ProjectileTrait a, ProjectileTrait b) => (a & b) != 0;
}
