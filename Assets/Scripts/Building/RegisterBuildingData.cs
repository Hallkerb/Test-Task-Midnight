using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Characteristics of a cash register at one level.</summary>
[Serializable]
public struct RegisterLevel
{
    [Tooltip("Price to upgrade TO this level. Ignored for level 1 (its price is the purchase price).")]
    public double costToReach;

    [Tooltip("Seconds needed to serve one customer.")]
    public float serviceTime;

    [Tooltip("How many customers fit in the queue. The register prefab needs at least this many Queue Points.")]
    public int queueLength;
}

/// <summary>
/// Data for a cash register slot: everything from BuildingData plus the table of upgrade levels
/// (the first entry is the level after purchase).
/// Create via: Assets → Create → Tycoon → Register Building Data.
/// </summary>
[CreateAssetMenu(fileName = "Register", menuName = "Tycoon/Register Building Data")]
public class RegisterBuildingData : BuildingData
{
    [Tooltip("Index 0 = level 1 (right after purchase). Add entries for more levels.")]
    [SerializeField] private RegisterLevel[] levels =
    {
        new RegisterLevel { costToReach = 0,   serviceTime = 2.0f, queueLength = 3 },
        new RegisterLevel { costToReach = 200, serviceTime = 1.6f, queueLength = 4 },
        new RegisterLevel { costToReach = 500, serviceTime = 1.2f, queueLength = 5 }
    };

    public IReadOnlyList<RegisterLevel> Levels => levels;
}
