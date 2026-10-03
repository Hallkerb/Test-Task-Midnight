using UnityEngine;

/// <summary>
/// What a cash register needs from a customer.
/// The register depends on this interface, not on a concrete Customer class.
/// </summary>
public interface IShopCustomer
{
    /// <summary>Total price of everything the customer picked up.</summary>
    double BasketTotal { get; }

    /// <summary>
    /// True only when the customer has reached its current destination and stands still.
    /// Must be false while a path is still being calculated or walked.
    /// </summary>
    bool IsAtDestination { get; }

    /// <summary>Orders the customer to walk to a point and face the given rotation on arrival.</summary>
    void MoveTo(Vector3 position, Quaternion rotation);

    /// <summary>Called after the customer has paid. The customer should leave the store.</summary>
    void OnServed();
}
