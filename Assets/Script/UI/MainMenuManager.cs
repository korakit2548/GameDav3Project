
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    private void OnEnable() { ShowCursor(); }

    private void LateUpdate() { ShowCursor(); }

    private static void ShowCursor()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void Quit()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
