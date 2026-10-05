using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings window: master volume slider and graphics quality dropdown.
/// The same panel is used in the main menu and in the pause menu.
/// Changes are applied immediately and saved when the panel is closed.
/// Hook the "Back" button to Close().
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [Tooltip("Optional: shows the volume as a percentage.")]
    [SerializeField] private TMP_Text volumeValueLabel;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    /// <summary>Raised when the player closes the panel (so a parent menu can show itself again).</summary>
    public event Action Closed;

    private void Awake()
    {
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

        volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        qualityDropdown.onValueChanged.AddListener(HandleQualityChanged);
    }

    private void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        qualityDropdown.onValueChanged.RemoveListener(HandleQualityChanged);
    }

    // Every time the panel opens, show the stored values (without triggering the change events).
    private void OnEnable()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        qualityDropdown.SetValueWithoutNotify(GameSettings.QualityLevel);
        UpdateVolumeLabel();
    }

    public void Open() => gameObject.SetActive(true);

    public void Close()
    {
        GameSettings.Save();
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void HandleVolumeChanged(float value)
    {
        GameSettings.SetVolume(value);
        UpdateVolumeLabel();
    }

    private void HandleQualityChanged(int index) => GameSettings.SetQuality(index);

    private void UpdateVolumeLabel()
    {
        if (volumeValueLabel != null)
            volumeValueLabel.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
    }
}
