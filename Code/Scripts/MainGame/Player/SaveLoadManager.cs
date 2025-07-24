using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class SaveLoadManager : MonoBehaviour
{
    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "playerData.json");
    }

    public void SaveData(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Data saved to local file");
    }

    public bool SaveFileExists()
    {
        // Debug.Log(File.Exists(filePath));
        return File.Exists(filePath);
    }

    public PlayerData LoadData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            // Debug.Log("Data loaded: " + json);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            if (data == null)
            {
                Debug.LogError("Failed to deserialize PlayerData from JSON.");
            }
            else
            {
                // Debug.Log("Successfully deserialized PlayerData: " + JsonUtility.ToJson(data, true));
            }
            return data;
        }
        Debug.LogWarning("No save file found at: " + filePath);
        return null;
    }



}

