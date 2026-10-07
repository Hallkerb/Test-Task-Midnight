using UnityEngine;

/// <summary>
/// A slot that unlocks an expandable store section: instead of spawning a prefab it activates
/// an existing scene object. Also provides the section's rectangular bounds (IStoreBounds) for camera navigation.
/// Everything shared with other slots (states, chain, buying, clicking, selection) lives in Slot.
/// </summary>
[DisallowMultipleComponent]
public class StoreSlot : Slot, IStoreBounds
{
    [Header("Expansion")]
    [Tooltip("Name shown on the price tag.")]
    [SerializeField] private string displayName = "Expansion";
    [Tooltip("Price to unlock and expand this store section.")]
    [SerializeField] private double _price = 500;
    [Tooltip("Currency type required for purchasing this expansion.")]
    [SerializeField] private CurrencyType _currencyType = CurrencyType.Cash;
    [Tooltip("The actual store section game object to activate when bought.")]
    [SerializeField] private GameObject _expansionObject;

    [Header("Zone Boundaries (World Relative Position)")]
    [SerializeField] private Vector2 _minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 _maxBounds = new Vector2(10f, 10f);

    public override string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public override double Price => _price;
    public override CurrencyType Currency => _currencyType;

    protected override bool CanConstruct => true;

    public Vector2 MinBounds => (Vector2)transform.position + _minBounds;
    public Vector2 MaxBounds => (Vector2)transform.position + _maxBounds;

    private void OnEnable() => StoreRegistry.Register(this);
    private void OnDisable() => StoreRegistry.Unregister(this);

    /// <summary>Checks if a world position (X, Z) is inside this zone's boundaries.</summary>
    public bool Contains(Vector3 position)
    {
        Vector2 min = MinBounds;
        Vector2 max = MaxBounds;

        return position.x >= min.x && position.x <= max.x &&
               position.z >= min.y && position.z <= max.y;
    }

    /// <summary>Activates the expansion object (the level is not used by store sections).</summary>
    protected override void Construct(int level)
    {
        if (_expansionObject != null)
            _expansionObject.SetActive(true);
    }

    [ContextMenu("Try Set Bounds")]
    private void TrySetBounds()
    {
        if (TryGetComponent(out Collider collider))
        {
            _minBounds = new Vector2(collider.bounds.min.x, collider.bounds.min.z);
            _maxBounds = new Vector2(collider.bounds.max.x, collider.bounds.max.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((MinBounds.x + MaxBounds.x) * 0.5f, transform.position.y, (MinBounds.y + MaxBounds.y) * 0.5f);
        Vector3 size = new Vector3(MaxBounds.x - MinBounds.x, 1f, MaxBounds.y - MinBounds.y);
        Gizmos.DrawWireCube(center, size);
    }
}
