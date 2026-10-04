using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A cash register with a queue. queuePoints[0] is the service spot right at the register,
/// the following points go backwards: the customer at queue[i] stands at queuePoints[i].
/// The customer at the head is served for 'serviceTime' seconds, then pays via
/// EconomyManager.RegisterSale(). The queue moves up only after the served customer has
/// finished its goodbye and reported back, so the next one never walks into it.
/// Purchasing and upgrading are handled by BuildSlot, not by the register itself.
/// </summary>
[DisallowMultipleComponent]
public class CashRegister : MonoBehaviour, IBuildable, ICheckout
{
    [Header("Queue")]
    [Tooltip("Point 0 = service spot at the register, then the queue going backwards. Rotate them to face the register.")]
    [SerializeField] private Transform[] queuePoints;
    [Tooltip("How many of the points are in use. Reserved for future upgrades (longer queue).")]
    [SerializeField, Min(1)] private int activeQueueLength = 3;

    [Header("Service")]
    [Tooltip("Seconds needed to serve one customer.")]
    [SerializeField, Min(0.1f)] private float serviceTime = 2f;

    /// <summary>A sale was completed: (this register, amount actually paid). Use it for +$ popups.</summary>
    public event Action<CashRegister, double> OnSaleCompleted;

    private readonly List<IShopCustomer> queue = new List<IShopCustomer>();
    private float serviceTimer;
    private bool awaitingRelease;   // paid, but the head is still finishing its goodbye

    public BuildSlot Slot { get; private set; }

    /// <summary>Maximum number of customers (including the one being served).</summary>
    public int Capacity => queuePoints == null ? 0 : Mathf.Min(activeQueueLength, queuePoints.Length);

    public int QueueCount => queue.Count;
    public bool HasFreeSpot => queue.Count < Capacity;

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    public void OnBuilt(BuildSlot slot) => Slot = slot;

    /// <summary>Puts the customer at the end of the queue. Returns false if the queue is full.</summary>
    public bool TryJoin(IShopCustomer customer)
    {
        if (customer == null || !HasFreeSpot || queue.Contains(customer))
            return false;

        queue.Add(customer);
        SendToPoint(queue.Count - 1);
        return true;
    }

    /// <summary>Removes a customer from the queue (e.g. ran out of patience). The rest move up.</summary>
    public void Leave(IShopCustomer customer)
    {
        int index = queue.IndexOf(customer);
        if (index < 0) return;

        queue.RemoveAt(index);
        if (index == 0)
        {
            serviceTimer = 0f;
            awaitingRelease = false;
        }

        ShiftQueue(index);
    }

    private void Update()
    {
        if (queue.Count == 0 || awaitingRelease) return;

        IShopCustomer head = queue[0];

        // Service starts only when the customer has actually reached the service spot.
        if (!head.IsAtDestination) return;

        serviceTimer += Time.deltaTime;
        if (serviceTimer >= serviceTime)
            CompleteService(head);
    }

    private void CompleteService(IShopCustomer customer)
    {
        serviceTimer = 0f;
        awaitingRelease = true;

        double paid = 0;
        if (EconomyManager.Instance != null)
            paid = EconomyManager.Instance.RegisterSale(customer.BasketTotal);

        OnSaleCompleted?.Invoke(this, paid);

        // The customer stays at the register (still counts as part of the queue)
        // until it finishes its goodbye and calls us back.
        customer.OnServed(() => ReleaseHead(customer));
    }

    private void ReleaseHead(IShopCustomer customer)
    {
        // Ignore a late callback if the customer was already removed (e.g. via Leave).
        if (queue.Count == 0 || queue[0] != customer) return;

        queue.RemoveAt(0);
        awaitingRelease = false;
        ShiftQueue(0);
    }

    /// <summary>Sends every customer from 'fromIndex' to the point matching their new position.</summary>
    private void ShiftQueue(int fromIndex)
    {
        for (int i = fromIndex; i < queue.Count; i++)
            SendToPoint(i);
    }

    private void SendToPoint(int index)
    {
        Transform point = queuePoints[index];
        queue[index].MoveTo(point.position, point.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        if (queuePoints == null) return;

        for (int i = 0; i < queuePoints.Length; i++)
        {
            if (queuePoints[i] == null) continue;

            Gizmos.color = i == 0 ? Color.green : Color.yellow;
            Gizmos.DrawSphere(queuePoints[i].position, 0.15f);
            Gizmos.DrawRay(queuePoints[i].position, queuePoints[i].forward * 0.4f);
        }
    }
}
