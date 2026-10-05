using System;

/// <summary>
/// Economy data for the save system.
/// Serialized via JsonUtility.
/// </summary>
[Serializable]
public class EconomyData
{
    public double cash;
    public double gems;
    public double totalEarnedCash;
    public double averageIncomePerSecond;   // used to calculate offline earnings
    public long lastSaveUnixTime;           // UTC, seconds
}
