using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles asynchronous scene loading in the "Loading" scene.
/// Reads the target scene name from SceneTransition, displays progress on UI,
/// and activates the scene once loading and the minimum display time are complete.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image _progressBarFill;
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("Settings")]
    [SerializeField] private float _minLoadingTime = 0.5f;
    [SerializeField] private string prefix = "Loading:";

    /// <summary>Default scene name to load if no target scene was specified in SceneTransition.</summary>
    [SerializeField] private const string DefaultSceneName = "MainMenu";

    private void Start()
    {
        string sceneToLoad = SceneTransition.TargetScene;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[LoadingScreen] TargetScene is null or empty! Loading default scene.");
            sceneToLoad = DefaultSceneName;
        }

        StartCoroutine(LoadSceneRoutine(sceneToLoad));
    }

    /// <summary>
    /// Coroutine that asynchronously loads the target scene and updates the progress bar.
    /// </summary>
    /// <param name="targetScene">The name of the scene to load.</param>
    private IEnumerator LoadSceneRoutine(string targetScene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        
        // Prevent scene activation until loading is finished and minimum display time has elapsed.
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;

        while (!asyncLoad.isDone)
        {
            timer += Time.deltaTime;

            // asyncLoad.progress ranges from 0.0 to 0.9 (0.9 means scene loading is complete).
            float rawProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Smooth progress calculation taking minimum display duration into account.
            float targetProgress = Mathf.Min(rawProgress, timer / _minLoadingTime);

            // Update UI elements if assigned.
            if (_progressBarFill != null)
            {
                _progressBarFill.fillAmount = targetProgress;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{prefix} {(targetProgress * 100f):F0}%";
            }

            // Activate scene when fully loaded and minimum duration has been met.
            if (asyncLoad.progress >= 0.9f && timer >= _minLoadingTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}