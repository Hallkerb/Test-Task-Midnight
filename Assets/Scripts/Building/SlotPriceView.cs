using TMPro;
using UnityEngine;

/// <summary>
/// Floor label of a slot (building or store expansion): shows the name and price on the slot rectangle.
/// Text color reflects the state:
///   Locked                    - grey, shows what must be built first
///   Available, can afford     - green
///   Available, cannot afford  - red
/// Hides itself when the slot is built.
/// </summary>
public class SlotPriceView : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color affordableColor = new Color(0.35f, 0.85f, 0.35f);
    [SerializeField] private Color unaffordableColor = new Color(0.9f, 0.3f, 0.3f);

    [SerializeField] private TextMeshPro label;

    private Slot slot;
    private EconomyManager economy;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TextMeshPro>();

        slot = GetComponentInParent<Slot>();
    }

    // Subscribe in Start: all Awake calls (EconomyManager.Instance) are guaranteed to be done.
    private void Start()
    {
        if (slot == null)
        {
            Debug.LogWarning($"{name}: no Slot found in parents.", this);
            return;
        }

        slot.OnStateChanged += HandleSlotChanged;

        economy = EconomyManager.Instance;
        if (economy != null)
            economy.OnBalanceChanged += HandleBalanceChanged;

        Refresh();
    }

    private void OnDestroy()
    {
        if (slot != null) slot.OnStateChanged -= HandleSlotChanged;
        if (economy != null) economy.OnBalanceChanged -= HandleBalanceChanged;
    }

    private void HandleSlotChanged(Slot _) => Refresh();

    private void HandleBalanceChanged(CurrencyType type, double balance, double delta)
    {
        if (type == slot.Currency) Refresh();
    }

    private void Refresh()
    {
        switch (slot.State)
        {
            case SlotState.Built:
                gameObject.SetActive(false);   // the building replaces the label
                break;

            case SlotState.Locked:
                label.color = lockedColor;
                label.text = BuildLockedText();
                break;

            case SlotState.Available:
                bool canAfford = economy != null && economy.CanAfford(slot.Currency, slot.Price);
                label.color = canAfford ? affordableColor : unaffordableColor;
                label.text = $"{slot.DisplayName}\n{FormatPrice(slot.Price, slot.Currency)}";
                break;
        }
    }

    private string BuildLockedText()
    {
        if (StoreLevel.Instance != null && slot.RequiredLevel > StoreLevel.Instance.CurrentLevel)
            return $"Need: Level {slot.RequiredLevel}";

        Slot required = slot.RequiredSlot;
        string requiredName = required != null ? required.DisplayName : "previous";

        return $"Need: {requiredName}";
    }

    /// <summary>Cash is shown as "$100", other currencies as "5 Gems".</summary>
    private static string FormatPrice(double price, CurrencyType currency)
    {
        string amount = EconomyManager.Format(price);
        return currency == CurrencyType.Cash ? "$" + amount : $"{amount} {currency}";
    }
}
