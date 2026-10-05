using UnityEngine;

/// <summary>
/// Player settings (volume, graphics quality): applies them and stores them in PlayerPrefs.
/// It is a static class that initializes itself before the first scene loads,
/// so the saved settings are in effect in whichever scene the game starts from.
/// UI (SettingsPanel) only reads and changes values through this class.
/// </summary>
public static class GameSettings
{
    private const string VolumeKey = "settings.volume";
    private const string QualityKey = "settings.quality";

    /// <summary>Master volume, 0..1.</summary>
    public static float Volume { get; private set; } = 1f;

    /// <summary>Index into QualitySettings.names.</summary>
    public static int QualityLevel { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        int lastQuality = QualitySettings.names.Length - 1;

        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, lastQuality);

        ApplyVolume();
        ApplyQuality();
    }

    public static void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public static void SetQuality(int level)
    {
        QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        ApplyQuality();
    }

    /// <summary>Writes the current values to disk. Called when the settings panel is closed.</summary>
    public static void Save()
    {
        PlayerPrefs.SetFloat(VolumeKey, Volume);
        PlayerPrefs.SetInt(QualityKey, QualityLevel);
        PlayerPrefs.Save();
    }

    private static void ApplyVolume() => AudioListener.volume = Volume;

    private static void ApplyQuality() => QualitySettings.SetQualityLevel(QualityLevel, true);
}
