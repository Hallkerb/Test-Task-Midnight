using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Characteristics of a shelf at one level.</summary>
[Serializable]
public struct ShelfLevel
{
    [Tooltip("Price to upgrade TO this level. Ignored for level 1 (its price is the purchase price).")]
    public double costToReach;

    [Tooltip("Multiplies the product price.")]
    public float priceMultiplier;

    [Tooltip("Seconds a customer stands at the shelf.")]
    public float pickDuration;
}

/// <summary>
/// Data for a shelf slot: everything from BuildingData, the product the shelf sells
/// and the table of its upgrade levels (the first entry is the level after purchase).
/// One shelf prefab can be used by any number of these assets, each selling a different product,
/// so a new kind of shelf needs no new prefab.
/// Create via: Assets → Create → Tycoon → Shelf Building Data.
/// </summary>
[CreateAssetMenu(fileName = "Shelf_", menuName = "Tycoon/Shelf Building Data")]
public class ShelfBuildingData : BuildingData
{
    [Tooltip("The product sold on this shelf.")]
    [SerializeField] private ProductData product;

    [Tooltip("Index 0 = level 1 (right after purchase). Add entries for more levels.")]
    [SerializeField] private ShelfLevel[] levels =
    {
        new ShelfLevel { costToReach = 0,   priceMultiplier = 1f,    pickDuration = 1.5f },
        new ShelfLevel { costToReach = 150, priceMultiplier = 1.25f, pickDuration = 1.3f },
        new ShelfLevel { costToReach = 400, priceMultiplier = 1.6f,  pickDuration = 1.0f }
    };

    public ProductData Product => product;
    public IReadOnlyList<ShelfLevel> Levels => levels;
}
