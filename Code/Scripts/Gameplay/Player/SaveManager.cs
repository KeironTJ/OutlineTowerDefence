using System;
using System.Threading.Tasks;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager main;

    [SerializeField] private string slotId = "default";
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private float debounceDelay = 0.25f; // batch rapid changes

    // Local authoritative payload
    public PlayerSavePayload Current { get; private set; }
    public event Action<PlayerSavePayload> OnAfterLoad;
    public event Action<PlayerSavePayload> OnBeforeSave;

    private ISaveProvider provider;          // Always a LocalFileSaveProvider in the simplified (hybrid) model
    private float nextAutoTime;
    private bool dirty;
    private float earliestSaveTime;
    private bool loading;
    private bool freshCreate;
    private bool skipNextRevision;
    public bool FreshCreate => freshCreate;

    public string SlotId => slotId;
    public bool InitialLoadComplete { get; private set; }

    private async void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);

        provider = new LocalFileSaveProvider(); // Local always authoritative
        await LoadOrCreateAsync();
        ScheduleAuto();
    }

    private void Update()
    {
        if (!loading && dirty && Time.unscaledTime >= earliestSaveTime)
            DoSaveSync();

        if (Time.unscaledTime >= nextAutoTime)
        {
            QueueSave();
            ScheduleAuto();
        }
    }

    private void ScheduleAuto() => nextAutoTime = Time.unscaledTime + autoSaveInterval;

    public async Task LoadOrCreateAsync()
    {
        loading = true;
        var exists = await provider.ExistsAsync(slotId);
        if (exists)
        {
            Current = await provider.LoadAsync(slotId);
            if (Current == null)
                Current = PlayerSavePayload.CreateNew();
            MigrateIfNeeded(Current);
        }
        else
        {
            Current = PlayerSavePayload.CreateNew();
            freshCreate = true;
            Debug.Log("[SaveManager] Created new save (freshCreate=true) UUID=" + Current.player.UUID);
            ((LocalFileSaveProvider)provider).SaveSync(slotId, Current);
        }

        loading = false;
        OnAfterLoad?.Invoke(Current);
        // REMOVE: freshCreate = false;  (keep flag until adoption logic runs)
        InitialLoadComplete = true;
    }

    public void QueueImmediateSave()
    {
        QueueSave();
        earliestSaveTime = Time.unscaledTime; // save next frame
    }

    public void QueueSave()
    {
        dirty = true;
        if (earliestSaveTime < Time.unscaledTime + 0.01f)
            earliestSaveTime = Time.unscaledTime + debounceDelay;
    }

    private void DoSaveSync()
    {
        if (Current == null) return;
        dirty = false;
        OnBeforeSave?.Invoke(Current);
        Current.lastSaveIsoUtc = DateTime.UtcNow.ToString("o");
        if (!skipNextRevision) Current.revision++;
        skipNextRevision = false;
        Current.lastHash = ComputeHash(Current);
        ((LocalFileSaveProvider)provider).SaveSync(slotId, Current);
    }

    private void OnApplicationQuit()
    {
        if (dirty) DoSaveSync();
    }

    private void MigrateIfNeeded(PlayerSavePayload payload)
    {
        // Future data migrations by dataVersion
    }

    // Hybrid model support: allow external (CloudSyncService) to replace current payload
    public void ReplaceCurrent(PlayerSavePayload payload)
    {
        if (payload == null) return;
        Current = payload;
        freshCreate = false; // no longer a placeholder
        OnAfterLoad?.Invoke(Current);
        QueueImmediateSave();
    }

    public void ClearFreshFlag()
    {
        freshCreate = false;
    }

    private string ComputeHash(PlayerSavePayload payload)
    {
        var json = JsonUtility.ToJson(payload, false);
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    public void AdoptFromCloud(PlayerSavePayload payload)
    {
        if (payload == null) return;
        Current = payload;
        freshCreate = false;
        skipNextRevision = true; // prevent artificial revision bump
        OnAfterLoad?.Invoke(Current);
        QueueImmediateSave(); // persist locally
    }
}