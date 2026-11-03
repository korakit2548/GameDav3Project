using System;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameOverChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    private static Type sequenceType;
    private static Component runtimeSequence;
    private static double runtimeStart;
    private static int phase;
    private static bool checkedFade;

    public static void Run()
    {
        try
        {
            sequenceType = Type.GetType("GameOverSequence, Assembly-CSharp");
            Check("Shared game-over sequence exists", sequenceType != null);
            var author = Type.GetType("GameOverScreenEditor, Assembly-CSharp-Editor");
            if (author != null) author.GetMethod("EnsurePrefab").Invoke(null, null);
            EditableLayout();
            if (sequenceType != null) Behavior();
            if (failures != 0) { Complete(); return; }
            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(menu, "Assets/GameOverTestMenu.unity");
            var gameplay = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(gameplay, "Assets/GameOverTestGameplay.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/GameOverTestGameplay.unity", true), new EditorBuildSettingsScene("Assets/GameOverTestMenu.unity", true) };
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); Complete(); }
    }

    private static void Behavior()
    {
        var root = new GameObject("Game-over fixture");
        root.AddComponent<AudioListener>();
        var ambience = root.AddComponent<AudioSource>();
        ambience.clip = AudioClip.Create("Ambient fixture", 44100, 1, 44100, false);
        ambience.volume = 0.4f;
        ambience.loop = true;
        ambience.Play();
        var sequence = root.AddComponent(sequenceType);
        Call(sequence, "Show", "GameOverTestMenu");
        var canvas = GameObject.Find("Game Over Screen").GetComponent<Canvas>();
        var black = GameObject.Find("Game Over Fade").GetComponent<Image>();
        var menu = GameObject.Find("Game Over Menu").GetComponent<CanvasGroup>();
        Check("Death freezes gameplay and unlocks the cursor", Time.timeScale == 0f && Cursor.lockState == CursorLockMode.None && Cursor.visible);
        Check("Game-over covers damage and all HUD canvases", canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder > 100 && canvas.GetComponent<GraphicRaycaster>() != null);
        Check("Fade begins transparently with hidden non-clickable controls", black.color.a == 0f && menu.alpha == 0f && !menu.interactable && !menu.blocksRaycasts);
        Call(sequence, "Advance", 1f);
        Check("The world fades gradually before showing controls", black.color.a > 0.1f && black.color.a < 0.9f && menu.alpha == 0f);
        Check("Existing atmosphere fades with the picture", ambience.volume > 0f && ambience.volume < 0.4f);
        float previous = black.color.a;
        Call(sequence, "Show", "WrongScene");
        Check("Repeated death events do not restart the fade", black.color.a == previous && UnityEngine.Object.FindObjectsByType<Canvas>().Length == 1);
        Call(sequence, "Advance", 1f);
        Check("The background reaches completely opaque black", black.color == Color.black && menu.alpha == 0f);
        Check("House audio stops at full black", !ambience.isPlaying);
        Call(sequence, "Advance", 0.8f);
        Check("Buttons appear after the full black fade and become usable", menu.alpha == 1f && menu.interactable && menu.blocksRaycasts);
        var title = GameObject.Find("Game Over Title").GetComponent<TMP_Text>();
        Check("A large Game Over title uses the requested game font", title.text == "Game Over" && title.fontSize >= 100f && title.font != null && title.font.name == "Iannnnn-BAT-Regular SDF");
        var buttons = menu.GetComponentsInChildren<Button>();
        Check("Two choices use English labels", buttons.Length == 2 && buttons[0].GetComponentInChildren<TMP_Text>().text == "Restart" && buttons[1].GetComponentInChildren<TMP_Text>().text == "Main Menu");
        var sound = GameObject.Find("Game Over Audio").GetComponent<AudioSource>();
        Check("Death has a dedicated audible non-looping 2D sting", sound.clip != null && sound.clip.length >= 2f && sound.clip.length <= 5f && !sound.loop && sound.spatialBlend == 0f && sound.ignoreListenerPause && sound.volume >= 0.5f);
        Call(sequence, "OnDestroy");
        Check("Destroying the owner removes the standalone screen", canvas == null);
        UnityEngine.Object.DestroyImmediate(ambience.clip);
        UnityEngine.Object.DestroyImmediate(root);
        Time.timeScale = 1f;
    }

    private static void EditableLayout()
    {
        var template = Resources.Load<GameObject>("GameOver/GameOverScreen");
        Check("An editable GameOverScreen prefab is available", template != null);
        var viewType = Type.GetType("GameOverScreenView, Assembly-CSharp");
        Check("The prefab uses serialized references for the editable controls", viewType != null && template != null && template.GetComponent(viewType) != null);
        if (template == null || viewType == null || sequenceType == null) return;
        var custom = UnityEngine.Object.Instantiate(template);
        var title = custom.GetComponentsInChildren<TMP_Text>(true)[0];
        title.name = "My renamed heading";
        title.text = "YOU DIED";
        title.color = new Color(0.2f, 0.6f, 0.8f);
        title.rectTransform.anchoredPosition = new Vector2(-120f, 210f);
        var restart = custom.GetComponentsInChildren<Button>(true)[0];
        ((RectTransform)restart.transform).anchoredPosition = new Vector2(60f, -200f);
        var customPrefab = PrefabUtility.SaveAsPrefabAsset(custom, "Assets/CustomGameOverScreen.prefab");
        UnityEngine.Object.DestroyImmediate(custom);
        var owner = new GameObject("Artist layout fixture");
        var sequence = owner.AddComponent(sequenceType);
        var settings = new SerializedObject(sequence);
        var slot = settings.FindProperty("screenPrefab");
        Check("A custom game-over layout can be assigned", slot != null);
        if (slot == null) { UnityEngine.Object.DestroyImmediate(owner); return; }
        slot.objectReferenceValue = customPrefab;
        settings.ApplyModifiedPropertiesWithoutUndo();
        Call(sequence, "Show", "GameOverTestMenu");
        var visibleTitle = GameObject.Find("My renamed heading").GetComponent<TMP_Text>();
        Check("Gameplay preserves the artist's heading, color, and position", visibleTitle.text == "YOU DIED" && visibleTitle.color == new Color(0.2f, 0.6f, 0.8f) && visibleTitle.rectTransform.anchoredPosition == new Vector2(-120f, 210f));
        var visibleRestart = GameObject.Find("Game Over Screen").GetComponentsInChildren<Button>(true)[0];
        Check("Gameplay preserves the artist's button positions", ((RectTransform)visibleRestart.transform).anchoredPosition == new Vector2(60f, -200f));
        Call(sequence, "Advance", 3f);
        Check("An edited layout still fades in and enables both controls", GameObject.Find("Game Over Menu").GetComponent<CanvasGroup>().interactable && GameObject.Find("Game Over Screen").GetComponentsInChildren<Button>().Length == 2);
        Call(sequence, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(owner);
        Time.timeScale = 1f;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        runtimeStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += RuntimeChecks;
    }

    private static Component CreateSequence()
    {
        var owner = new GameObject("Runtime game-over owner");
        owner.AddComponent<AudioListener>();
        var sequence = owner.AddComponent(sequenceType);
        Call(sequence, "Show", "GameOverTestMenu");
        return sequence;
    }

    private static void RuntimeChecks()
    {
        try
        {
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (Time.frameCount < 3) return;
            if (EditorApplication.timeSinceStartup - runtimeStart > 25d) throw new Exception("Runtime test timed out at phase " + phase);
            if (phase == 0)
            {
                runtimeSequence = CreateSequence();
                Check("The game-over sound actually plays while gameplay is frozen", GameObject.Find("Game Over Audio").GetComponent<AudioSource>().isPlaying && Time.timeScale == 0f);
                runtimeStart = EditorApplication.timeSinceStartup;
                phase = 1;
            }
            else if (phase == 1)
            {
                double elapsed = EditorApplication.timeSinceStartup - runtimeStart;
                if (!checkedFade && elapsed >= 0.7d)
                {
                    var image = GameObject.Find("Game Over Fade").GetComponent<Image>();
                    Check("Real frame updates advance the fade at time scale zero", Time.timeScale == 0f && image.color.a > 0f && image.color.a < 1f);
                    checkedFade = true;
                }
                if (elapsed < 3d) return;
                Check("Real paused frames finish the screen transition", GameObject.Find("Game Over Fade").GetComponent<Image>().color == Color.black && GameObject.Find("Game Over Menu").GetComponent<CanvasGroup>().interactable);
                GameObject.Find("Restart Button").GetComponent<Button>().onClick.Invoke();
                phase = 2;
            }
            else if (phase == 2 && runtimeSequence == null)
            {
                Check("Restart button reloads the current scene and resumes game time", SceneManager.GetActiveScene().name == "GameOverTestGameplay" && Time.timeScale == 1f && GameObject.Find("Game Over Screen") == null);
                runtimeSequence = CreateSequence();
                Call(runtimeSequence, "Advance", 3f);
                GameObject.Find("Main Menu Button").GetComponent<Button>().onClick.Invoke();
                phase = 3;
            }
            else if (phase == 3 && SceneManager.GetActiveScene().name == "GameOverTestMenu")
            {
                Check("Main menu button loads the menu with time and cursor restored", Time.timeScale == 1f && Cursor.visible && Cursor.lockState == CursorLockMode.None);
                Check("Changing scenes leaves no stale game-over screen", GameObject.Find("Game Over Screen") == null);
                Complete();
            }
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); Complete(); }
    }

    private static void Call(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }
    private static void Check(string message, bool success)
    {
        Report.AppendLine((success ? "PASS: " : "FAIL: ") + message);
        if (!success) failures++;
    }
    private static void Complete()
    {
        EditorApplication.update -= RuntimeChecks;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        Time.timeScale = 1f;
        File.WriteAllText(Environment.GetEnvironmentVariable("GAME_OVER_CHECK_REPORT"), Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
