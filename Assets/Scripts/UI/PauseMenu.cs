using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// In-game pause: freezes the game (Time.timeScale = 0), shows the pause panel
/// and gives access to the settings and the way back to the main menu.
/// Hook the buttons to Pause(), Resume(), OpenSettings() and QuitToMenu().
/// Everything that moves uses scaled time (NavMeshAgent, Animator, timers), so it all freezes.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Tooltip("The pause window (starts closed). Its full-screen dim background blocks clicks on the world.")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private string menuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    private void Start()
    {
        // The scene always starts unpaused with every panel closed.
        pausePanel.SetActive(false);
        settingsPanel.gameObject.SetActive(false);

        settingsPanel.Closed += HandleSettingsClosed;
    }

    private void OnDestroy()
    {
        if (settingsPanel != null)
            settingsPanel.Closed -= HandleSettingsClosed;

        // Never leave the game frozen when this scene is unloaded.
        Time.timeScale = 1f;
    }

    public void Pause()
    {
        IsPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.Open();
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;   // otherwise the next scene would start frozen

        // The save system will be called here once it exists.
        SceneTransition.LoadWithScreen(menuSceneName);
    }

    private void HandleSettingsClosed()
    {
        if (IsPaused)
            pausePanel.SetActive(true);
    }
}
