using System;
using UnityEngine;

/// <summary>
/// Manages store level progression based on total cash earned from EconomyManager.
/// Calculates progress to the next level and raises events on level changes.
/// </summary>
[DisallowMultipleComponent]
public class StoreLevel : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Singleton
    // ------------------------------------------------------------------
    public static StoreLevel Instance { get; private set; }

    // ------------------------------------------------------------------
    // Inspector settings
    // ------------------------------------------------------------------
    [Header("Progression Settings")]
    [Tooltip("Base amount of cash required to reach Level 2.")]
    [SerializeField] private double _baseRequiredCash = 500;

    [Tooltip("Growth exponent for required cash per level. Formula: Base * (Level ^ Exponent)")]
    [SerializeField, Range(1f, 3f)] private float _growthExponent = 1.5f;

    [Tooltip("Maximum store level limit.")]
    [SerializeField] private int _maxLevel = 100;

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------
    /// <summary>Raised when the store levels up: (newLevel).</summary>
    public event Action<int> OnLevelUp;

    /// <summary>Raised when progress changes: (currentLevel, currentEarnedInLevel, requiredForNextLevel, normalizedProgress 0..1).</summary>
    public event Action<int, double, double, float> OnProgressChanged;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private int _currentLevel = 1;

    // ------------------------------------------------------------------
    // Public Properties
    // ------------------------------------------------------------------
    public int CurrentLevel => _currentLevel;
    public int MaxLevel => _maxLevel;
    public bool IsMaxLevel => _currentLevel >= _maxLevel;

    /// <summary>Total cash required to reach the specified level from level 1.</summary>
    public double GetTotalCashForLevel(int level)
    {
        if (level <= 1) return 0;
        return _baseRequiredCash * Math.Pow(level - 1, _growthExponent);
    }

    /// <summary>Cash required specifically to upgrade from (level - 1) to level.</summary>
    public double GetCashRequiredForNextLevel()
    {
        if (IsMaxLevel) return 0;
        return GetTotalCashForLevel(_currentLevel + 1) - GetTotalCashForLevel(_currentLevel);
    }

    /// <summary>Current earned cash progress towards the next level.</summary>
    public double GetCurrentProgressInLevel()
    {
        if (EconomyManager.Instance == null) return 0;

        double totalEarned = EconomyManager.Instance.TotalEarnedCash;
        double previousLevelCost = GetTotalCashForLevel(_currentLevel);

        return Math.Max(0, totalEarned - previousLevelCost);
    }

    /// <summary>Normalized progress to next level from 0.0 to 1.0 (useful for UI Sliders).</summary>
    public float GetNormalizedProgress()
    {
        if (IsMaxLevel) return 1f;

        double required = GetCashRequiredForNextLevel();
        if (required <= 0) return 1f;

        double current = GetCurrentProgressInLevel();
        return Mathf.Clamp01((float)(current / required));
    }

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
    }

    private void Start()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnBalanceChanged += HandleBalanceChanged;

        // Initial check in case values were already loaded
        CheckLevelProgression();
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;

        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    // Level Evaluation Logic
    // ------------------------------------------------------------------
    private void HandleBalanceChanged(CurrencyType type, double newBalance, double delta)
    {
        // We only re-evaluate progress when Cash increases
        if (type == CurrencyType.Cash && delta > 0)
        {
            CheckLevelProgression();
        }
    }

    /// <summary>
    /// Recalculates current store level based on TotalEarnedCash.
    /// </summary>
    public void CheckLevelProgression()
    {
        if (EconomyManager.Instance == null) return;

        double totalEarned = EconomyManager.Instance.TotalEarnedCash;
        int startingLevel = _currentLevel;

        while (_currentLevel < _maxLevel)
        {
            double nextLevelRequiredTotal = GetTotalCashForLevel(_currentLevel + 1);
            if (totalEarned >= nextLevelRequiredTotal)
            {
                _currentLevel++;
            }
            else
            {
                break;
            }
        }

        if (_currentLevel > startingLevel)
        {
            OnLevelUp?.Invoke(_currentLevel);
        }

        NotifyProgress();
    }

    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(
            _currentLevel,
            GetCurrentProgressInLevel(),
            GetCashRequiredForNextLevel(),
            GetNormalizedProgress()
        );
    }

    // ------------------------------------------------------------------
    // Save / Load Data Capture
    // ------------------------------------------------------------------
    public StoreLevelData CaptureData()
    {
        return new StoreLevelData
        {
            currentLevel = _currentLevel
        };
    }

    public void ApplyData(StoreLevelData data)
    {
        if (data == null) return;

        _currentLevel = Math.Clamp(data.currentLevel, 1, _maxLevel);
        CheckLevelProgression();
    }

    public void ResetToNewGame()
    {
        _currentLevel = 1;
        NotifyProgress();
    }
}
