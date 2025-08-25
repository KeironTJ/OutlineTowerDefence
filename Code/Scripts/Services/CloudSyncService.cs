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
    private bool authEverSucceeded;

    // NEW: suppress next cloud upload after we just downloaded/adopted cloud data
    private bool suppressOneUpload;

    public bool InitialAdoptAttempted { get; private set; }
    private bool adoptionSucceeded;
    [SerializeField] int adoptRetrySeconds = 20;

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
        if (GameServicesInitializer.main.SignedIn) { authEverSucceeded = true; return true; }
        if (Time.unscaledTime < nextAuthAttemptTime) return false;

        try
        {
            await GameServicesInitializer.main.InitAndSignInAsync();
            if (GameServicesInitializer.main.SignedIn)
            {
                authEverSucceeded = true;
                return true;
            }
        }
        catch { }

        nextAuthAttemptTime = Time.unscaledTime + authRetrySeconds;
        return false;
    }

    private async Task TryAdoptNewer()
    {
        if (!await EnsureAuthReady())
        {
            Debug.Log("[CloudSync] Adoption deferred (auth not ready).");
            return;
        }

        var slot = SaveManager.main.SlotId;
        var fresh = SaveManager.main.FreshCreate;

        var (exists, cloudPayload) = await cloud.TryLoad(slot);
        if (!exists || cloudPayload == null)
        {
            Debug.Log($"[CloudSync] No cloud data found for slot={slot}. fresh={fresh} -> scheduling upload.");
            ScheduleUpload();
            return;
        }

        var local = SaveManager.main.Current;
        var localTime = Parse(local.lastSaveIsoUtc);
        var cloudTime = Parse(cloudPayload.lastSaveIsoUtc);

        Debug.Log($"[CloudSync] Adoption check slot={slot} fresh={fresh} localTime={localTime:O} cloudTime={cloudTime:O}");

        if (fresh)
        {
            Debug.Log("[CloudSync] Fresh placeholder -> adopting cloud.");
            suppressOneUpload = true;
            SaveManager.main.ReplaceCurrent(cloudPayload);
            PlayerManager.main?.ForceResyncFromCurrentSave();
            adoptionSucceeded = true;
            return;
        }

        if (cloudTime > localTime)
        {
            Debug.Log("[CloudSync] Cloud newer -> adopting.");
            suppressOneUpload = true;
            SaveManager.main.ReplaceCurrent(cloudPayload);
            PlayerManager.main?.ForceResyncFromCurrentSave();
            adoptionSucceeded = true;
        }
        else
        {
            Debug.Log("[CloudSync] Keeping local (>= cloud) -> will upload.");
            ScheduleUpload();
        }
    }

    private void ScheduleUpload()
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