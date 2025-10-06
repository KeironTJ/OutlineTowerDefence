using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralised helpers for resolving human-friendly display names for projectiles,
/// turrets, and enemies based on their definition assets. Results are cached to
/// avoid repeated lookups at runtime and gracefully fall back to the raw id when
/// a definition cannot be found.
/// </summary>
public static class DefinitionDisplayNameUtility
{
    private static readonly Dictionary<string, string> ProjectileNames = new(System.StringComparer.Ordinal);
    private static bool _projectileResourcesCached;

    private static readonly Dictionary<string, string> TurretNames = new(System.StringComparer.Ordinal);
    private static bool _turretResourcesCached;

    private static readonly Dictionary<string, string> EnemyNames = new(System.StringComparer.Ordinal);
    private static bool _enemyResourcesCached;

    private static readonly Dictionary<string, string> TowerBaseNames = new(System.StringComparer.Ordinal);
    private static bool _towerBaseResourcesCached;

    private const string UnknownProjectile = "Unknown Projectile";
    private const string UnknownTurret = "Unknown Turret";
    private const string UnknownEnemy = "Unknown Enemy";
    private const string UnknownTowerBase = "Unknown Tower Base";

    public static string GetProjectileName(string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId))
            return UnknownProjectile;

        if (ProjectileNames.TryGetValue(projectileId, out var cached))
            return cached;

        if (ProjectileDefinitionManager.Instance != null &&
            ProjectileDefinitionManager.Instance.TryGet(projectileId, out var fromManager))
        {
            return RegisterProjectile(fromManager);
        }

        if (!_projectileResourcesCached)
        {
            CacheProjectilesFromResources();
            _projectileResourcesCached = true;
            if (ProjectileNames.TryGetValue(projectileId, out cached))
                return cached;
        }

        ProjectileNames[projectileId] = projectileId;
        return projectileId;
    }

    public static string GetTowerBaseName(string towerBaseId)
    { 
        if (string.IsNullOrEmpty(towerBaseId))
            return UnknownTowerBase;

        if (TowerBaseNames.TryGetValue(towerBaseId, out var cached))
            return cached;

        if (TowerBaseManager.Instance != null &&
            TowerBaseManager.Instance.TryGetBaseById(towerBaseId, out var fromManager))
        {
            return RegisterTowerBase(fromManager);
        }

        if (!_towerBaseResourcesCached)
        {
            CacheTowerBasesFromResources();
            _towerBaseResourcesCached = true;
            if (TowerBaseNames.TryGetValue(towerBaseId, out cached))
                return cached;
        }

        TowerBaseNames[towerBaseId] = towerBaseId;
        return towerBaseId;
    }




    public static string GetTurretName(string turretId)
    {
        if (string.IsNullOrEmpty(turretId))
            return UnknownTurret;

        if (TurretNames.TryGetValue(turretId, out var cached))
            return cached;

        if (TurretDefinitionManager.Instance != null &&
            TurretDefinitionManager.Instance.TryGet(turretId, out var fromManager))
        {
            return RegisterTurret(fromManager);
        }

        if (!_turretResourcesCached)
        {
            CacheTurretsFromResources();
            _turretResourcesCached = true;
            if (TurretNames.TryGetValue(turretId, out cached))
                return cached;
        }

        TurretNames[turretId] = turretId;
        return turretId;
    }

    public static string GetEnemyName(string enemyDefinitionId)
    {
        if (string.IsNullOrEmpty(enemyDefinitionId))
            return UnknownEnemy;

        if (EnemyNames.TryGetValue(enemyDefinitionId, out var cached))
            return cached;

        if (WaveManager.Instance != null)
        {
            var defs = WaveManager.Instance.EnemyDefinitions;
            if (defs != null)
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    var def = defs[i];
                    if (def == null || string.IsNullOrEmpty(def.id)) continue;
                    RegisterEnemy(def);
                    if (def.id == enemyDefinitionId)
                        return EnemyNames[enemyDefinitionId];
                }
            }
        }

        if (!_enemyResourcesCached)
        {
            CacheEnemiesFromResources();
            _enemyResourcesCached = true;
            if (EnemyNames.TryGetValue(enemyDefinitionId, out cached))
                return cached;
        }

        EnemyNames[enemyDefinitionId] = enemyDefinitionId;
        return enemyDefinitionId;
    }

    private static string RegisterProjectile(ProjectileDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
            return UnknownProjectile;

        string friendly = string.IsNullOrEmpty(definition.projectileName)
            ? definition.id
            : definition.projectileName;
        ProjectileNames[definition.id] = friendly;
        return friendly;
    }

    private static string RegisterTowerBase(TowerBaseData definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
            return UnknownTowerBase;

        string friendly = string.IsNullOrEmpty(definition.displayName)
            ? definition.id
            : definition.displayName;
        TowerBaseNames[definition.id] = friendly;
        return friendly;
    }


    private static string RegisterTurret(TurretDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
            return UnknownTurret;

        string friendly = string.IsNullOrEmpty(definition.displayName)
            ? definition.id
            : definition.displayName;
        TurretNames[definition.id] = friendly;
        return friendly;
    }

    private static string RegisterEnemy(EnemyTypeDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
            return UnknownEnemy;

        string friendly = string.IsNullOrEmpty(definition.displayName)
            ? definition.id
            : definition.displayName;
        EnemyNames[definition.id] = friendly;
        return friendly;
    }

    private static void CacheTowerBasesFromResources()
    {
        var loaded = Resources.LoadAll<TowerBaseData>("Data/TowerBases");
        if (loaded == null || loaded.Length == 0) return;
        foreach (var def in loaded)
            RegisterTowerBase(def);
    }

    private static void CacheProjectilesFromResources()
    {
        var loaded = Resources.LoadAll<ProjectileDefinition>("Data/Projectiles");
        if (loaded == null || loaded.Length == 0) return;
        foreach (var def in loaded)
            RegisterProjectile(def);
    }

    private static void CacheTurretsFromResources()
    {
        var loaded = Resources.LoadAll<TurretDefinition>("Data/Turrets");
        if (loaded == null || loaded.Length == 0) return;
        foreach (var def in loaded)
            RegisterTurret(def);
    }

    private static void CacheEnemiesFromResources()
    {
        CacheEnemiesFromPath("GameData/Enemies");
        CacheEnemiesFromPath("GameData/Enemies/EnemyTypes");
    }

    private static void CacheEnemiesFromPath(string resourcesPath)
    {
        var loaded = Resources.LoadAll<EnemyTypeDefinition>(resourcesPath);
        if (loaded == null || loaded.Length == 0) return;
        foreach (var def in loaded)
            RegisterEnemy(def);
    }
}
