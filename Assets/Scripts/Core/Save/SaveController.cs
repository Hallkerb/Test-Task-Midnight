using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Lives in the game scene and drives saving and loading:
/// loads the save when the scene starts and saves periodically, after every purchase or upgrade,
/// when the app is paused (mobile) and when the app quits.
/// (Leaving to the main menu is saved by PauseMenu.)
/// Runs before all other scripts, so every system already sees the loaded values in its own Start.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    [SerializeField, Min(5f)] private float autosaveInterval = 30f;

    /// <summary>
    /// Cash earned while the player was away (0 if none).
    /// A "welcome back" popup can read it in its own Start.
    /// </summary>
    public double OfflineEarnings { get; private set; }

    private List<Slot> slots;   // a copy: the registry may already be cleared when this object is destroyed
    private float timer;

    public event Action OnLoaded;  // raised after the save is loaded and all slots are restored

    // Start runs after every Awake, so EconomyManager and all BuildSlots already exist.
    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        OfflineEarnings = SaveSystem.LoadGame();

        // Subscribe only after loading: restoring slots must not trigger a save.
        slots = new List<Slot>(StoreRegistry.StoreSlots);
        slots.AddRange(StoreRegistry.BuildingSlots);
        foreach (Slot slot in slots)
        {
            slot.OnBuilt += HandleSlotChanged;
            if (slot is BuildSlot buildSlot)
                buildSlot.OnUpgraded += HandleSlotChanged;
        }

        timer = autosaveInterval;

        OnLoaded?.Invoke();
    }

    private void OnDestroy()
    {
        if (slots == null) return;

        foreach (Slot slot in slots)
        {
            if (slot == null) continue;

            slot.OnBuilt -= HandleSlotChanged;
            if (slot is BuildSlot buildSlot)
                buildSlot.OnUpgraded -= HandleSlotChanged;
        }

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Unscaled time: autosave keeps working while the game is paused.
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;

        timer = autosaveInterval;
        SaveNow();
    }

    private void HandleSlotChanged(Slot slot) => SaveNow();

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    public void SaveNow() => SaveSystem.SaveGame();
}
