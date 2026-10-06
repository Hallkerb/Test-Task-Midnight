using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField, Min(5f)] private float autosaveInterval = 30f;

    /// <summary>
    /// Cash earned while the player was away (0 if none).
    /// A "welcome back" popup can read it in its own Start.
    /// </summary>
    public double OfflineEarnings { get; private set; }

    private List<BuildSlot> slots;   // a copy: the registry may already be cleared when this object is destroyed
    private float timer;

    // Start runs after every Awake, so EconomyManager and all BuildSlots already exist.
    private void Start()
    {
        OfflineEarnings = SaveSystem.LoadGame();

        // Subscribe only after loading: restoring slots must not trigger a save.
        slots = new List<BuildSlot>(StoreRegistry.Slots);
        foreach (BuildSlot slot in slots)
        {
            slot.OnBuilt += HandleSlotChanged;
            slot.OnUpgraded += HandleSlotChanged;
        }

        timer = autosaveInterval;
    }

    private void OnDestroy()
    {
        if (slots == null) return;

        foreach (BuildSlot slot in slots)
        {
            if (slot == null) continue;

            slot.OnBuilt -= HandleSlotChanged;
            slot.OnUpgraded -= HandleSlotChanged;
        }
    }

    private void Update()
    {
        // Unscaled time: autosave keeps working while the game is paused.
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;

        timer = autosaveInterval;
        SaveNow();
    }

    private void HandleSlotChanged(BuildSlot slot) => SaveNow();

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    public void SaveNow() => SaveSystem.SaveGame();
}
