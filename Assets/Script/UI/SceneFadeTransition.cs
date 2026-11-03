using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Loads a scene behind a persistent black fade, including from paused gameplay.</summary>
[DisallowMultipleComponent]
public sealed class SceneFadeTransition : MonoBehaviour
{
    private const float FadeDuration = 0.7f;
    private static SceneFadeTransition instance;

    private readonly List<(EventSystem events, bool navigation)> blockedEvents =
        new List<(EventSystem, bool)>();
    private Image black;
    private bool gameplayDestinationRequested;
    private PlayerCam destinationPlayer;

    public static bool IsTransitioning => instance != null;

    public static void LoadScene(string sceneName)
    {
        Begin(sceneName, false);
    }

    public static void LoadGameplayScene(string sceneName)
    {
        Begin(sceneName, true);
    }

    private static void Begin(string sceneName, bool gameplayDestination)
    {
        if (IsTransitioning) return;

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("Cannot fade to scene '" + sceneName + "': it is not enabled in Build Settings.");
            RestoreMenuState();
            return;
        }

        var screen = new GameObject("Scene Fade Transition", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneFadeTransition));
        instance = screen.GetComponent<SceneFadeTransition>();
        instance.gameplayDestinationRequested = gameplayDestination;
        DontDestroyOnLoad(screen);
        instance.CreateOverlay();
        instance.BlockNavigation();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        instance.StartCoroutine(instance.Transition(sceneName));
    }

    private void CreateOverlay()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var cover = new GameObject("Scene Fade Black", typeof(RectTransform), typeof(Image));
        cover.transform.SetParent(transform, false);
        black = cover.GetComponent<Image>();
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = true;
        var rect = black.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator Transition(string sceneName)
    {
        yield return null;
        yield return FadeTo(1f);
        // Render one fully black frame before replacing the old paused scene.
        yield return null;
        if (!gameplayDestinationRequested) RestoreMenuState();

        AsyncOperation load = null;
        try
        {
            load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Could not load scene '" + sceneName + "': " + error.Message);
        }

        if (load != null)
        {
            while (!load.isDone) yield return null;
            // Let the destination initialise while it is still covered.
            yield return null;
        }

        yield return FadeTo(0f);
        Destroy(gameObject);
    }

    private IEnumerator FadeTo(float target)
    {
        float start = black.color.a;
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / FadeDuration)));
            black.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        black.color = new Color(0f, 0f, 0f, target);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BlockNavigation();
        if (gameplayDestinationRequested)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                destinationPlayer = root.GetComponentInChildren<PlayerCam>(true);
                if (destinationPlayer == null) continue;
                destinationPlayer.PrepareForSceneTransition();
                return;
            }
        }
        RestoreMenuState();
    }

    private void BlockNavigation()
    {
        // The overlay catches pointer clicks; also suppress keyboard/controller submit.
        foreach (var events in FindObjectsByType<EventSystem>())
        {
            if (blockedEvents.Exists(saved => saved.events == events)) continue;
            blockedEvents.Add((events, events.sendNavigationEvents));
            events.sendNavigationEvents = false;
            events.SetSelectedGameObject(null);
        }
    }

    private static void RestoreMenuState()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (var saved in blockedEvents)
        {
            if (saved.events != null) saved.events.sendNavigationEvents = saved.navigation;
        }
        if (instance != this) return;
        instance = null;
        if (destinationPlayer != null) destinationPlayer.CompleteSceneTransition();
        else RestoreMenuState();
    }
}
