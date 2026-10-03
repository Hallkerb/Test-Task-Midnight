using UnityEngine;

/// <summary>
/// Describes something the player can buy in a slot (register, shelf, ...).
/// Create assets via: Assets → Create → Tycoon → Building Data.
/// A new purchasable item is just a new asset, no code changes needed.
/// </summary>
[CreateAssetMenu(fileName = "Building_", menuName = "Tycoon/Building Data")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string displayName = "New Building";
    [SerializeField, Min(0)] private double price = 100;
    [Tooltip("Prefab that is spawned into the slot after purchase.")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public double Price => price;
    public GameObject Prefab => prefab;
    public Sprite Icon => icon;
}
