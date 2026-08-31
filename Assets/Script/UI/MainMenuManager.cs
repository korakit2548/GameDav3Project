using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Game";


    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }


    public void Quit()
    {
        Debug.Log("Quit Game");

        Application.Quit();
    }
}
