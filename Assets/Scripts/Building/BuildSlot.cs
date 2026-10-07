using System;
using UnityEngine;

/// <summary>
/// A slot that spawns a building. The same prefab is used for every register, shelf, etc.:
/// what gets built is defined by the assigned BuildingData.
/// Everything shared with other slots (states, chain, buying, clicking, selection) lives in Slot.
/// After the purchase this slot also owns the building's upgrade level: it takes the money,
/// the building (IUpgradable) applies the level to itself.
/// </summary>
[DisallowMultipleComponent]
public class BuildSlot : Slot
{
    [Header("Building")]
    [SerializeField] private BuildingData building;

    [Tooltip("Where the building is spawned. If empty, this object's transform is used.")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>Raised after every successful upgrade.</summary>
    public event Action<Slot> OnUpgraded;

    public BuildingData Building => building;

    /// <summary>The spawned building (null until built).</summary>
    public GameObject BuiltInstance { get; private set; }

    /// <summary>The upgradable part of the building (null if it cannot be upgraded).</summary>
    public IUpgradable Upgradable { get; private set; }

    /// <summary>Current upgrade level: 0 until built, 1 right after the purchase.</summary>
    public int Level { get; private set; }

    public bool CanUpgrade => State == SlotState.Built && Upgradable != null && Level < Upgradable.MaxLevel;
    public double NextUpgradePrice => Upgradable != null ? Upgradable.NextUpgradePrice : 0;

    public override string DisplayName => building != null ? building.DisplayName : name;
    public override double Price => building != null ? building.Price : 0;

    protected override bool CanConstruct => building != null && building.Prefab != null;

    // Only buildings that can be upgraded open the upgrade panel.
    protected override bool IsSelectable => Upgradable != null;

    protected override void Awake()
    {
        base.Awake();

        if (spawnPoint == null) spawnPoint = transform;
    }

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

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

    protected override void Construct(int level)
    {
        BuiltInstance = Instantiate(building.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        // First let the building read its data (e.g. the shelf takes its product)...
        foreach (IBuildable buildable in BuiltInstance.GetComponentsInChildren<IBuildable>())
            buildable.OnBuilt(this);

        // ...then apply the level, so the level table drives even level 1.
        Upgradable = BuiltInstance.GetComponentInChildren<IUpgradable>();
        Level = Mathf.Clamp(level, 1, Upgradable != null ? Upgradable.MaxLevel : 1);
        if (Upgradable != null) Upgradable.ApplyLevel(Level);
    }
}
