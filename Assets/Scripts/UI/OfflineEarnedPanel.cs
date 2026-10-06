using UnityEngine;
using TMPro;

public class OfflineEarnedPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text offlineEarningsText;
    [SerializeField] private string prefix = "You earned";
    [SerializeField] private string secondPrefix = "$";
    [SerializeField] private string postfix = "while offline.";
    [SerializeField] private GameObject window;

    void Start()
    {
        if (SaveController.Instance != null && SaveController.Instance.OfflineEarnings > 0)
        {
            double offlineEarnings = SaveController.Instance.OfflineEarnings;
            offlineEarningsText.text = $"{prefix} <color=#00FF00>{secondPrefix}{EconomyManager.Format(offlineEarnings)}</color> {postfix}";
            window.SetActive(true);
        }
    }

    public void Close()
    {
        window.SetActive(false);
    }
}
