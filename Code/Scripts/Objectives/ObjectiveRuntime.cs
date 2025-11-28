using System;

public class ObjectiveRuntime
{
    public ObjectiveDefinition definition;
    public ObjectiveProgressData progressData; // Direct link to saved struct
    public float Current => progressData.currentProgress;
    public bool Completed => progressData.completed;
    public bool Claimed => progressData.claimed;
    public DateTime AssignedAtUtc =>
        DateTime.TryParse(progressData.assignedAtIsoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt : DateTime.MinValue;
}