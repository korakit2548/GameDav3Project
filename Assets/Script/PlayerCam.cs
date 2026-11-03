
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerCam : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private GameObject levelCompletePanel;

    [SerializeField]
    private GameObject inGameUI;

    private readonly List<(GameObject ui, bool active)> gameplayUI = new List<(GameObject, bool)>();
    private bool gameplayUIHidden;

    [Header("Scene Settings")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [SerializeField]
    private string nextLevelSceneName = "Level2";

    [Header("Game Over Transition")]
    [SerializeField] private GameOverSequence gameOverSequence;

    private PausePanelSlide pauseSlide;
    private PlayerInput gameplayInput;
    private readonly List<(CinemachineInputAxisController input, bool enabled)> cameraInputs = new List<(CinemachineInputAxisController, bool)>();
    private bool mouseModeReady;
    private bool windowFocused = true;
    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isLevelComplete = false;

    public void Start()
    {
        Time.timeScale = 1f;

        ResolveGameplayInput();
        ResolveGameplayUI();
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var input in root.GetComponentsInChildren<CinemachineInputAxisController>(true))
                cameraInputs.Add((input, input.enabled));
        mouseModeReady = true;
        pauseSlide = GetComponent<PausePanelSlide>();
        if (pauseSlide == null) pauseSlide = gameObject.AddComponent<PausePanelSlide>();
        pauseSlide.Initialize(pausePanel);
        pauseSlide.HideImmediate();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        if (SceneFadeTransition.IsTransitioning) PrepareForSceneTransition();
        else
        {
            ApplyMouseMode();
        }
    }

    private void ApplyMouseMode()
    {
        if (!mouseModeReady) return;
        bool playing = windowFocused && !isPaused && !isGameOver && !isLevelComplete && !SceneFadeTransition.IsTransitioning;
        var mode = playing ? CursorLockMode.Locked : CursorLockMode.None;
        if (Cursor.lockState != mode) Cursor.lockState = mode;
        if (Cursor.visible == playing) Cursor.visible = !playing;
        foreach (var saved in cameraInputs)
            if (saved.input != null && saved.input.enabled != (playing && saved.enabled))
                saved.input.enabled = playing && saved.enabled;
    }

    private void OnDisable()
    {
        foreach (var saved in cameraInputs)
            if (saved.input != null) saved.input.enabled = saved.enabled;
        if (!mouseModeReady) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate() { ApplyMouseMode(); }

    private void OnApplicationFocus(bool focused)
    {
        windowFocused = focused;
        ApplyMouseMode();
    }

    private void ResolveGameplayInput()
    {
        if (gameplayInput != null) return;
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            gameplayInput = root.GetComponentInChildren<PlayerInput>(true);
            if (gameplayInput != null) return;
        }
    }

    private void ResolveGameplayUI()
    {
        if (gameplayUI.Count > 0) return;
        var roots = gameObject.scene.GetRootGameObjects();
        if (inGameUI == null || inGameUI.scene != gameObject.scene)
        {
            inGameUI = null;
            foreach (var root in roots)
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != "InGame UI") continue;
                    inGameUI = child.gameObject;
                    break;
                }
                if (inGameUI != null) break;
            }
        }
        if (inGameUI != null)
        {
            gameplayUI.Add((inGameUI, inGameUI.activeSelf));
            return;
        }

        // Older levels keep these HUD elements directly below their canvas.
        foreach (var root in roots)
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == "ToolUI" || child.name == "SanitySlider" || child.name == "ObjectiveUI")
                    gameplayUI.Add((child.gameObject, child.gameObject.activeSelf));
    }

    private void SetGameplayUIVisible(bool visible)
    {
        ResolveGameplayUI();
        if (visible)
        {
            if (!gameplayUIHidden) return;
            foreach (var saved in gameplayUI)
                if (saved.ui != null) saved.ui.SetActive(saved.active);
            gameplayUIHidden = false;
            return;
        }
        if (gameplayUIHidden) return;
        for (int i = 0; i < gameplayUI.Count; i++)
        {
            var saved = gameplayUI[i];
            if (saved.ui == null) continue;
            gameplayUI[i] = (saved.ui, saved.ui.activeSelf);
            saved.ui.SetActive(false);
        }
        gameplayUIHidden = true;
    }

    internal void PrepareForSceneTransition()
    {
        SetGameplayUIVisible(false);
        ResolveGameplayInput();
        if (gameplayInput != null) gameplayInput.DeactivateInput();
        Time.timeScale = 0f;
        ApplyMouseMode();
    }

    internal void CompleteSceneTransition()
    {
        SetGameplayUIVisible(true);
        Time.timeScale = 1f;
        if (gameplayInput != null) gameplayInput.ActivateInput();
        ApplyMouseMode();
    }

    public void Update()
    {
        if (isGameOver || isLevelComplete || SceneFadeTransition.IsTransitioning)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (SceneFadeTransition.IsTransitioning || (pauseSlide != null && pauseSlide.IsAnimating)) return;
        if (isPaused) { ResumeGame(); return; }
        isPaused = true;
        Time.timeScale = 0f;
        if (gameplayInput != null) gameplayInput.DeactivateInput();
        if (pauseSlide != null) pauseSlide.Show();
        else if (pausePanel != null) pausePanel.SetActive(true);
        ApplyMouseMode();
    }

    public void ResumeGame()
    {
        if (isGameOver || isLevelComplete || SceneFadeTransition.IsTransitioning) return;
        MenuFeedbackAudio.PlayClick();
        if (pauseSlide != null) pauseSlide.Hide(CompleteResume);
        else CompleteResume();
    }

    private void CompleteResume()
    {
        if (isGameOver || isLevelComplete || SceneFadeTransition.IsTransitioning) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameplayInput != null) gameplayInput.ActivateInput();
        ApplyMouseMode();
    }

    public void GameOver()
    {
        if (isGameOver || isLevelComplete) return;
        isGameOver = true;
        isPaused = false;
        ApplyMouseMode();
        if (pauseSlide != null) pauseSlide.HideImmediate();
        else if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        var input = FindAnyObjectByType<PlayerInput>();
        if (input != null) input.DeactivateInput();
        foreach (var dust in FindObjectsByType<TrashDustAudio>()) dust.enabled = false;
        if (gameOverSequence == null) gameOverSequence = GetComponent<GameOverSequence>();
        if (gameOverSequence == null) gameOverSequence = gameObject.AddComponent<GameOverSequence>();
        gameOverSequence.Show(mainMenuSceneName);
    }

    // ‡√’¬°‡¡◊ËÕ®∫¥Ë“π
    public void LevelComplete()
    {
        if (isGameOver || isLevelComplete)
            return;

        isLevelComplete = true;
        MenuFeedbackAudio.PlayLevelComplete();
        isPaused = false;

        if (pauseSlide != null) pauseSlide.HideImmediate();
        else if (pausePanel != null) pausePanel.SetActive(false);

        PrepareForSceneTransition();
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);

        ApplyMouseMode();

        Debug.Log("Level Complete");
    }

    // ªÿË¡‰ª¥Ë“π∂—¥‰ª
    public void GoToNextLevel()
    {
        if (SceneFadeTransition.IsTransitioning) return;
        MenuFeedbackAudio.PlayClick();
        SceneFadeTransition.LoadGameplayScene(nextLevelSceneName);
    }

    public void GoToMainMenu()
    {
        if (SceneFadeTransition.IsTransitioning) return;
        MenuFeedbackAudio.PlayClick();
        SceneFadeTransition.LoadScene(mainMenuSceneName);
    }
}
