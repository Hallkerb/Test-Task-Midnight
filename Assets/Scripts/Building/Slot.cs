using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>Lifecycle of a slot.</summary>
public enum SlotState
{
    Locked,     // the required slot is not built yet
    Available,  // can be bought
    Built       // already bought, the content exists
}

/// <summary>
/// Base class of everything the player can buy in the scene: build slots (a building is spawned)
/// and store slots (a store section is unlocked).
///
/// It owns everything they have in common: the state machine Locked -> Available -> Built,
/// the chain of requirements (any kind of slot can require any other kind), buying with the economy,
/// the visuals of each state, click handling and selection.
/// Derived classes only define WHAT is bought (name, price, currency) and what "built" means (Construct).
///
/// Clicking requires: a Collider on this object, an EventSystem in the scene
/// and a PhysicsRaycaster on the camera.
/// </summary>
[DisallowMultipleComponent]
public abstract class Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("Data")]
    [Tooltip("Unique id, used by the save system. If empty, the object name is used.")]
    [FormerlySerializedAs("_slotId")]
    [SerializeField] private string slotId;

    [Tooltip("This level must be reached first.")]
    [FormerlySerializedAs("_requiredLevel")]
    [SerializeField] private int requiredLevel = 0;

    [Tooltip("This slot must be built first (any kind of slot). Leave empty for the first slot of a chain.")]
    [FormerlySerializedAs("_requiredSlot")]
    [SerializeField] private Slot requiredSlot;

    [Tooltip("Current state, shown here for debugging. Leave it Locked in the editor.")]
    [SerializeField] private SlotState state = SlotState.Locked;

    [Header("State visuals")]
    [Tooltip("Shown while the slot is Locked (silhouette, lock icon).")]
    [FormerlySerializedAs("_lockedVisual")]
    [SerializeField] private GameObject lockedVisual;

    [Tooltip("Shown while the slot is Available (price tag, highlight).")]
    [FormerlySerializedAs("_availableVisual")]
    [SerializeField] private GameObject availableVisual;

    [Tooltip("Shown in every state until the slot is built (e.g. a barrier). Hidden once Built.")]
    [FormerlySerializedAs("_showUntilBuilt")]
    [SerializeField] private GameObject[] showUntilBuilt;

    /// <summary>Raised once the slot has been built (bought or restored from a save).</summary>
    public event Action<Slot> OnBuilt;

    /// <summary>Raised on every state change (UI can update price tags, locks, etc.).</summary>
    public event Action<Slot> OnStateChanged;

    /// <summary>Raised when the player clicks a built slot that can be selected (e.g. opens the upgrade panel).</summary>
    public event Action<Slot> OnSelected;

    /// <summary>Raised when the slot becomes selected or deselected.</summary>
    public event Action<Slot> OnSelectionChanged;

    public string SlotId => string.IsNullOrEmpty(slotId) ? name : slotId;
    public int RequiredLevel => requiredLevel;
    public Slot RequiredSlot => requiredSlot;
    public SlotState State => state;

    /// <summary>True while the slot's details (e.g. the upgrade panel) are open.</summary>
    public bool IsSelected { get; private set; }

    /// <summary>Name shown to the player (price tags, panels).</summary>
    public abstract string DisplayName { get; }

    /// <summary>Price of the slot.</summary>
    public abstract double Price { get; }

    /// <summary>Currency the slot is bought with.</summary>
    public virtual CurrencyType Currency => CurrencyType.Cash;

    /// <summary>True if there is something to build (e.g. a building with a prefab is assigned).</summary>
    protected abstract bool CanConstruct { get; }

    /// <summary>Whether clicking the slot after it is built should raise OnSelected.</summary>
    protected virtual bool IsSelectable => true;

    private bool IsUnlocked
    {
        get
        {
            if (StoreLevel.Instance != null && StoreLevel.Instance.CurrentLevel < requiredLevel)
                return false;

            if (requiredSlot == null)
                return true;

            return requiredSlot.State == SlotState.Built;
        }
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------
    protected virtual void Awake() { }

    protected virtual void Start()
    {
        if (StoreLevel.Instance != null)
            StoreLevel.Instance.OnLevelUp += HandleLevelChanged;

        if (requiredSlot != null)
            requiredSlot.OnBuilt += HandleRequiredBuilt;

        // The save system may have already restored this slot before Start.
        if (state != SlotState.Built)
            SetState(IsUnlocked ? SlotState.Available : SlotState.Locked);
    }

    protected virtual void OnDestroy()
    {
        if (StoreLevel.Instance != null)
            StoreLevel.Instance.OnLevelUp -= HandleLevelChanged;
        
        if (requiredSlot != null)
            requiredSlot.OnBuilt -= HandleRequiredBuilt;
    }

    // ------------------------------------------------------------------
    // Buying
    // ------------------------------------------------------------------
    /// <summary>Tries to buy the slot. Returns true on success.</summary>
    public bool TryBuy()
    {
        if (state != SlotState.Available || !CanConstruct)
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null || !economy.TrySpend(Currency, Price))
            return false;

        Build(1);
        return true;
    }

    /// <summary>Called by the save system: builds the slot (at the saved level) without charging money.</summary>
    public void RestoreBuilt(int level = 1)
    {
        if (state != SlotState.Built && CanConstruct)
            Build(level);
    }

    /// <summary>Creates the content of the slot (spawns a building, activates a section...).</summary>
    protected abstract void Construct(int level);

    private void Build(int level)
    {
        Construct(level);
        SetState(SlotState.Built);
        OnBuilt?.Invoke(this);
    }

    // ------------------------------------------------------------------
    // Interaction
    // ------------------------------------------------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        if (state == SlotState.Built)
        {
            if (IsSelectable) OnSelected?.Invoke(this);
            return;
        }

        TryBuy();
    }

    /// <summary>Called by the UI when it opens or closes the details of this slot.</summary>
    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;

        IsSelected = value;
        OnSelectionChanged?.Invoke(this);
    }

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private void HandleRequiredBuilt(Slot _)
    {
        RequiredCheck();
    }

    private void HandleLevelChanged(int _)
    {
        RequiredCheck();
    }

    private void RequiredCheck()
    {
        if (state == SlotState.Locked && IsUnlocked)
            SetState(SlotState.Available);
    }

    private void SetState(SlotState newState)
    {
        state = newState;

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
