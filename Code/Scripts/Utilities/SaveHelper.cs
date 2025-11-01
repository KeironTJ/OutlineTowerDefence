/// <summary>
/// Helper utility for common save and cloud sync operations.
/// Centralizes the pattern of saving data and scheduling cloud uploads.
/// </summary>
public static class SaveHelper
{
    /// <summary>
    /// Queues a save and schedules a cloud upload.
    /// Use this for non-critical saves that can be batched.
    /// </summary>
    public static void SaveAndSync()
    {
        SaveManager.main?.QueueSave();
        CloudSyncService.main?.ScheduleUpload();
    }

    /// <summary>
    /// Performs an immediate save and schedules a cloud upload.
    /// Use this for critical data that should be saved immediately.
    /// </summary>
    public static void SaveImmediateAndSync()
    {
        SaveManager.main?.QueueImmediateSave();
        CloudSyncService.main?.ScheduleUpload();
    }
    
    /// <summary>
    /// Performs an immediate save only, without scheduling cloud upload.
    /// Use this when cloud sync will be triggered separately.
    /// </summary>
    public static void SaveImmediate()
    {
        SaveManager.main?.QueueImmediateSave();
    }
    
    /// <summary>
    /// Schedules a cloud upload only, without saving.
    /// Use this when save has already been queued separately.
    /// </summary>
    public static void SyncOnly()
    {
        CloudSyncService.main?.ScheduleUpload();
    }
}
