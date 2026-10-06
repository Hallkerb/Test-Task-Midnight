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
/// After the purchase the slot also owns the building's upgrade level: it takes the money,
/// the building (IUpgradable) applies the level to itself.
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

    /// <summary>Raised after every successful upgrade.</summary>
    public event Action<BuildSlot> OnUpgraded;

    /// <summary>Raised when the player clicks a built slot whose building can be upgraded (opens the upgrade panel).</summary>
    public event Action<BuildSlot> OnSelected;

    /// <summary>Raised on every state change (UI can update price tags, locks, etc.).</summary>
    public event Action<BuildSlot> OnStateChanged;

    /// <summary>Raised when the slot becomes selected or deselected (the upgrade panel is open for it).</summary>
    public event Action<BuildSlot> OnSelectionChanged;

    public string SlotId => string.IsNullOrEmpty(slotId) ? name : slotId;
    public BuildingData Building => building;
    public BuildSlot RequiredSlot => requiredSlot;
    public SlotState State { get; private set; } = SlotState.Locked;

    /// <summary>The spawned building (null until built).</summary>
    public GameObject BuiltInstance { get; private set; }

    /// <summary>The upgradable part of the building (null if it cannot be upgraded).</summary>
    public IUpgradable Upgradable { get; private set; }

    /// <summary>Current upgrade level: 0 until built, 1 right after the purchase.</summary>
    public int Level { get; private set; }

    /// <summary>True while the upgrade panel is open for this slot.</summary>
    public bool IsSelected { get; private set; }

    public bool CanUpgrade => State == SlotState.Built && Upgradable != null && Level < Upgradable.MaxLevel;
    public double NextUpgradePrice => Upgradable != null ? Upgradable.NextUpgradePrice : 0;

    private bool IsUnlocked => requiredSlot == null || requiredSlot.State == SlotState.Built;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

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

        Construct(1);
        return true;
    }

    /// <summary>Tries to upgrade the building by one level. Returns true on success.</summary>
    public bool TryUpgrade()
    {
        if (!CanUpgrade) return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || !economy.TrySpend(CurrencyType.Cash, Upgradable.NextUpgradePrice))
            return false;

        Level++;
        Upgradable.ApplyLevel(Level);
        OnUpgraded?.Invoke(this);
        return true;
    }

    /// <summary>Called by the save system: builds the slot at the saved level without charging money.</summary>
    public void RestoreBuilt(int level = 1)
    {
        if (State != SlotState.Built && building != null && building.Prefab != null)
            Construct(level);
    }

    /// <summary>Called by the upgrade panel when it opens or closes for this slot.</summary>
    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;

        IsSelected = value;
        OnSelectionChanged?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (State == SlotState.Built)
        {
            if (Upgradable != null) OnSelected?.Invoke(this);
            return;
        }

        TryBuy();
    }

    private void Construct(int level)
    {
        BuiltInstance = Instantiate(building.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        // First let the building read its data (e.g. the shelf takes its product)...
        foreach (IBuildable buildable in BuiltInstance.GetComponentsInChildren<IBuildable>())
            buildable.OnBuilt(this);

        // ...then apply the level, so the level table drives even level 1.
        Upgradable = BuiltInstance.GetComponentInChildren<IUpgradable>();
        Level = Mathf.Clamp(level, 1, Upgradable != null ? Upgradable.MaxLevel : 1);
        if (Upgradable != null) Upgradable.ApplyLevel(Level);

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
