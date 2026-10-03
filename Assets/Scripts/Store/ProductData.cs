using UnityEngine;

/// <summary>
/// A product sold on a shelf (tomato, milk, ...).
/// Create assets via: Assets → Create → Tycoon → Product Data.
/// Note: this is the price customers PAY, not the price of buying the shelf (see BuildingData).
/// </summary>
[CreateAssetMenu(fileName = "Product_", menuName = "Tycoon/Product Data")]
public class ProductData : ScriptableObject
{
    [SerializeField] private string displayName = "New Product";
    [SerializeField, Min(0)] private double basePrice = 10;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public double BasePrice => basePrice;
    public Sprite Icon => icon;
}
