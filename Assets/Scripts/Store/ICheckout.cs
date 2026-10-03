/// <summary>
/// What a customer needs from anything where they can pay (cash register, self-checkout, ...).
/// </summary>
public interface ICheckout
{
    /// <summary>Maximum number of customers in the queue (including the one being served).</summary>
    int Capacity { get; }

    int QueueCount { get; }
    bool HasFreeSpot { get; }

    /// <summary>Puts the customer at the end of the queue. Returns false if the queue is full.</summary>
    bool TryJoin(IShopCustomer customer);

    /// <summary>Removes the customer from the queue (e.g. ran out of patience).</summary>
    void Leave(IShopCustomer customer);
}
