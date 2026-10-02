using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    public static string TargetScene { get; private set; }

    public static void LoadWithScreen(string target)
    {
        TargetScene = target;
        SceneManager.LoadScene("Loading");
    }
}
