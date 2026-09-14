using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Game";
    [SerializeField]
    private GameObject tutorialPanel;

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    public void OpenTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }
    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void Quit()
    {
        Debug.Log("Quit Game");

        Application.Quit();
    }
}
