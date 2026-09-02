using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCam : MonoBehaviour
{
    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;


    public void Start()
    {
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
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

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
