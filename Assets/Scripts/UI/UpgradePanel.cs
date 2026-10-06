using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The upgrade window. Opens when the player clicks a built slot whose building can be upgraded
/// and shows what the upgrade changes ("now > after") together with the price.
/// It knows nothing about shelves or registers: everything comes from the slot and IUpgradable.
///
/// Put this script on an object that is always active (e.g. the HUD) and assign the window,
/// which starts inactive. Hook the Close button to Close().
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    /// <summary>One row of the window: characteristic name, current value and value after the upgrade.</summary>
    [Serializable]
    private class StatRow
    {
        public GameObject root;
        public TMP_Text label;
        public TMP_Text current;
        public TMP_Text next;
    }

    [Header("Window")]
    [Tooltip("The window itself (starts inactive).")]
    [SerializeField] private GameObject window;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text levelLabel;
    [Tooltip("Rows for characteristics. Unused rows are hidden.")]
    [SerializeField] private StatRow[] rows;

    [Header("Upgrade button")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonLabel;
    [SerializeField] private Color affordableColor = new Color(0.35f, 0.85f, 0.35f);
    [SerializeField] private Color unaffordableColor = new Color(0.9f, 0.3f, 0.3f);

    private List<BuildSlot> slots;
    private BuildSlot selected;
    private EconomyManager economy;

    // Slots register themselves in OnEnable, so they are all known by the time Start runs.
    private void Start()
    {
        window.SetActive(false);

        slots = new List<BuildSlot>(StoreRegistry.Slots);
        foreach (BuildSlot slot in slots)
            slot.OnSelected += Show;

        economy = EconomyManager.Instance;
        if (economy != null)
            economy.OnBalanceChanged += HandleBalanceChanged;

        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
    }

    private void OnDestroy()
    {
        if (slots != null)
        {
            foreach (BuildSlot slot in slots)
                if (slot != null) slot.OnSelected -= Show;
        }

        if (selected != null) selected.OnUpgraded -= HandleUpgraded;
        if (economy != null) economy.OnBalanceChanged -= HandleBalanceChanged;
    }

    public void Show(BuildSlot slot)
    {
        if (selected != null)
        {
            selected.OnUpgraded -= HandleUpgraded;
            selected.SetSelected(false);
        }

        selected = slot;
        selected.OnUpgraded += HandleUpgraded;
        selected.SetSelected(true);

        window.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (selected != null)
        {
            selected.OnUpgraded -= HandleUpgraded;
            selected.SetSelected(false);
        }

        selected = null;
        window.SetActive(false);
    }

    private void HandleUpgradeClicked()
    {
        if (selected != null)
            selected.TryUpgrade();   // the window refreshes itself through OnUpgraded
    }

    private void HandleUpgraded(BuildSlot slot) => Refresh();

    private void HandleBalanceChanged(CurrencyType type, double balance, double delta)
    {
        if (selected != null && type == CurrencyType.Cash)
            RefreshButton();
    }

    private void Refresh()
    {
        if (selected == null || selected.Upgradable == null) return;

        IUpgradable upgradable = selected.Upgradable;

        titleLabel.text = selected.Building != null ? selected.Building.DisplayName : selected.name;
        levelLabel.text = $"Level {selected.Level}/{upgradable.MaxLevel}";

        IReadOnlyList<UpgradeStat> stats = upgradable.GetStats();

        for (int i = 0; i < rows.Length; i++)
        {
            bool used = i < stats.Count;
            rows[i].root.SetActive(used);
            if (!used) continue;

            UpgradeStat stat = stats[i];
            rows[i].label.text = stat.Label;
            rows[i].current.text = stat.Current;

            // At the maximum level there is no "next" value to show.
            bool hasNext = stat.Next != null;
            rows[i].next.gameObject.SetActive(hasNext);
            if (hasNext) rows[i].next.text = "> " + stat.Next;
        }

        RefreshButton();
    }

    private void RefreshButton()
    {
        if (selected == null) return;

        if (!selected.CanUpgrade)
        {
            upgradeButton.interactable = false;
            upgradeButtonLabel.text = "MAX LEVEL";
            return;
        }

        double price = selected.NextUpgradePrice;
        bool canAfford = economy != null && economy.CanAfford(CurrencyType.Cash, price);

        upgradeButton.interactable = canAfford;
        upgradeButtonLabel.text = "Upgrade  $" + EconomyManager.Format(price);
        upgradeButtonLabel.color = canAfford ? affordableColor : unaffordableColor;
    }
}
