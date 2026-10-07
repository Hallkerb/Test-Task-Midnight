using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelView : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image levelProgressBar;

    private void Start()
    {
        if (StoreLevel.Instance == null)
        {
            Debug.LogWarning($"{name}: StoreLevel instance is missing.", this);
            enabled = false;
            return;
        }
        
        StoreLevel.Instance.OnProgressChanged += HandleProgressChanged;

        HandleProgressChanged(StoreLevel.Instance.CurrentLevel, StoreLevel.Instance.GetCurrentProgressInLevel(), StoreLevel.Instance.GetCashRequiredForNextLevel(), StoreLevel.Instance.GetNormalizedProgress());
    }

    private void OnDestroy()
    {
        if (StoreLevel.Instance != null)
            StoreLevel.Instance.OnProgressChanged -= HandleProgressChanged;
    }

    private void HandleProgressChanged(int currentLevel, double currentEarnedInLevel, double requiredForNextLevel, float normalizedProgress)
    {
        if (levelText != null)
            levelText.text = $"Level {currentLevel}";
            
        if (progressText != null)
            progressText.text = $"${EconomyManager.Format(currentEarnedInLevel)} / ${EconomyManager.Format(requiredForNextLevel)}";

        if (levelProgressBar != null)
            levelProgressBar.fillAmount = normalizedProgress;
    }
}
