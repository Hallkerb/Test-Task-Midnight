using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Keeps track of the things that currently exist in the store: shelves, checkouts and build slots.
/// Shelves and checkouts are handled through interfaces (IShelf, ICheckout), so new kinds of them
/// can be added without touching the customers.
/// Everything registers itself in OnEnable and unregisters in OnDisable.
/// </summary>
public static class StoreRegistry
{
    private static readonly List<IShelf> shelves = new List<IShelf>();
    private static readonly List<ICheckout> checkouts = new List<ICheckout>();
    private static readonly List<BuildSlot> buildingSlots = new List<BuildSlot>();
    private static readonly List<StoreSlot> storeSlots = new List<StoreSlot>();

    public static IReadOnlyList<IShelf> Shelves => shelves;
    public static IReadOnlyList<ICheckout> Checkouts => checkouts;

    /// <summary>All build slots of the scene (used by the save system).</summary>
    public static IReadOnlyList<BuildSlot> BuildingSlots => buildingSlots;
    public static IReadOnlyList<StoreSlot> StoreSlots => storeSlots;

    public static event Action OnAnyRegisteredChanged;

    /// <summary>
    /// Maximum number of customers allowed in the store at once:
    /// Min(total queue places of all checkouts, total pick places of all shelves).
    /// Buying only shelves or only registers does not raise it: both have to grow.
    /// </summary>
    public static int MaxCustomers
    {
        get
        {
            int queuePlaces = 0;
            foreach (ICheckout checkout in checkouts)
                queuePlaces += checkout.Capacity;

            int pickPlaces = 0;
            foreach (IShelf shelf in shelves)
                pickPlaces += shelf.SpotCount;

            return Mathf.Min(queuePlaces, pickPlaces);
        }
    }

    // Statics survive between Play sessions when domain reload is disabled, so clear them.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        shelves.Clear();
        checkouts.Clear();
        buildingSlots.Clear();
    }

    public static void Register(IShelf shelf)
    {
        if (!shelves.Contains(shelf)) 
        {
            shelves.Add(shelf);
            OnAnyRegisteredChanged?.Invoke();
        }
    }

    public static void Unregister(IShelf shelf)
    {
        shelves.Remove(shelf);
        OnAnyRegisteredChanged?.Invoke();
    }

    public static void Register(ICheckout checkout)
    {
        if (!checkouts.Contains(checkout)) 
        {
            checkouts.Add(checkout);
            OnAnyRegisteredChanged?.Invoke();
        }
    }

    public static void Unregister(ICheckout checkout)
    {
        checkouts.Remove(checkout);
        OnAnyRegisteredChanged?.Invoke();
    }

    public static void Register(BuildSlot slot)
    {
        if (!buildingSlots.Contains(slot)) 
        {
            buildingSlots.Add(slot);
            OnAnyRegisteredChanged?.Invoke();
        }
    }

    public static void Unregister(BuildSlot slot)
    {
        buildingSlots.Remove(slot);
        OnAnyRegisteredChanged?.Invoke();
    }

    public static void Register(StoreSlot slot)
    {
        if (!storeSlots.Contains(slot)) 
        {
            storeSlots.Add(slot);
            OnAnyRegisteredChanged?.Invoke();
        }
    }

    public static void Unregister(StoreSlot slot)
    {
        storeSlots.Remove(slot);
        OnAnyRegisteredChanged?.Invoke();
    }

    /// <summary>Returns the checkout with the shortest queue that still has a free spot (or null).</summary>
    public static ICheckout FindBestCheckout()
    {
        ICheckout best = null;

        foreach (ICheckout checkout in checkouts)
        {
            if (!checkout.HasFreeSpot) continue;
            if (best == null || checkout.QueueCount < best.QueueCount)
                best = checkout;
        }

        return best;
    }
}
