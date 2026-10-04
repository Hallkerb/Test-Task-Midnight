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

    // Customers that joined but are still walking to the end of the lane.
    private readonly HashSet<IShopCustomer> approaching = new HashSet<IShopCustomer>();

    private bool headReady;         // the head of the queue stands at the service spot

    public BuildSlot Slot { get; private set; }

    /// <summary>Maximum number of customers (including the one being served).</summary>
    public int Capacity => queuePoints == null ? 0 : Mathf.Min(activeQueueLength, queuePoints.Length);

    public int QueueCount => queue.Count;
    public bool HasFreeSpot => queue.Count < Capacity;

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    public void OnBuilt(BuildSlot slot) => Slot = slot;

    /// <summary>
    /// Puts the customer at the end of the queue. Returns false if the queue is full.
    /// The customer first walks to the end of the lane and only then along it to its place,
    /// so it never cuts in from the side or from the front.
    /// </summary>
    public bool TryJoin(IShopCustomer customer)
    {
        if (customer == null || !HasFreeSpot || queue.Contains(customer))
            return false;

        queue.Add(customer);
        approaching.Add(customer);

        Transform laneEnd = queuePoints[Capacity - 1];
        customer.MoveTo(laneEnd.position, laneEnd.rotation, () => OnReachedLaneEnd(customer));
        return true;
    }

    /// <summary>Removes a customer from the queue (e.g. ran out of patience). The rest move up.</summary>
    public void Leave(IShopCustomer customer)
    {
        int index = queue.IndexOf(customer);
        if (index < 0) return;

        queue.RemoveAt(index);
        approaching.Remove(customer);
        if (index == 0)
        {
            serviceTimer = 0f;
            awaitingRelease = false;
            headReady = false;
        }

        ShiftQueue(index);
    }

    private void Update()
    {
        // The timer runs only while the head stands at the service spot.
        if (!headReady || awaitingRelease) return;

        serviceTimer += Time.deltaTime;
        if (serviceTimer >= serviceTime)
            CompleteService(queue[0]);
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
        headReady = false;
        ShiftQueue(0);
    }

    /// <summary>Sends every customer from 'fromIndex' to the point matching their new position.</summary>
    private void ShiftQueue(int fromIndex)
    {
        for (int i = fromIndex; i < queue.Count; i++)
            SendToPoint(i);
    }

    /// <summary>The customer reached the end of the lane: now it walks along it to its own place.</summary>
    private void OnReachedLaneEnd(IShopCustomer customer)
    {
        approaching.Remove(customer);

        int index = queue.IndexOf(customer);
        if (index >= 0) SendToPoint(index);
    }

    /// <summary>The customer reached its queue point. If it is the head, service can start.</summary>
    private void OnReachedPoint(IShopCustomer customer)
    {
        if (queue.Count > 0 && queue[0] == customer)
            headReady = true;
    }

    private void SendToPoint(int index)
    {
        IShopCustomer customer = queue[index];

        // Customers still walking to the end of the lane get their real point on arrival.
        if (approaching.Contains(customer)) return;

        // Only the head needs to be notified: its arrival starts the service.
        Action onArrived = null;
        if (index == 0) onArrived = () => OnReachedPoint(customer);

        Transform point = queuePoints[index];
        customer.MoveTo(point.position, point.rotation, onArrived);
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
