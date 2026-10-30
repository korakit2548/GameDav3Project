
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCam : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private GameObject levelCompletePanel;

    [Header("Scene Settings")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [SerializeField]
    private string nextLevelSceneName = "Level2";

    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isLevelComplete = false;

    public void Start()
    {
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        levelCompletePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
        if (isGameOver || isLevelComplete)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
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
        }
        else
        {
            Time.timeScale = 1f;
            pausePanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

    // ‡√’¬°‡¡◊ËÕ®∫¥Ë“π
    public void LevelComplete()
    {
        if (isGameOver || isLevelComplete)
            return;

        isLevelComplete = true;
        isPaused = false;

        pausePanel.SetActive(false);

        Time.timeScale = 0f;
        levelCompletePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Level Complete");
    }

    // ªÿË¡‰ª¥Ë“π∂—¥‰ª
    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
