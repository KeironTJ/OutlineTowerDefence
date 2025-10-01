// filepath: c:\Users\keiro\Outline - Tower Defence\Assets\Code\Scripts\Persistence\CloudSaveProvider.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;

#pragma warning disable 0618
public enum CloudUploadResult { Uploaded, SkippedNoChange, Failed }

public class CloudSaveProvider
{
    private string DataKey(string slot) => $"player_{slot}_json";
    private string HashKey(string slot) => $"player_{slot}_hash";
    private string RevisionKey(string slot) => $"player_{slot}_rev";

    private async Task<bool> EnsureAuth()
    {
        if (GameServicesInitializer.main == null) return false;
        try
        {
            await GameServicesInitializer.main.InitAndSignInAsync();
            return GameServicesInitializer.main.SignedIn;
        }
        catch { return false; }
    }

    public async Task<(bool exists, PlayerSavePayload payload)> TryLoad(string slot)
    {
        if (!await EnsureAuth()) return (false, null);
        try
        {
            var dict = await RetryUtility.RetryAsync(
                () => CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { DataKey(slot) }),
                logPrefix: "CloudSync");
            if (dict != null && dict.TryGetValue(DataKey(slot), out var json) && !string.IsNullOrEmpty(json))
                return (true, JsonUtility.FromJson<PlayerSavePayload>(json));
            return (false, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CloudSync] Load failed: " + e.Message);
            return (false, null);
        }
    }

    public async Task<(bool exists, PlayerSavePayload payload, int revision)> TryLoadWithRevision(string slot)
    {
        if (!await EnsureAuth()) return (false, null, 0);
        try
        {
            var keys = new HashSet<string> { DataKey(slot), RevisionKey(slot) };
            var dict = await RetryUtility.RetryAsync(
                () => CloudSaveService.Instance.Data.LoadAsync(keys),
                logPrefix: "CloudSync");
            if (dict != null && dict.TryGetValue(DataKey(slot), out var json) && !string.IsNullOrEmpty(json))
            {
                var payload = JsonUtility.FromJson<PlayerSavePayload>(json);
                int rev = 0;
                if (dict.TryGetValue(RevisionKey(slot), out var revStr))
                    int.TryParse(revStr, out rev);
                return (true, payload, rev);
            }
            return (false, null, 0);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CloudSync] Load failed: " + e.Message);
            return (false, null, 0);
        }
    }

    public async Task<CloudUploadResult> UploadDetailed(string slot, PlayerSavePayload payload)
    {
        if (payload == null) return CloudUploadResult.Failed;
        if (!await EnsureAuth()) return CloudUploadResult.Failed;

        var json = JsonUtility.ToJson(payload, false);
        var newHash = HashUtility.MD5Hash(json);
        var rev = payload.revision;

        bool unchanged = false;
        try
        {
            var keys = new HashSet<string> { HashKey(slot), RevisionKey(slot) };
            var meta = await CloudSaveService.Instance.Data.LoadAsync(keys);
            if (meta != null &&
                meta.TryGetValue(HashKey(slot), out var existingHash) &&
                existingHash == newHash &&
                meta.TryGetValue(RevisionKey(slot), out var existingRevStr) &&
                int.TryParse(existingRevStr, out var existingRev) &&
                existingRev == rev)
            {
                unchanged = true;
            }
        }
        catch { }

        if (unchanged)
            return CloudUploadResult.SkippedNoChange;

        try
        {
            await RetryUtility.RetryAsync(async () =>
            {
                await CloudSaveService.Instance.Data.ForceSaveAsync(
                    new Dictionary<string, object>
                    {
                        { DataKey(slot), json },
                        { HashKey(slot), newHash },
                        { RevisionKey(slot), rev.ToString() }
                    });
                return true;
            }, logPrefix: "CloudSync");
            return CloudUploadResult.Uploaded;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CloudSync] Upload failed: " + e.Message);
            return CloudUploadResult.Failed;
        }
    }

    public async Task<bool> Upload(string slot, PlayerSavePayload payload)
    {
        var r = await UploadDetailed(slot, payload);
        return r != CloudUploadResult.Failed;
    }
}
#pragma warning restore 0618
