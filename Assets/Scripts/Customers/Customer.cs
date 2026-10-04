using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>Current activity of a customer.</summary>
public enum CustomerState
{
    Idle,             // waiting for Initialize()
    ChoosingShelf,    // looking for a shelf with a free pick point
    WalkingToShelf,
    Picking,          // standing at the shelf and taking the product
    ChoosingCheckout, // looking for a checkout with a free queue place
    InQueue,          // the checkout drives us (MoveTo / OnServed)
    Paid,             // paid, standing at the register and waving goodbye
    Leaving           // walking to the exit
}

/// <summary>
/// A store customer. Flow:
/// ChoosingShelf -> WalkingToShelf -> Picking -> (repeat for every wanted item)
/// -> ChoosingCheckout -> InQueue -> (paid) -> Leaving -> removed.
///
/// Customers deliberately walk through each other (obstacle avoidance is off).
/// The customer only knows the IShelf / ICheckout interfaces, never concrete classes.
/// Customers are pooled: the spawner calls Initialize() at the start of every visit
/// (which resets all state) and takes the customer back when OnLeftStore is raised.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour, IShopCustomer
{
    [Header("Shopping")]
    [Tooltip("How many different shelves a customer visits (min, max inclusive).")]
    [SerializeField] private Vector2Int itemsToBuy = new Vector2Int(1, 3);
    [Tooltip("How often (seconds) to look for a free shelf / checkout while waiting.")]
    [SerializeField, Min(0.1f)] private float retryInterval = 0.5f;
    [Tooltip("Give up on an item if no shelf is free for this long (seconds).")]
    [SerializeField, Min(0f)] private float maxWaitForShelf = 5f;
    [Tooltip("Leave without paying if every queue is full for this long (seconds).")]
    [SerializeField, Min(0f)] private float maxWaitForCheckout = 10f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float turnSpeed = 360f;
    [Tooltip("Random walking speed difference so customers don't move like clones.")]
    [SerializeField, Range(0f, 0.3f)] private float speedVariation = 0.1f;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [Tooltip("Float parameter that receives the agent speed.")]
    [SerializeField] private string speedParameter = "Speed";
    [Tooltip("Trigger parameter played after paying.")]
    [SerializeField] private string waveTrigger = "Wave";
    [Tooltip("Fallback wave duration (seconds), used only if the Wave clip length can't be found automatically.")]
    [SerializeField, Min(0f)] private float paidDelay = 1.2f;

    /// <summary>Raised when the customer has reached the exit. The owner (spawner) returns it to the pool.</summary>
    public event Action<Customer> OnLeftStore;

    private NavMeshAgent agent;
    private CustomerState state = CustomerState.Idle;

    private Vector3 exitPosition;
    private int itemsLeft;
    private double basketTotal;
    private readonly HashSet<IShelf> visitedShelves = new HashSet<IShelf>();

    private IShelf currentShelf;
    private Transform currentPoint;

    private float baseSpeed;
    private float pickTimer;
    private float paidTimer;
    private float paidDuration;      // how long the goodbye wave lasts
    private Action servedCallback; // tells the register that we are done
    private float retryTimer;
    private float waited;
    private Quaternion? facing;   // rotation to take once the destination is reached

    private int speedHash = -1;
    private int waveHash = -1;

    public CustomerState State => state;
    public double BasketTotal => basketTotal;

    // ------------------------------------------------------------------
    // IShopCustomer
    // ------------------------------------------------------------------
    public bool IsAtDestination
    {
        get
        {
            if (agent.pathPending) return false;
            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
    }

    public void MoveTo(Vector3 position, Quaternion rotation) => GoTo(position, rotation);

    public void OnServed(Action onFinished)
    {
        servedCallback = onFinished;

        // Wave goodbye on the spot: the register is released only when the wave is over.
        if (animator != null && waveHash != -1 && paidDuration > 0f)
        {
            animator.SetTrigger(waveHash);
            paidTimer = paidDuration;
            SetState(CustomerState.Paid);
            return;
        }

        // No wave animation: the service ends immediately.
        ReportServiceFinished();
        StartLeaving();
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Customers are allowed to pass through each other.
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        baseSpeed = agent.speed;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        speedHash = FindParameter(speedParameter, AnimatorControllerParameterType.Float);
        waveHash = FindParameter(waveTrigger, AnimatorControllerParameterType.Trigger);
        paidDuration = FindWaveDuration();
    }

    /// <summary>
    /// Prepares the customer for a new visit: places it on the NavMesh, resets everything
    /// left over from the previous visit (customers are pooled) and starts shopping.
    /// The GameObject must already be active.
    /// </summary>
    public void Initialize(Vector3 spawnPosition, Quaternion spawnRotation, Vector3 exit)
    {
        // Reset state from the previous visit.
        visitedShelves.Clear();
        basketTotal = 0;
        currentShelf = null;
        currentPoint = null;
        servedCallback = null;
        facing = null;
        pickTimer = 0f;
        paidTimer = 0f;

        // A fresh random walking speed for every visit.
        agent.speed = baseSpeed * (1f + Random.Range(-speedVariation, speedVariation));

        if (!agent.Warp(spawnPosition))
            Debug.LogWarning($"{name}: the spawn point is not on the NavMesh.", this);
        agent.ResetPath();
        transform.rotation = spawnRotation;

        if (animator != null)
        {
            if (waveHash != -1) animator.ResetTrigger(waveHash);
            if (speedHash != -1) animator.SetFloat(speedHash, 0f);
        }

        exitPosition = exit;
        itemsLeft = Random.Range(itemsToBuy.x, itemsToBuy.y + 1);
        SetState(CustomerState.ChoosingShelf);
    }

    private void Update()
    {
        switch (state)
        {
            case CustomerState.ChoosingShelf:
                UpdateChoosingShelf();
                break;

            case CustomerState.WalkingToShelf:
                if (IsAtDestination) BeginPicking();
                break;

            case CustomerState.Picking:
                UpdatePicking();
                break;

            case CustomerState.ChoosingCheckout:
                UpdateChoosingCheckout();
                break;

            case CustomerState.Paid:
                UpdatePaid();
                break;

            case CustomerState.Leaving:
                if (IsAtDestination) LeaveStore();
                break;
        }

        FaceTarget();
        UpdateAnimator();
    }

    // ------------------------------------------------------------------
    // States
    // ------------------------------------------------------------------
    private void UpdateChoosingShelf()
    {
        if (itemsLeft <= 0)
        {
            SetState(CustomerState.ChoosingCheckout);
            return;
        }

        waited += Time.deltaTime;
        retryTimer -= Time.deltaTime;
        if (retryTimer > 0f) return;
        retryTimer = retryInterval;

        if (TryReserveShelf())
        {
            SetState(CustomerState.WalkingToShelf);
            return;
        }

        if (!HasUnvisitedShelf())
        {
            itemsLeft = 0;          // nothing left to visit
        }
        else if (waited >= maxWaitForShelf)
        {
            itemsLeft--;            // give up on this item
            waited = 0f;
        }
    }

    private void BeginPicking()
    {
        pickTimer = currentShelf.PickDuration;
        SetState(CustomerState.Picking);
    }

    private void UpdatePicking()
    {
        pickTimer -= Time.deltaTime;
        if (pickTimer > 0f) return;

        basketTotal += currentShelf.Price;
        currentShelf.Release(currentPoint);
        currentShelf = null;
        currentPoint = null;

        itemsLeft--;
        SetState(CustomerState.ChoosingShelf);
    }

    private void UpdateChoosingCheckout()
    {
        // Nothing in the basket: nothing to pay for.
        if (basketTotal <= 0)
        {
            StartLeaving();
            return;
        }

        waited += Time.deltaTime;
        retryTimer -= Time.deltaTime;
        if (retryTimer > 0f) return;
        retryTimer = retryInterval;

        ICheckout checkout = StoreRegistry.FindBestCheckout();
        if (checkout != null && checkout.TryJoin(this))
        {
            SetState(CustomerState.InQueue);   // the checkout now sends us to our queue point
            return;
        }

        if (waited >= maxWaitForCheckout)      // every queue stayed full: leave without buying
        {
            StartLeaving();
        }
    }

    private void UpdatePaid()
    {
        paidTimer -= Time.deltaTime;
        if (paidTimer <= 0f)
        {
            ReportServiceFinished();   // the register can now call the next customer
            StartLeaving();
        }
    }

    private void ReportServiceFinished()
    {
        Action callback = servedCallback;
        servedCallback = null;
        callback?.Invoke();
    }

    private void StartLeaving()
    {
        GoTo(exitPosition, null);
        SetState(CustomerState.Leaving);
    }

    private void LeaveStore()
    {
        SetState(CustomerState.Idle);

        // Pooled customers are handed back to their owner; without an owner, just remove it.
        if (OnLeftStore != null) OnLeftStore.Invoke(this);
        else Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private bool TryReserveShelf()
    {
        IShelf chosen = null;
        int candidates = 0;

        foreach (IShelf shelf in StoreRegistry.Shelves)
        {
            if (visitedShelves.Contains(shelf) || !shelf.HasFreeSpot) continue;

            // Reservoir sampling: a uniformly random pick without building a temporary list.
            candidates++;
            if (Random.Range(0, candidates) == 0) chosen = shelf;
        }

        if (chosen == null || !chosen.TryReserve(transform.position, out Transform point))
            return false;

        currentShelf = chosen;
        currentPoint = point;
        visitedShelves.Add(chosen);

        GoTo(point.position, point.rotation);
        return true;
    }

    private bool HasUnvisitedShelf()
    {
        foreach (IShelf shelf in StoreRegistry.Shelves)
            if (!visitedShelves.Contains(shelf)) return true;

        return false;
    }

    private void SetState(CustomerState newState)
    {
        state = newState;
        waited = 0f;
        retryTimer = 0f;
    }

    private void GoTo(Vector3 position, Quaternion? rotation)
    {
        facing = rotation;
        agent.SetDestination(position);
    }

    /// <summary>Once the destination is reached, smoothly turn to the requested rotation.</summary>
    private void FaceTarget()
    {
        if (facing == null || !IsAtDestination) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, facing.Value, turnSpeed * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null || speedHash == -1) return;
        animator.SetFloat(speedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// Length of the wave clip, found by name in the controller (e.g. "normal wave").
    /// Falls back to paidDelay if no such clip exists. Returns 0 if there is no Wave trigger at all.
    /// </summary>
    private float FindWaveDuration()
    {
        if (animator == null || waveHash == -1) return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.IndexOf(waveTrigger, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip.length;

        return paidDelay;
    }

    /// <summary>Returns the parameter hash, or -1 if the controller doesn't have such a parameter.</summary>
    private int FindParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(parameterName))
            return -1;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == type)
                return parameter.nameHash;

        return -1;
    }
}
