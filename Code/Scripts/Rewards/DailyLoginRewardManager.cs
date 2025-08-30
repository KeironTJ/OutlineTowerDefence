using System;
using UnityEngine;

public class DailyLoginRewardManager : MonoBehaviour
{
    public static DailyLoginRewardManager main;

    [SerializeField] int dailyPremiumReward = 250;

    private void Awake()
    {
        main = this;
    }

    public bool CanClaimToday()
    {
        var pd = SaveManager.main?.Current?.player;
        if (pd == null) return false;
        if (string.IsNullOrEmpty(pd.lastDailyLoginIsoUtc)) return true;

        DateTime lastClaim;
        if (!DateTime.TryParse(pd.lastDailyLoginIsoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastClaim))
            return true;

        return DateTime.UtcNow.Date > lastClaim.Date;
    }

    public void ClaimToday()
    {
        var pd = SaveManager.main?.Current?.player;
        if (pd == null || !CanClaimToday()) return;

        pd.lastDailyLoginIsoUtc = DateTime.UtcNow.ToString("o");
        PlayerManager.main?.AddCredits(
            basic: 0,
            premium: dailyPremiumReward,
            luxury: 0,
            special: 0
        );
        pd.dailyLoginStreak = (pd.dailyLoginStreak == 0 || DateTime.UtcNow.Date == DateTime.Parse(pd.lastDailyLoginIsoUtc).AddDays(1).Date)
            ? pd.dailyLoginStreak + 1 : 1;

        SaveManager.main.QueueSave();
        CloudSyncService.main?.ScheduleUpload();
    }
}
