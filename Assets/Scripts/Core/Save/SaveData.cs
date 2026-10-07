using System;
using System.Collections.Generic;

/// <summary>
/// Root serializable container for all persistent game save data.
/// Serialized via JsonUtility.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Save format version, reserved for migrating old saves.</summary>
    public int Version = 1;

    /// <summary>Economy progress, balance and offline timers data.</summary>
    public EconomyData Economy = new EconomyData();

    /// <summary>States of all saved buildings and slots.</summary>
    public List<BuildingSaveData> Buildings = new List<BuildingSaveData>();

    public StoreLevelData StoreLevel = new StoreLevelData();
}
