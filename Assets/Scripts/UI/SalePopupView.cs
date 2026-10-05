using TMPro;
using UnityEngine;

/// <summary>
/// A floating "+$" above a cash register: appears on every sale, rises and fades out.
/// Place this script on the register prefab (root or any child that is NOT the label itself)
/// and assign a 3D TextMeshPro child as the label. It finds its register automatically.
/// Uses scaled time, so the popup freezes when the game is paused.
/// </summary>
public class SalePopupView : MonoBehaviour
{
    [Tooltip("A 3D TextMeshPro object (child of the register), positioned above it.")]
    [SerializeField] private TextMeshPro label;
    [SerializeField] private string prefix = "+$";
    [SerializeField, Min(0.1f)] private float duration = 1f;
    [SerializeField, Min(0f)] private float riseDistance = 1f;

    private CashRegister register;
    private Vector3 startLocalPosition;
    private Camera cam;
    private float timer;

    private void Awake()
    {
        register = GetComponentInParent<CashRegister>();

        if (label != null)
            startLocalPosition = label.transform.localPosition;
    }

    private void Start()
    {
        if (register == null || label == null)
        {
            Debug.LogWarning($"{name}: needs a CashRegister in its parents and an assigned label.", this);
            enabled = false;
            return;
        }

        register.OnSaleCompleted += HandleSale;
        label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (register != null)
            register.OnSaleCompleted -= HandleSale;
    }

    private void HandleSale(CashRegister source, double amount)
    {
        if (amount <= 0) return;

        label.text = prefix + EconomyManager.Format(amount);
        label.transform.localPosition = startLocalPosition;
        label.alpha = 1f;
        label.gameObject.SetActive(true);

        timer = duration;
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(timer / duration);   // 0 -> 1

        label.transform.localPosition = startLocalPosition + Vector3.up * (riseDistance * progress);
        label.alpha = 1f - progress * progress;                  // stays visible longer, then fades quickly

        FaceCamera();

        if (timer <= 0f)
            label.gameObject.SetActive(false);
    }

    /// <summary>Billboard: the text always faces the camera, whatever the register's rotation is.</summary>
    private void FaceCamera()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
            label.transform.rotation = cam.transform.rotation;
    }
}
