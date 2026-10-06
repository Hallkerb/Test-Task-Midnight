using System.Collections.Generic;
using System.Globalization;

/// <summary>One row of the upgrade panel: a characteristic with its current and next value.</summary>
public readonly struct UpgradeStat
{
    public readonly string Label;
    public readonly string Current;

    /// <summary>Value after the next upgrade, or null if the building is at its maximum level.</summary>
    public readonly string Next;

    public UpgradeStat(string label, string current, string next = null)
    {
        Label = label;
        Current = current;
        Next = next;
    }

    /// <summary>Formats a number the same way on every machine (no comma/dot differences between locales).</summary>
    public static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>
/// Implemented by buildings that can be upgraded (shelf, cash register).
/// BuildSlot takes the money and stores the level; the building applies the level to itself.
/// Level 1 is the level right after the purchase.
/// </summary>
public interface IUpgradable
{
    int Level { get; }
    int MaxLevel { get; }

    /// <summary>Price to reach Level + 1 (0 at the maximum level).</summary>
    double NextUpgradePrice { get; }

    /// <summary>Sets the building's characteristics for the given level (also used when loading a save).</summary>
    void ApplyLevel(int level);

    /// <summary>Rows for the upgrade panel: what the characteristics are now and what they become.</summary>
    IReadOnlyList<UpgradeStat> GetStats();
}
