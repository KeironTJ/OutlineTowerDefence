using System;
using System.Threading.Tasks;
using UnityEngine;

public class CloudSyncService : MonoBehaviour
{
    [SerializeField] float uploadDebounceSeconds = 2f;
    [SerializeField] bool autoDownloadNewerOnStart = true;
    [SerializeField] int authRetrySeconds = 15;

    public static CloudSyncService main;

    private CloudSaveProvider cloud;
    private bool pendingUpload;
    private float earliestUpload;
    private bool busy;

    private float nextAuthAttemptTime;

    // suppress next cloud upload after downloaded/adopted cloud data
    private bool suppressOneUpload;

    public bool InitialAdoptAttempted { get; private set; }
    private bool adoptionSucceeded;

    // Add these fields/properties so other systems (Loader, PlayerManager) can await real completion
    private TaskCompletionSource<bool> syncTcs = new TaskCompletionSource<bool>();
    public Task SyncCompleted => syncTcs.Task;
    public bool InitialAdoptCompleted { get; private set; } = false;

    private void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);
        cloud = new CloudSaveProvider();
    }

    private async void Start()
    {
        // Wait local load
        while (SaveManager.main == null || SaveManager.main.Current == null)
            await Task.Yield();

        // Try cloud adopt
        if (autoDownloadNewerOnStart)
            await TryAdoptNewer();
        InitialAdoptAttempted = true;

        // Hook saves
        SaveManager.main.OnBeforeSave += OnBeforeLocalSave;
    }

    private void OnBeforeLocalSave(PlayerSavePayload _)
    {
        if (suppressOneUpload)
        {
            suppressOneUpload = false; // skip exactly once
            return;
        }
        ScheduleUpload();
    }

    private void Update()
    {
        if (pendingUpload && Time.unscaledTime >= earliestUpload && !busy)
            _ = UploadNow();

        // Retry adoption once auth later succeeds (e.g., auth was late)
        if (!adoptionSucceeded && SaveManager.main && SaveManager.main.FreshCreate && GameServicesInitializer.main && GameServicesInitializer.main.SignedIn)
        {
            // Only attempt once more
            _ = TryAdoptNewer();
            adoptionSucceeded = true; // prevent looping; TryAdoptNewer will set proper state/logs
        }
    }

    private async Task<bool> EnsureAuthReady()
    {
        if (GameServicesInitializer.main == null) return false;
        if (GameServicesInitializer.main.SignedIn) return true;
        if (Time.unscaledTime < nextAuthAttemptTime) return false;

        try
        {
            await GameServicesInitializer.main.InitAndSignInAsync();
            if (GameServicesInitializer.main.SignedIn)
            {
                return true;
            }
        }
        catch { }

        nextAuthAttemptTime = Time.unscaledTime + authRetrySeconds;
        return false;
    }

    public async void ForceUploadNow() => await UploadNow();
    public async void ForceDownloadNow() => await TryAdoptNewer(true);

    // Add helper to mark completion once a final decision was made (adopt/upload/keep)
    private void MarkInitialAdoptCompleted()
    {
        if (!InitialAdoptCompleted)
        {
            InitialAdoptCompleted = true;
            try { syncTcs.TrySetResult(true); } catch { /* swallow */ }
        }
    }

    private async Task TryAdoptNewer(bool force = false)
    {
        if (!await EnsureAuthReady())
        {
            Debug.Log("[CloudSync] Adoption deferred (auth not ready).");
            return;
        }

        var slot = SaveManager.main.SlotId;
        var fresh = SaveManager.main.FreshCreate;

        var (exists, cloudPayload, cloudRev) = await cloud.TryLoadWithRevision(slot);
        if (!exists || cloudPayload == null)
        {
            Debug.Log($"[CloudSync] No cloud data for slot={slot}. scheduling upload.");
            ScheduleUpload();
            return;
        }

        var local = SaveManager.main.Current;
        int localRev = local?.revision ?? -1;

        Debug.Log($"[CloudSync] ConflictCheck localRev={localRev} cloudRev={cloudRev} fresh={fresh}");

        if (fresh || localRev < cloudRev || force)
        {
            Debug.Log("[CloudSync] Adopting cloud payload.");
            suppressOneUpload = true;
            SaveManager.main.AdoptFromCloud(cloudPayload);
            PlayerManager.main?.ForceResyncFromCurrentSave();

            // final decision -> mark completion
            MarkInitialAdoptCompleted();
            return;
        }

        if (localRev > cloudRev)
        {
            Debug.Log("[CloudSync] Local newer -> upload.");
            ScheduleUpload();

            // final decision -> mark completion
            MarkInitialAdoptCompleted();
            return;
        }

        // Revisions equal: compare hash (optional)
        if (local.lastHash != cloudPayload.lastHash)
        {
            Debug.LogWarning("[CloudSync] Revision equal but hash differs -> keeping local & uploading.");
            ScheduleUpload();

            // final decision -> mark completion
            MarkInitialAdoptCompleted();
            return;
        }
        else
        {
            Debug.Log("[CloudSync] In sync (equal rev & hash).");
            // final decision -> mark completion
            MarkInitialAdoptCompleted();
            return;
        }
    }

    public void ScheduleUpload()
    {
        pendingUpload = true;
        earliestUpload = Time.unscaledTime + uploadDebounceSeconds;
    }

    private async Task UploadNow()
    {
        pendingUpload = false;
        if (!await EnsureAuthReady())
        {
            ScheduleUpload();
            return;
        }
        busy = true;
        var result = await cloud.UploadDetailed(SaveManager.main.SlotId, SaveManager.main.Current);
        switch (result)
        {
            case CloudUploadResult.Uploaded:
                Debug.Log("[CloudSync] Uploaded (changed).");
                break;
            case CloudUploadResult.SkippedNoChange:
                // Optional quiet
                break;
            case CloudUploadResult.Failed:
                ScheduleUpload();
                break;
        }
        busy = false;
    }

    private DateTime Parse(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return DateTime.MinValue;
        DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt);
        return dt;
    }
}