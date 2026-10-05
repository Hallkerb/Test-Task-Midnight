using System;

/// <summary>
/// Represents the save state for an individual building or build slot.
/// Serialized via JsonUtility.
/// </summary>
[Serializable]
public class BuildingSaveData
{
    /// <summary>Unique identifier of the building.</summary>
    public string BuildingId;

    /// <summary>Current upgrade level.</summary>
    public int CurrentLevel;

    /// <summary>Indicates whether the building is unlocked and active.</summary>
    public bool IsUnlocked;
}
