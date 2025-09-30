using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LocalFileSaveProvider : ISaveProvider
{
    private readonly string basePath;
    private static readonly object writeLock = new object();

    public LocalFileSaveProvider()
    {
        basePath = Application.persistentDataPath;
    }

    private string PathFor(string slot) => Path.Combine(basePath, $"player_{slot}.json");
    private string TempPathFor(string slot) => PathFor(slot) + ".tmp";

    public Task<bool> ExistsAsync(string slot) =>
        Task.FromResult(File.Exists(PathFor(slot)));

    public Task<PlayerSavePayload> LoadAsync(string slot)
    {
        PlayerSavePayload payload = null;
        try
        {
            var path = PathFor(slot);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                payload = JsonUtility.FromJson<PlayerSavePayload>(json);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LocalFileSaveProvider Load failed: " + ex);
        }
        return Task.FromResult(payload);
    }

    // Kept async signature for interface; does synchronous write
    public Task<bool> SaveAsync(string slot, PlayerSavePayload payload)
    {
        bool ok = SaveSync(slot, payload);
        return Task.FromResult(ok);
    }

    public bool SaveSync(string slot, PlayerSavePayload payload)
    {
        try
        {
            var finalPath = PathFor(slot);
            var tempPath  = TempPathFor(slot);
            var json = JsonUtility.ToJson(payload, false);

            lock (writeLock)
            {
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LocalFileSaveProvider Save failed: " + ex);
            return false;
        }
    }

    public Task<bool> DeleteAsync(string slot)
    {
        try
        {
            var p = PathFor(slot);
            if (File.Exists(p)) File.Delete(p);
            var t = TempPathFor(slot);
            if (File.Exists(t)) File.Delete(t);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LocalFileSaveProvider Delete failed: " + ex);
        }
        return Task.FromResult(true);
    }
}