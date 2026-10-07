using UnityEngine;
using TMPro;
using System;

public class TotalEarnedView : MonoBehaviour
{
    [SerializeField] protected TMP_Text label;
    [SerializeField] protected string prefix = "Total earned $";
    [Tooltip("How fast the shown number catches up with the real balance (higher = faster).")]
    [SerializeField, Min(1f)] protected float catchUpSpeed = 8f;

    protected EconomyManager economy;
    protected double target;       // the real balance
    protected double shown;        // the number currently displayed
    protected long lastRounded = -1;

    protected virtual void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    // Subscribe in Start: EconomyManager.Instance is guaranteed to exist after all Awake calls.
    protected virtual void Start()
    {
        economy = EconomyManager.Instance;
        if (economy == null || label == null)
        {
            Debug.LogWarning($"{name}: EconomyManager or label is missing.", this);
            enabled = false;
            return;
        }

        economy.OnBalanceChanged += HandleBalanceChanged;

        target = shown = economy.TotalEarnedCash;
        Refresh();
    }

    protected virtual void OnDestroy()
    {
        if (economy != null)
            economy.OnBalanceChanged -= HandleBalanceChanged;
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

    protected virtual void HandleBalanceChanged(CurrencyType type, double balance, double delta)
    {
        if (type == CurrencyType.Cash && delta > 0)
            target = economy.TotalEarnedCash;
    }

    protected virtual void Refresh()
    {
        // Rebuild the text only when the visible number actually changes (no string garbage every frame).
        long rounded = (long)Math.Round(shown);
        if (rounded == lastRounded) return;

        lastRounded = rounded;
        label.text = prefix + EconomyManager.Format(shown);
    }
}
