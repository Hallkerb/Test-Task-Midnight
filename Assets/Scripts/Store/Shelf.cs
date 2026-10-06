using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A shelf with one product. Customers reserve a pick point, stand there for PickDuration
/// seconds and then take the product (no stock: the shelf never runs out).
/// The number of pick points is the number of customers that can use the shelf at once.
/// Purchasing and upgrading are handled by BuildSlot, not by the shelf itself.
/// </summary>
[DisallowMultipleComponent]
public class Shelf : MonoBehaviour, IBuildable, IShelf, IUpgradable
{
    [Header("Product")]
    [SerializeField] private ProductData product;

    [Header("Pick points")]
    [Tooltip("Spots where customers stand (one per side of the shelf). Rotate them to face the shelf.")]
    [SerializeField] private Transform[] pickPoints;

    [Header("Stats")]
    [SerializeField, Min(0.1f)] private float pickDuration = 1.5f;
    [Tooltip("Multiplies the product price. Overwritten by the upgrade level when the shelf is built through a slot.")]
    [SerializeField, Min(0.1f)] private float priceMultiplier = 1f;

    private bool[] occupied;
    private ShelfBuildingData data;   // product and level table, set in OnBuilt
    private int level = 1;

    public BuildSlot Slot { get; private set; }
    public ProductData Product => product;
    public float PickDuration => pickDuration;

    public int SpotCount => occupied != null ? occupied.Length : 0;

    /// <summary>Price a customer pays for one item from this shelf.</summary>
    public double Price => product != null ? product.BasePrice * priceMultiplier : 0;

    public bool HasFreeSpot
    {
        get
        {
            for (int i = 0; i < occupied.Length; i++)
                if (!occupied[i]) return true;
            return false;
        }
    }

    private void Awake()
    {
        if (pickPoints == null) pickPoints = new Transform[0];
        occupied = new bool[pickPoints.Length];
    }

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    public void OnBuilt(BuildSlot slot)
    {
        Slot = slot;

        // The product and the level table come from the slot's data, so one shelf prefab can sell anything.
        data = slot.Building as ShelfBuildingData;
        if (data != null && data.Product != null)
            product = data.Product;
    }

    // ------------------------------------------------------------------
    // IUpgradable
    // ------------------------------------------------------------------
    public int Level => level;
    public int MaxLevel => data != null && data.Levels.Count > 0 ? data.Levels.Count : 1;

    /// <summary>Price to reach the next level (the table entry of that level).</summary>
    public double NextUpgradePrice => level < MaxLevel ? data.Levels[level].costToReach : 0;

    public void ApplyLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, MaxLevel);
        if (data == null || data.Levels.Count == 0) return;

        ShelfLevel stats = data.Levels[level - 1];
        priceMultiplier = stats.priceMultiplier;
        pickDuration = stats.pickDuration;
    }

    public IReadOnlyList<UpgradeStat> GetStats()
    {
        if (data == null || data.Levels.Count == 0)
            return Array.Empty<UpgradeStat>();

        ShelfLevel now = data.Levels[level - 1];
        bool hasNext = level < data.Levels.Count;
        ShelfLevel next = hasNext ? data.Levels[level] : now;

        double basePrice = product != null ? product.BasePrice : 0;

        return new[]
        {
            new UpgradeStat("Item price",
                "$" + UpgradeStat.Number(basePrice * now.priceMultiplier),
                hasNext ? "$" + UpgradeStat.Number(basePrice * next.priceMultiplier) : null),

            new UpgradeStat("Pick time",
                UpgradeStat.Number(now.pickDuration) + " s",
                hasNext ? UpgradeStat.Number(next.pickDuration) + " s" : null)
        };
    }

    /// <summary>
    /// Reserves the free pick point closest to 'from'.
    /// The customer MUST call Release() with the same point when done (or when leaving).
    /// </summary>
    public bool TryReserve(Vector3 from, out Transform point)
    {
        point = null;
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < pickPoints.Length; i++)
        {
            if (occupied[i] || pickPoints[i] == null) continue;

            float distance = (pickPoints[i].position - from).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return false;

        occupied[bestIndex] = true;
        point = pickPoints[bestIndex];
        return true;
    }

    public void Release(Transform point)
    {
        int index = Array.IndexOf(pickPoints, point);
        if (index >= 0) occupied[index] = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (pickPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform p in pickPoints)
        {
            if (p == null) continue;
            Gizmos.DrawSphere(p.position, 0.15f);
            Gizmos.DrawRay(p.position, p.forward * 0.4f);
        }
    }
}
