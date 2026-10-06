using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Highlights a built slot while the mouse is over it and while its upgrade panel is open:
/// a plate lying on the floor lights up, green if an upgrade can be bought right now, yellow otherwise
/// (not enough money, or the building is already at its maximum level).
/// The selected slot stays highlighted (more opaque) even after the mouse has left.
/// Put it on the BuildSlot root (the object with the collider) and assign a separate highlight plate.
/// It relies on the same EventSystem and PhysicsRaycaster as clicking; touch screens have no hover.
/// </summary>
[RequireComponent(typeof(BuildSlot))]
public class SlotHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("A plate (e.g. a Quad on the floor) used only for the highlight. Keep it inactive in the prefab.")]
    [SerializeField] private Renderer plate;

    [Header("Colors (use a transparent material on the plate)")]
    [SerializeField] private Color canUpgradeColor = new Color(0.35f, 0.85f, 0.35f, 0.6f);
    [SerializeField] private Color otherColor = new Color(0.95f, 0.8f, 0.2f, 0.6f);
    [Tooltip("Opacity of the plate while the slot is selected (hover uses the alpha of the colors above).")]
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.9f;

    // URP uses _BaseColor, the built-in pipeline uses _Color: set both, whichever the shader has is used.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private BuildSlot slot;
    private EconomyManager economy;
    private MaterialPropertyBlock block;
    private bool hovered;

    private void Awake()
    {
        slot = GetComponent<BuildSlot>();
        block = new MaterialPropertyBlock();

        if (plate == null)
        {
            Debug.LogWarning($"{name}: no highlight plate assigned.", this);
            enabled = false;
            return;
        }

        plate.gameObject.SetActive(false);
    }

    // Subscribe in Start: EconomyManager.Instance is guaranteed to exist after all Awake calls.
    private void Start()
    {
        slot.OnStateChanged += HandleSlotChanged;
        slot.OnUpgraded += HandleSlotChanged;
        slot.OnSelectionChanged += HandleSlotChanged;

        economy = EconomyManager.Instance;
        if (economy != null)
            economy.OnBalanceChanged += HandleBalanceChanged;
    }

    private void OnDestroy()
    {
        if (slot != null)
        {
            slot.OnStateChanged -= HandleSlotChanged;
            slot.OnUpgraded -= HandleSlotChanged;
            slot.OnSelectionChanged -= HandleSlotChanged;
        }

        if (economy != null)
            economy.OnBalanceChanged -= HandleBalanceChanged;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    // While hovered, the color follows purchases and balance changes (e.g. the player just became able to afford it).
    private void HandleSlotChanged(BuildSlot _) => Refresh();

    private void HandleBalanceChanged(CurrencyType type, double balance, double delta)
    {
        if ((hovered || slot.IsSelected) && type == CurrencyType.Cash)
            Refresh();
    }

    private void Refresh()
    {
        if (plate == null) return;

        bool highlighted = hovered || slot.IsSelected;
        bool show = highlighted && slot.State == SlotState.Built && slot.Upgradable != null;
        plate.gameObject.SetActive(show);
        if (!show) return;

        bool canUpgradeNow = slot.CanUpgrade
            && economy != null
            && economy.CanAfford(CurrencyType.Cash, slot.NextUpgradePrice);

        Color color = canUpgradeNow ? canUpgradeColor : otherColor;
        if (slot.IsSelected) color.a = selectedAlpha;

        plate.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        plate.SetPropertyBlock(block);
    }
}
