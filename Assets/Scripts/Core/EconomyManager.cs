using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>Currency types used in the game. To add a new currency, just add it here.</summary>
public enum CurrencyType
{
    Cash = 0,   // main (soft) currency
    Gems = 1    // premium currency
}

/// <summary>
/// Manages the player's balance and income multipliers.
/// Money does NOT arrive passively: it comes only from customers via RegisterSale().
/// The manager tracks a smoothed average income per second, which is later used
/// to calculate offline earnings.
/// It knows nothing about UI, buildings or saving - it only notifies about changes via events.
/// Lives only in the game scene (not persisted between scenes): the SaveSystem is responsible
/// for calling CaptureData() before the scene unloads and ApplyData() after it loads.
/// </summary>
[DisallowMultipleComponent]
public class EconomyManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Singleton
    // ------------------------------------------------------------------
    public static EconomyManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // Inspector settings
    // ------------------------------------------------------------------
    [Header("Static")]
    [SerializeField] public static double ExchangeRate = 500; // 1 Gem = 500 Cash

    [Header("Starting values (new game)")]
    [SerializeField] private double startingCash = 100;
    [SerializeField] private double startingGems = 0;

    [Header("Average income tracking")]
    [Tooltip("Smoothing window in seconds. Larger value = more stable average, slower reaction.")]
    [SerializeField, Min(5f)] private float averagingWindowSeconds = 60f;

    [Header("Offline earnings")]
    [SerializeField, Min(0)] private float maxOfflineHours = 8f;
    [Tooltip("Share of the average income the player receives while away (0..1).")]
    [SerializeField, Range(0f, 1f)] private float offlineEfficiency = 0.5f;

    // ------------------------------------------------------------------
    // Events (UI and other systems subscribe to these)
    // ------------------------------------------------------------------
    /// <summary>Balance changed: (currency, new balance, delta).</summary>
    public event Action<CurrencyType, double, double> OnBalanceChanged;

    /// <summary>Not enough funds: (currency, required amount, current balance).</summary>
    public event Action<CurrencyType, double, double> OnInsufficientFunds;

    /// <summary>Average income per second was recalculated (raised about once per second).</summary>
    public event Action<double> OnAverageIncomeChanged;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private const float StatsTickInterval = 1f;

    private readonly Dictionary<CurrencyType, double> balances = new Dictionary<CurrencyType, double>();

    // Multipliers (progression, upgrades, boosts): key = id, value = multiplier (1.5 = +50%).
    private readonly Dictionary<string, double> multipliers = new Dictionary<string, double>();

    private double totalEarnedCash;
    private double totalMultiplier = 1.0;

    private double averageIncomePerSecond;   // exponential moving average
    private double earnedThisTick;           // sales accumulated since the last stats tick
    private float statsTimer;

    // ------------------------------------------------------------------
    // Public properties
    // ------------------------------------------------------------------
    public double Cash => GetBalance(CurrencyType.Cash);
    public double Gems => GetBalance(CurrencyType.Gems);
    public double TotalEarnedCash => totalEarnedCash;
    public double TotalMultiplier => totalMultiplier;

    /// <summary>Smoothed average income per second from customer sales.</summary>
    public double AverageIncomePerSecond => averageIncomePerSecond;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            balances[type] = 0;

        ResetToNewGame();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Does not add any money. It only updates the average income statistics.
    /// Time.deltaTime is 0 while the game is paused (timeScale = 0), so pause doesn't skew the average.
    /// </summary>
    private void Update()
    {
        statsTimer += Time.deltaTime;
        if (statsTimer < StatsTickInterval) return;

        double instantIncome = earnedThisTick / statsTimer;

        // Exponential moving average: smooth, cheap, needs no history buffer.
        double alpha = 1.0 - Math.Exp(-statsTimer / averagingWindowSeconds);
        averageIncomePerSecond += (instantIncome - averageIncomePerSecond) * alpha;

        earnedThisTick = 0;
        statsTimer = 0f;

        OnAverageIncomeChanged?.Invoke(averageIncomePerSecond);
    }

    // ------------------------------------------------------------------
    // Income from customers
    // ------------------------------------------------------------------
    /// <summary>
    /// Registers a customer purchase. This is the ONLY way Cash income should enter the game.
    /// The amount is multiplied by the current income multiplier and counted
    /// in the average income statistics.
    /// </summary>
    /// <param name="baseAmount">Price paid by the customer before multipliers.</param>
    /// <returns>The actual amount added to the balance (after multipliers).</returns>
    public double RegisterSale(double baseAmount)
    {
        if (!IsValidAmount(baseAmount) || baseAmount <= 0) return 0;

        double amount = baseAmount * totalMultiplier;
        AddCurrency(CurrencyType.Cash, amount);
        earnedThisTick += amount;

        return amount;
    }

    // ------------------------------------------------------------------
    // Balance
    // ------------------------------------------------------------------
    public double GetBalance(CurrencyType type)
    {
        return balances.TryGetValue(type, out double value) ? value : 0;
    }

    public bool CanAfford(CurrencyType type, double amount)
    {
        return IsValidAmount(amount) && GetBalance(type) >= amount;
    }

    /// <summary>
    /// Adds currency without multipliers and without affecting the average income
    /// (rewards, exchange, offline earnings). For customer payments use RegisterSale().
    /// For Cash, it is also counted in the total earned statistics.
    /// </summary>
    public void AddCurrency(CurrencyType type, double amount)
    {
        if (!IsValidAmount(amount) || amount <= 0) return;

        if (type == CurrencyType.Cash)
            totalEarnedCash += amount;

        ChangeBalance(type, amount);
    }

    /// <summary>
    /// Tries to spend currency. Returns true if there were enough funds.
    /// If not, the balance is unchanged and OnInsufficientFunds is raised.
    /// </summary>
    public bool TrySpend(CurrencyType type, double amount)
    {
        if (!IsValidAmount(amount) || amount < 0) return false;
        if (amount == 0) return true;

        double current = GetBalance(type);
        if (current < amount)
        {
            OnInsufficientFunds?.Invoke(type, amount, current);
            return false;
        }

        ChangeBalance(type, -amount);
        return true;
    }

    /// <summary>Exchanges one currency for another at a given rate (e.g. 1 Gem = 500 Cash).</summary>
    public bool TryExchange(CurrencyType from, double fromAmount, CurrencyType to, double rate)
    {
        if (from == to || rate <= 0 || !IsValidAmount(fromAmount) || fromAmount <= 0) return false;
        if (!TrySpend(from, fromAmount)) return false;

        AddCurrency(to, fromAmount * rate);
        return true;
    }

    // ------------------------------------------------------------------
    // Multipliers (progression, upgrades)
    // ------------------------------------------------------------------
    /// <summary>
    /// Adds or updates a sales multiplier. Multipliers are multiplied together.
    /// Example: SetMultiplier("level_5", 1.25) → customers pay +25%.
    /// </summary>
    public void SetMultiplier(string multiplierId, double value)
    {
        if (string.IsNullOrEmpty(multiplierId) || !IsValidAmount(value) || value <= 0) return;

        multipliers[multiplierId] = value;
        RecalculateMultiplier();
    }

    public void RemoveMultiplier(string multiplierId)
    {
        if (string.IsNullOrEmpty(multiplierId)) return;
        if (multipliers.Remove(multiplierId))
            RecalculateMultiplier();
    }

    // ------------------------------------------------------------------
    // Save / load (called by the SaveSystem)
    // ------------------------------------------------------------------
    public EconomyData CaptureData()
    {
        return new EconomyData
        {
            cash = Cash,
            gems = Gems,
            totalEarnedCash = totalEarnedCash,
            averageIncomePerSecond = averageIncomePerSecond,
            lastSaveUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    /// <summary>
    /// Restores state from a save and grants offline earnings.
    /// Multipliers are restored by the corresponding systems (progression) via SetMultiplier.
    /// </summary>
    /// <returns>How much Cash was earned while the player was away.</returns>
    public double ApplyData(EconomyData data)
    {
        if (data == null) return 0;

        balances[CurrencyType.Cash] = Math.Max(0, data.cash);
        balances[CurrencyType.Gems] = Math.Max(0, data.gems);
        totalEarnedCash = Math.Max(0, data.totalEarnedCash);

        // Seed the moving average with the saved value so it doesn't start from zero.
        averageIncomePerSecond = Math.Max(0, data.averageIncomePerSecond);
        earnedThisTick = 0;
        statsTimer = 0f;

        RaiseAllBalances();
        OnAverageIncomeChanged?.Invoke(averageIncomePerSecond);

        double offline = CalculateOfflineEarnings(data);
        if (offline > 0)
            AddCurrency(CurrencyType.Cash, offline);

        return offline;
    }

    /// <summary>Resets the economy to starting values (new game).</summary>
    public void ResetToNewGame()
    {
        multipliers.Clear();
        totalEarnedCash = 0;
        averageIncomePerSecond = 0;
        earnedThisTick = 0;
        statsTimer = 0f;

        balances[CurrencyType.Cash] = startingCash;
        balances[CurrencyType.Gems] = startingGems;

        RecalculateMultiplier();
        RaiseAllBalances();
        OnAverageIncomeChanged?.Invoke(0);
    }

    // ------------------------------------------------------------------
    // Utilities
    // ------------------------------------------------------------------
    /// <summary>Formats large numbers: 1,234 → 1.23K, 5,000,000 → 5M, etc.</summary>
    public static string Format(double value)
    {
        if (value < 1000) return value.ToString("0");

        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };
        int index = 0;
        while (value >= 1000 && index < suffixes.Length - 1)
        {
            value /= 1000;
            index++;
        }

        return value.ToString("0.##") + suffixes[index];
    }

    // ------------------------------------------------------------------
    // Internal logic
    // ------------------------------------------------------------------
    private void ChangeBalance(CurrencyType type, double delta)
    {
        double newValue = Math.Max(0, GetBalance(type) + delta);
        balances[type] = newValue;
        OnBalanceChanged?.Invoke(type, newValue, delta);
    }

    private void RecalculateMultiplier()
    {
        totalMultiplier = 1.0;
        foreach (double m in multipliers.Values)
            totalMultiplier *= m;
    }

    private double CalculateOfflineEarnings(EconomyData data)
    {
        if (data.lastSaveUnixTime <= 0 || data.averageIncomePerSecond <= 0) return 0;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double elapsed = now - data.lastSaveUnixTime;

        // Protection against the system clock being set back and against very long absences.
        if (elapsed <= 0) return 0;
        elapsed = Math.Min(elapsed, maxOfflineHours * 3600.0);

        return data.averageIncomePerSecond * elapsed * offlineEfficiency;
    }

    private void RaiseAllBalances()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            OnBalanceChanged?.Invoke(type, GetBalance(type), 0);
    }

    private static bool IsValidAmount(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}