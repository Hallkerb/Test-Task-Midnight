using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] GameObject settingsPanel;

    void Awake()
    {
        playButton.onClick.AddListener(OnPlay);
        settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        quitButton.onClick.AddListener(OnQuit);
    }

    void OnPlay() => SceneTransition.LoadWithScreen("Game");

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
