using UnityEngine;

/// <summary>
/// Service that contributes tower stats based on the player's selected loadout.
/// This includes tower base, turrets, and projectiles.
/// </summary>
public class TowerLoadoutService : SingletonMonoBehaviour<TowerLoadoutService>, IStatContributor
{
    private PlayerManager playerManager;
    
    protected override void OnAwakeAfterInit()
    {
        // Base class handles singleton setup
    }
    
    private void Start()
    {
        playerManager = PlayerManager.main;
        
        // Register with TowerStatPipeline
        if (TowerStatPipeline.Instance != null)
        {
            TowerStatPipeline.Instance.RegisterContributor(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from TowerStatPipeline
        if (TowerStatPipeline.Instance != null)
        {
            TowerStatPipeline.Instance.UnregisterContributor(this);
        }
    }
    
    public void Contribute(StatCollector collector)
    {
        if (collector == null) return;
        if (playerManager == null)
            playerManager = PlayerManager.main;
        if (playerManager?.playerData == null) return;
        
        var playerData = playerManager.playerData;
        
        // Contribute from selected tower base
        ContributeFromTowerBase(collector, playerData.selectedTowerBaseId);
        
        // Contribute from selected turrets
        if (playerData.selectedTurretIds != null)
        {
            foreach (var turretId in playerData.selectedTurretIds)
            {
                if (!string.IsNullOrEmpty(turretId))
                {
                    ContributeFromTurret(collector, turretId);
                }
            }
        }
        
        // Contribute from selected projectiles
        if (playerData.selectedProjectilesBySlot != null)
        {
            foreach (var assignment in playerData.selectedProjectilesBySlot)
            {
                if (assignment != null && !string.IsNullOrEmpty(assignment.projectileId))
                {
                    ContributeFromProjectile(collector, assignment.projectileId);
                }
            }
        }
    }
    
    private void ContributeFromTowerBase(StatCollector collector, string towerBaseId)
    {
        if (string.IsNullOrEmpty(towerBaseId)) return;
        if (TowerBaseManager.Instance == null) return;
        
        var towerBase = TowerBaseManager.Instance.GetBaseById(towerBaseId);
        if (towerBase != null)
        {
            towerBase.ApplyStatBonuses(collector);
        }
    }
    
    private void ContributeFromTurret(StatCollector collector, string turretId)
    {
        if (string.IsNullOrEmpty(turretId)) return;
        if (TurretDefinitionManager.Instance == null) return;
        
        var turret = TurretDefinitionManager.Instance.GetById(turretId);
        if (turret != null)
        {
            turret.ApplyStatBonuses(collector);
        }
    }
    
    private void ContributeFromProjectile(StatCollector collector, string projectileId)
    {
        if (string.IsNullOrEmpty(projectileId)) return;
        if (ProjectileDefinitionManager.Instance == null) return;
        
        var projectile = ProjectileDefinitionManager.Instance.GetById(projectileId);
        if (projectile != null)
        {
            projectile.ApplyStatBonuses(collector);
        }
    }
    
    /// <summary>
    /// Manually trigger a stat recalculation when loadout changes
    /// </summary>
    public void OnLoadoutChanged()
    {
        TowerStatPipeline.SignalDirty();
    }
}
