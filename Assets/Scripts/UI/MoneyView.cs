using System;
using TMPro;
using UnityEngine;

/// <summary>
/// HUD money counter. Listens to EconomyManager and smoothly "counts" towards the real balance
/// instead of jumping, which makes every sale feel rewarding.
/// Put it on a TextMeshPro (UI) label, or assign the label manually.
/// </summary>
public class MoneyView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string prefix = "$";
    [Tooltip("How fast the shown number catches up with the real balance (higher = faster).")]
    [SerializeField, Min(1f)] private float catchUpSpeed = 8f;

    private EconomyManager economy;
    private double target;       // the real balance
    private double shown;        // the number currently displayed
    private long lastRounded = -1;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    // Subscribe in Start: EconomyManager.Instance is guaranteed to exist after all Awake calls.
    private void Start()
    {
        economy = EconomyManager.Instance;
        if (economy == null || label == null)
        {
            Debug.LogWarning($"{name}: EconomyManager or label is missing.", this);
            enabled = false;
            return;
        }

        economy.OnBalanceChanged += HandleBalanceChanged;

        target = shown = economy.Cash;
        Refresh();
    }

    private void OnDestroy()
    {
        if (economy != null)
            economy.OnBalanceChanged -= HandleBalanceChanged;
    }

    private void HandleBalanceChanged(CurrencyType type, double balance, double delta)
    {
        if (type == CurrencyType.Cash)
            target = balance;
    }

    private void Update()
    {
        if (shown == target) return;

        // Exponential catch-up: fast at first, smooth at the end, snaps when close enough.
        // Unscaled time, so the counter still finishes while the game is paused.
        double difference = target - shown;
        if (Math.Abs(difference) < 0.5)
            shown = target;
        else
            shown += difference * (1.0 - Math.Exp(-catchUpSpeed * Time.unscaledDeltaTime));

        Refresh();
    }

    private void Refresh()
    {
        // Rebuild the text only when the visible number actually changes (no string garbage every frame).
        long rounded = (long)Math.Round(shown);
        if (rounded == lastRounded) return;

        lastRounded = rounded;
        label.text = prefix + EconomyManager.Format(shown);
    }
}
