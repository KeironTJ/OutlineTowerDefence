using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Outline/Difficulty Progression")]
public class DifficultyProgression : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Min(1)] public int level;
        [Tooltip("Difficulty level whose wave record is checked. Leave 0 to use level - 1.")]
        [Min(0)] public int prerequisiteLevel;
        [Min(1)] public int requiredWave;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public int MinLevel => entries == null || entries.Length == 0
        ? 1
        : entries.Min(e => Mathf.Max(1, e.level));

    public int MaxLevel => entries == null || entries.Length == 0
        ? 1
        : entries.Max(e => Mathf.Max(1, e.level));

    public bool TryGetEntry(int level, out Entry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].level == level)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public int GetRequiredWave(int level)
    {
        return TryGetEntry(level, out var entry) ? entry.requiredWave : int.MaxValue;
    }

    public bool IsUnlocked(PlayerData data, int level)
    {
        if (data == null) return level <= 1;
        if (level <= MinLevel) return true;

        if (!TryGetEntry(level, out var entry))
        {
            return level <= Mathf.Max(1, data.maxDifficultyAchieved);
        }

        int prerequisite = entry.prerequisiteLevel > 0 ? entry.prerequisiteLevel : Mathf.Max(level - 1, MinLevel);
        int index = GetIndexForDifficulty(data, prerequisite);
        int bestWave = index >= 0 ? data.difficultyMaxWaveAchieved[index] : 0;

        return bestWave >= entry.requiredWave;
    }

    public int GetHighestUnlocked(PlayerData data)
    {
        if (data == null) return MinLevel;
        int highest = MinLevel;

        if (entries == null || entries.Length == 0)
        {
            return Mathf.Max(highest, Mathf.Max(1, data.maxDifficultyAchieved));
        }

        var ordered = entries.OrderBy(e => e.level);
        foreach (var entry in ordered)
        {
            if (IsUnlocked(data, entry.level))
                highest = Mathf.Max(highest, entry.level);
            else
                break;
        }

        return highest;
    }

    private static int GetIndexForDifficulty(PlayerData data, int difficulty)
    {
        if (data?.difficultyMaxWaveAchieved == null || data.difficultyMaxWaveAchieved.Length == 0)
            return -1;

        int index = Mathf.Clamp(difficulty - 1, 0, data.difficultyMaxWaveAchieved.Length - 1);
        return index;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].level = Mathf.Max(1, entries[i].level);
            entries[i].prerequisiteLevel = Mathf.Max(0, entries[i].prerequisiteLevel);
            entries[i].requiredWave = Mathf.Max(1, entries[i].requiredWave);
        }

        entries = entries
            .OrderBy(e => e.level)
            .ToArray();
    }
#endif
}