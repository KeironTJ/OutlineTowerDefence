using System;
using UnityEngine;

[Serializable]
public class PlayerSavePayload
{
    public int dataVersion = 1;
    public string createdAtIsoUtc;
    public string lastSaveIsoUtc;
    public PlayerData player;

    public static PlayerSavePayload CreateNew()
    {
        var now = DateTime.UtcNow.ToString("o");
        return new PlayerSavePayload
        {
            createdAtIsoUtc = now,
            lastSaveIsoUtc = now,
            player = new PlayerData()
        };
    }
}
