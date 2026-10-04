using System;
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
    /// Orders the customer to walk to a point and face the given rotation on arrival.
    /// 'onArrived' (optional) is called once when the customer has reached that point.
    /// A new MoveTo replaces the previous destination together with its pending callback.
    /// </summary>
    void MoveTo(Vector3 position, Quaternion rotation, Action onArrived = null);

    /// <summary>
    /// Called after the customer has paid. The customer finishes its goodbye (e.g. a wave)
    /// and MUST invoke 'onFinished' exactly once when done: only then does the register
    /// call the next customer. After that the customer leaves the store.
    /// </summary>
    void OnServed(Action onFinished);
}
