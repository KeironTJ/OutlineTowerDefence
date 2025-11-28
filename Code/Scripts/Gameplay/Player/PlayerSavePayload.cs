using System;

[Serializable]
public class PlayerSavePayload
{
    public int dataVersion = 1;
    public string createdAtIsoUtc;
    public string lastSaveIsoUtc;
    public int revision;
    public string lastHash;
    public PlayerData player;

    public static PlayerSavePayload CreateNew()
    {
        var now = DateTime.UtcNow.ToString("o");
        return new PlayerSavePayload
        {
            createdAtIsoUtc = now,
            lastSaveIsoUtc = now,
            revision = 0,
            lastHash = "",
            player = new PlayerData()
        };
    }
}
