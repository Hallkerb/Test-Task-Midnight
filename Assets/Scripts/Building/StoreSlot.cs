using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents an expandable store section. Inherits from StoreZone to provide camera boundaries (IStoreBounds).
/// Instead of instantiating a prefab, it activates an existing scene object (store expansion) upon purchase.
/// Supports chained dependencies (requiredSlot) and EconomyManager transactions.
/// </summary>
[DisallowMultipleComponent]
public class StoreSlot : StoreZone, IPointerClickHandler
{
    [Header("Data")]
    [Tooltip("Unique id, used by the save system. If empty, the object name is used.")]
    [SerializeField] private string _slotId;
    [Tooltip("Price to unlock and expand this store section.")]
    [SerializeField] private double _price = 500;
    [Tooltip("Currency type required for purchasing this expansion.")]
    [SerializeField] private CurrencyType _currencyType = CurrencyType.Cash;
    [Tooltip("This slot must be built/unlocked first. Leave empty for the first zone of a chain.")]
    [SerializeField] private StoreSlot _requiredSlot;
    [SerializeField] private SlotState state = SlotState.Locked;

    [Header("Scene References")]
    [Tooltip("The actual store section game object to activate when bought.")]
    [SerializeField] private GameObject _expansionObject;
    [Tooltip("Shown while the slot is Locked (silhouette, lock icon).")]
    [SerializeField] private GameObject _lockedVisual;
    [Tooltip("Shown while the slot is Available (price tag, highlight).")]
    [SerializeField] private GameObject _availableVisual;
    [Tooltip("Shown in every state until the slot is built (e.g. barrier). Hidden once Built.")]
    [SerializeField] private GameObject[] _showUntilBuilt;

    /// <summary>Raised once the store slot has been unlocked and activated.</summary>
    public event Action<StoreSlot> OnBuilt;

    /// <summary>Raised when the player clicks a built slot (opens expansion info panel, if needed).</summary>
    public event Action<StoreSlot> OnSelected;

    /// <summary>Raised on every state change (UI can update price tags, locks, etc.).</summary>
    public event Action<StoreSlot> OnStateChanged;

    /// <summary>Raised when the slot becomes selected or deselected.</summary>
    public event Action<StoreSlot> OnSelectionChanged;

    public string SlotId => string.IsNullOrEmpty(_slotId) ? name : _slotId;
    public double Price => _price;
    public CurrencyType CurrencyType => _currencyType;
    public StoreSlot RequiredSlot => _requiredSlot;
    public SlotState State => state;

    /// <summary>True while the slot details/panel is selected by the player.</summary>
    public bool IsSelected { get; private set; }

    private bool IsUnlocked => _requiredSlot == null || _requiredSlot.State == SlotState.Built;

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    private void Start()
    {
        if (_requiredSlot != null)
        {
            _requiredSlot.OnBuilt += HandleRequiredBuilt;
        }

        // The save system may have already restored this slot before Start.
        if (State != SlotState.Built)
        {
            SetState(IsUnlocked ? SlotState.Available : SlotState.Locked);
        }
    }

    private void OnDestroy()
    {
        if (_requiredSlot != null)
        {
            _requiredSlot.OnBuilt -= HandleRequiredBuilt;
        }
    }

    /// <summary>
    /// Attempts to purchase and activate the store expansion zone. Returns true on success.
    /// </summary>
    public bool TryBuy()
    {
        if (State != SlotState.Available)
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || !economy.TrySpend(_currencyType, _price))
            return false;

        Construct();
        return true;
    }

    /// <summary>
    /// Called by the save system: unlocks and activates the store section without charging money.
    /// </summary>
    public void RestoreBuilt()
    {
        if (State != SlotState.Built)
        {
            Construct();
        }
    }

    /// <summary>
    /// Called when selecting/deselecting this store zone in the UI.
    /// </summary>
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
            OnSelected?.Invoke(this);
            return;
        }

        TryBuy();
    }

    /// <summary>
    /// Activates the expansion object and updates slot state to Built.
    /// </summary>
    private void Construct()
    {
        if (_expansionObject != null)
        {
            _expansionObject.SetActive(true);
        }

        SetState(SlotState.Built);
        OnBuilt?.Invoke(this);
    }

    private void HandleRequiredBuilt(StoreSlot _)
    {
        if (State == SlotState.Locked)
        {
            SetState(SlotState.Available);
        }
    }

    private void SetState(SlotState newState)
    {
        state = newState;

        if (_lockedVisual != null) _lockedVisual.SetActive(newState == SlotState.Locked);
        if (_availableVisual != null) _availableVisual.SetActive(newState == SlotState.Available);

        if (_showUntilBuilt != null)
        {
            foreach (GameObject go in _showUntilBuilt)
            {
                if (go != null) go.SetActive(newState != SlotState.Built);
            }
        }

        OnStateChanged?.Invoke(this);
    }
}
