using UnityEngine;
using TMPro;

public class CustomersView : MonoBehaviour
{
    [SerializeField] protected TMP_Text label;
    [SerializeField] protected string prefix = "$";

    private void Start()
    {
        if (label == null) label = GetComponent<TMP_Text>();

        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.OnCustomersChanged += HandleCustomersChanged;
            StoreRegistry.OnAnyRegisteredChanged += HandleCustomersChanged;
        }

        HandleCustomersChanged();
    }

    private void OnDestroy()
    {
        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.OnCustomersChanged -= HandleCustomersChanged;
            StoreRegistry.OnAnyRegisteredChanged -= HandleCustomersChanged;
        }    
    }

    private void HandleCustomersChanged()
    {
        if (label == null || CustomerSpawner.Instance == null) return;

        label.text = $"{prefix}{CustomerSpawner.Instance.ActiveCount}/{CustomerSpawner.Instance.Limit}";
    }
}
