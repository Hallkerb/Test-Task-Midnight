using UnityEngine;

/// <summary>
/// What a customer needs from anything that sells a product (shelf, fruit stand, ...).
/// </summary>
public interface IShelf
{
    ProductData Product { get; }

    /// <summary>Price a customer pays for one item.</summary>
    double Price { get; }

    /// <summary>Seconds the customer has to stand at the pick point.</summary>
    float PickDuration { get; }

    /// <summary>Total number of pick points (how many customers can use the shelf at once).</summary>
    int SpotCount { get; }

    bool HasFreeSpot { get; }

    /// <summary>Reserves the free pick point closest to 'from'. Release it when done.</summary>
    bool TryReserve(Vector3 from, out Transform point);

    void Release(Transform point);
}
