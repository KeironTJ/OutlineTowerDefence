using UnityEngine;
using UnityEngine.UI;

public class CloudSyncDebugUI : MonoBehaviour
{
    [SerializeField] Button forceUploadBtn;
    [SerializeField] Button forceDownloadBtn;
    [SerializeField] TMPro.TextMeshProUGUI status;

    private void Awake()
    {
        if (forceUploadBtn) forceUploadBtn.onClick.AddListener(() => _ = ForceUpload());
        if (forceDownloadBtn) forceDownloadBtn.onClick.AddListener(() => _ = ForceDownload());
    }

    private void Update()
    {
        if (status && GameServicesInitializer.main)
            status.text = GameServicesInitializer.main.SignedIn ? "Cloud Auth: OK" : "Cloud Auth: OFF";
    }

    private async System.Threading.Tasks.Task ForceUpload()
    {
        if (CloudSyncService.main == null) return;
        // Schedule immediate upload
        typeof(CloudSyncService).GetMethod("ScheduleUpload",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(CloudSyncService.main, null);
    }

    private async System.Threading.Tasks.Task ForceDownload()
    {
        if (CloudSyncService.main == null) return;
        // Force attempt adopt now (call private via reflection or expose a public wrapper instead)
    }
}