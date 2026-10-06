using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System;

/// <summary>
/// Spawns customers while the store has room for them and reuses finished customers
/// through an object pool (UnityEngine.Pool.ObjectPool) instead of creating new ones.
///
/// The visitor limit comes from StoreRegistry.MaxCustomers (Min of queue places and pick places),
/// multiplied by 'capacityFactor' for tuning.
/// Both the spawn point and the exit point must lie on the NavMesh.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Customer prefabs (different models). A random one is used whenever the pool needs a new customer.")]
    [SerializeField] private Customer[] customerPrefabs;
    [Tooltip("Parent for spawned customers (the 'Units' group). If empty, this object is used.")]
    [SerializeField] private Transform customersRoot;

    [Header("Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Spawning")]
    [Tooltip("Random time between spawn attempts, in seconds (min, max).")]
    [SerializeField] private Vector2 spawnInterval = new Vector2(2f, 4f);
    [Tooltip("Multiplies StoreRegistry.MaxCustomers. Raise it if the store looks empty.")]
    [SerializeField, Min(0f)] private float capacityFactor = 1f;

    [Header("Pool")]
    [Tooltip("Customers created in advance, to avoid hiccups during the game.")]
    [SerializeField, Min(0)] private int prewarmCount = 5;
    [SerializeField, Min(1)] private int maxPoolSize = 30;

    public ObjectPool<Customer> pool { get; private set; }
    private float timer;

    /// <summary>Customers currently in the store (for the HUD: "Visitors 5/8").</summary>
    public int ActiveCount => pool != null ? pool.CountActive : 0;

    /// <summary>Current visitor limit.</summary>
    public int Limit => Mathf.FloorToInt(StoreRegistry.MaxCustomers * capacityFactor);

    public event Action OnCustomersChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (customerPrefabs == null || customerPrefabs.Length == 0 || spawnPoint == null || exitPoint == null)
        {
            Debug.LogError($"{name}: assign customer prefabs, a spawn point and an exit point.", this);
            enabled = false;
            return;
        }

        if (customersRoot == null) customersRoot = transform;

        pool = new ObjectPool<Customer>(
            createFunc: CreateCustomer,
            actionOnGet: customer => HandleOnGet(customer),
            actionOnRelease: customer => HandleOnRelease(customer),
            actionOnDestroy: DestroyCustomer,
            collectionCheck: true,      // catches a customer being released twice
            defaultCapacity: 10,
            maxSize: maxPoolSize);

        Prewarm();
    }

    private void Start()
    {
        timer = UnityEngine.Random.Range(spawnInterval.x, spawnInterval.y);
    }

    private void OnDestroy()
    {
        pool?.Dispose();

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = UnityEngine.Random.Range(spawnInterval.x, spawnInterval.y);

        if (ActiveCount < Limit)
            Spawn();
    }

    private void Spawn()
    {
        Customer customer = pool.Get();
        customer.Initialize(spawnPoint.position, spawnPoint.rotation, exitPoint.position);
    }

    [ContextMenu("Prewarm")]
    private void Prewarm()
    {
        var created = new List<Customer>(prewarmCount);

        for (int i = 0; i < prewarmCount; i++)
            created.Add(pool.Get());

        foreach (Customer customer in created)
            pool.Release(customer);
    }

    // ------------------------------------------------------------------
    // Pool callbacks
    // ------------------------------------------------------------------
    private void HandleOnGet(Customer customer)
    {
        customer.gameObject.SetActive(true);
        
        OnCustomersChanged?.Invoke();
    }

    private void HandleOnRelease(Customer customer)
    {
        customer.gameObject.SetActive(false);

        OnCustomersChanged?.Invoke();
    }
    
    private Customer CreateCustomer()
    {
        Customer prefab = customerPrefabs[UnityEngine.Random.Range(0, customerPrefabs.Length)];

        // Created at the spawn point so that its NavMeshAgent starts on the NavMesh.
        Customer customer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, customersRoot);
        customer.OnLeftStore += HandleCustomerLeft;
        return customer;
    }

    private void DestroyCustomer(Customer customer)
    {
        customer.OnLeftStore -= HandleCustomerLeft;
        Destroy(customer.gameObject);
    }

    private void HandleCustomerLeft(Customer customer) => pool.Release(customer);
}
