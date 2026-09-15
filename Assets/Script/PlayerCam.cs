using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCam : MonoBehaviour
{
    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isGameOver = false;

    public void Start()
    {
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
        if (isGameOver)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;

            pausePanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Game Paused");
        }
        else
        {
            Time.timeScale = 1f;

            pausePanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Game Resumed");
        }
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        isPaused = false;
        pausePanel.SetActive(false);

        Time.timeScale = 0f;

        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Over");
    }
    public void GoToMainMenu()
    {
        
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
