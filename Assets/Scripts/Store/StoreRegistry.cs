using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of the shelves and checkouts that currently exist in the store.
/// Works with interfaces (IShelf, ICheckout), so new kinds of shelves or registers
/// can be added without touching the customers.
/// Objects register themselves in OnEnable and unregister in OnDisable.
/// </summary>
public static class StoreRegistry
{
    private static readonly List<IShelf> shelves = new List<IShelf>();
    private static readonly List<ICheckout> checkouts = new List<ICheckout>();

    public static IReadOnlyList<IShelf> Shelves => shelves;
    public static IReadOnlyList<ICheckout> Checkouts => checkouts;

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
    }

    public static void Register(IShelf shelf)
    {
        if (!shelves.Contains(shelf)) shelves.Add(shelf);
    }

    public static void Unregister(IShelf shelf) => shelves.Remove(shelf);

    public static void Register(ICheckout checkout)
    {
        if (!checkouts.Contains(checkout)) checkouts.Add(checkout);
    }

    public static void Unregister(ICheckout checkout) => checkouts.Remove(checkout);

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
