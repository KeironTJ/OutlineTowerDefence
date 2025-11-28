using System;

// Enemy classification types
public enum EnemyTier { Basic, Advanced, Elite, Boss }

// Flags so one definition can also be Flying+Summoner etc.
[Flags]
public enum EnemyTrait
{
    None      = 0,
    Fast      = 1 << 0,
    Tank      = 1 << 1,
    Glass     = 1 << 2,
    Flying    = 1 << 3,
    Shielded  = 1 << 4,
    Summoner  = 1 << 5,
    Explosive = 1 << 6,
    Support   = 1 << 7
}

public static class EnemyTraitExt
{
    public static bool Has(this EnemyTrait a, EnemyTrait b) => (a & b) != 0;
}