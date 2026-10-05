using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Lifecycle of a build slot.</summary>
public enum SlotState
{
    Locked,     // the required slot is not built yet
    Available,  // can be bought
    Built       // already bought, the building exists
}

/// <summary>
/// Universal purchasable slot. The same prefab is used for every register, shelf, etc.:
/// what gets built is defined by the assigned BuildingData.
/// Slots form chains via 'requiredSlot' (register 3 requires register 2, and so on).
///
/// Clicking requires: a Collider on this object, an EventSystem in the scene
/// and a PhysicsRaycaster on the camera.
/// </summary>
[DisallowMultipleComponent]
public class BuildSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Data")]
    [Tooltip("Unique id, used by the save system. If empty, the object name is used.")]
    [SerializeField] private string slotId;
    [SerializeField] private BuildingData building;
    [Tooltip("This slot must be built first. Leave empty for the first slot of a chain.")]
    [SerializeField] private BuildSlot requiredSlot;

    [Header("Scene references")]
    [Tooltip("Where the building is spawned. If empty, this object's transform is used.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Shown while the slot is Locked (silhouette, lock icon).")]
    [SerializeField] private GameObject lockedVisual;
    [Tooltip("Shown while the slot is Available (price tag, highlight).")]
    [SerializeField] private GameObject availableVisual;
    [Tooltip("Shown in every state until the slot is built (e.g. a barrier). Hidden once Built.")]
    [SerializeField] private GameObject[] showUntilBuilt;

    /// <summary>Raised once the building has been spawned.</summary>
    public event Action<BuildSlot> OnBuilt;

    /// <summary>Raised on every state change (UI can update price tags, locks, etc.).</summary>
    public event Action<BuildSlot> OnStateChanged;

    public string SlotId => string.IsNullOrEmpty(slotId) ? name : slotId;
    public BuildingData Building => building;
    public BuildSlot RequiredSlot => requiredSlot;
    public SlotState State { get; private set; } = SlotState.Locked;

    /// <summary>The spawned building (null until built).</summary>
    public GameObject BuiltInstance { get; private set; }

    private bool IsUnlocked => requiredSlot == null || requiredSlot.State == SlotState.Built;

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void Start()
    {
        if (requiredSlot != null)
            requiredSlot.OnBuilt += HandleRequiredBuilt;

        // The save system may have already restored this slot before Start.
        if (State != SlotState.Built)
            SetState(IsUnlocked ? SlotState.Available : SlotState.Locked);
    }

    private void OnDestroy()
    {
        if (requiredSlot != null)
            requiredSlot.OnBuilt -= HandleRequiredBuilt;
    }

    /// <summary>Tries to buy the building. Returns true on success.</summary>
    public bool TryBuy()
    {
        if (State != SlotState.Available || building == null || building.Prefab == null)
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || !economy.TrySpend(CurrencyType.Cash, building.Price))
            return false;

        Construct();
        return true;
    }

    /// <summary>Called by the save system: builds the slot without charging money.</summary>
    public void RestoreBuilt()
    {
        if (State != SlotState.Built && building != null && building.Prefab != null)
            Construct();
    }

    public void OnPointerClick(PointerEventData eventData) => TryBuy();

    private void Construct()
    {
        BuiltInstance = Instantiate(building.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        foreach (IBuildable buildable in BuiltInstance.GetComponentsInChildren<IBuildable>())
            buildable.OnBuilt(this);

        SetState(SlotState.Built);
        OnBuilt?.Invoke(this);
    }

    private void HandleRequiredBuilt(BuildSlot _)
    {
        if (State == SlotState.Locked)
            SetState(SlotState.Available);
    }

    private void SetState(SlotState newState)
    {
        State = newState;

        if (lockedVisual != null) lockedVisual.SetActive(newState == SlotState.Locked);
        if (availableVisual != null) availableVisual.SetActive(newState == SlotState.Available);

        if (showUntilBuilt != null)
        {
            foreach (GameObject go in showUntilBuilt)
                if (go != null) go.SetActive(newState != SlotState.Built);
        }

        OnStateChanged?.Invoke(this);
    }
}
