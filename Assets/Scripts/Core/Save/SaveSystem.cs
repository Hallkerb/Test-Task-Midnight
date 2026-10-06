using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles persistent save/load operations using JSON file serialization.
/// Captures and restores the economy (EconomyManager) and the state of every BuildSlot in the scene.
/// Customers are not saved: they are recreated from the pool.
/// Settings are not part of the save file: they live in PlayerPrefs (see GameSettings).
/// </summary>
public static class SaveSystem
{
    private const string SaveFileName = "savegame.json";

    /// <summary>
    /// Absolute persistent file path for saving and loading data.
    /// </summary>
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    /// <summary>True if a save file exists (used by the main menu).</summary>
    public static bool HasSave => File.Exists(SaveFilePath);

    /// <summary>
    /// Captures the current game state (economy and build slots) and writes it to a JSON file.
    /// Must be called while the game scene is alive.
    /// </summary>
    public static void SaveGame()
    {
        try
        {
            SaveData saveData = LoadRawData();

            if (EconomyManager.Instance != null)
            {
                saveData.Economy = EconomyManager.Instance.CaptureData();
            }

            // Never wipe the saved buildings if this is called from a scene that has no slots.
            List<BuildingSaveData> buildings = CaptureBuildings();
            if (buildings.Count > 0)
            {
                saveData.Buildings = buildings;
            }

            WriteToDisk(JsonUtility.ToJson(saveData, true));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to save game data: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads the save file and restores the economy and the build slots.
    /// If there is no save (or it is unreadable) a new game is started instead.
    /// </summary>
    /// <returns>Amount of offline cash earned during absence, or 0 if no save exists.</returns>
    public static double LoadGame()
    {
        if (!TryReadSave(out SaveData saveData))
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.ResetToNewGame();
            }
            return 0;
        }

        double offlineEarnings = 0;

        if (EconomyManager.Instance != null && saveData.Economy != null)
        {
            offlineEarnings = EconomyManager.Instance.ApplyData(saveData.Economy);
        }

        ApplyBuildings(saveData.Buildings);

        return offlineEarnings;
    }

    /// <summary>
    /// Reads raw SaveData from disk without side-effects or mutating manager states.
    /// </summary>
    /// <returns>Deserialized SaveData instance or a new one if the file is missing or unreadable.</returns>
    public static SaveData LoadRawData()
    {
        return TryReadSave(out SaveData data) ? data : new SaveData();
    }

    /// <summary>
    /// Deletes save file from disk if it exists.
    /// </summary>
    public static void ClearSave()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to delete save file: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------
    // Disk access
    // ------------------------------------------------------------------
    /// <summary>Returns false if the file is missing or cannot be parsed.</summary>
    private static bool TryReadSave(out SaveData data)
    {
        data = null;

        if (!File.Exists(SaveFilePath))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveFilePath));
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to read save file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Writes to a temporary file first and then swaps it in, so a crash in the middle
    /// of writing cannot destroy the previous save.
    /// </summary>
    private static void WriteToDisk(string json)
    {
        string tempPath = SaveFilePath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(SaveFilePath))
        {
            File.Replace(tempPath, SaveFilePath, null);
        }
        else
        {
            File.Move(tempPath, SaveFilePath);
        }
    }

    // ------------------------------------------------------------------
    // Build slots
    // ------------------------------------------------------------------
    private static List<BuildingSaveData> CaptureBuildings()
    {
        var result = new List<BuildingSaveData>();
        var usedIds = new HashSet<string>();

        foreach (BuildSlot slot in StoreRegistry.Slots)
        {
            if (!usedIds.Add(slot.SlotId))
            {
                Debug.LogWarning($"[SaveSystem] Duplicate slot id '{slot.SlotId}': give every BuildSlot a unique Slot Id.", slot);
            }

            result.Add(new BuildingSaveData
            {
                BuildingId = slot.SlotId,
                CurrentLevel = slot.Level,
                IsUnlocked = slot.State == SlotState.Built
            });
        }

        // A stable order keeps the JSON readable and diff-friendly.
        result.Sort((a, b) => string.CompareOrdinal(a.BuildingId, b.BuildingId));
        return result;
    }

    private static void ApplyBuildings(List<BuildingSaveData> saved)
    {
        if (saved == null) return;

        var byId = new Dictionary<string, BuildingSaveData>();
        foreach (BuildingSaveData entry in saved)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.BuildingId))
            {
                byId[entry.BuildingId] = entry;
            }
        }

        foreach (BuildSlot slot in StoreRegistry.Slots)
        {
            if (byId.TryGetValue(slot.SlotId, out BuildingSaveData data) && data.IsUnlocked)
            {
                // Older saves have no level (0): treat that as level 1.
                slot.RestoreBuilt(Mathf.Max(1, data.CurrentLevel));
            }
        }
    }
}
