
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenTutorial()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void Quit()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
