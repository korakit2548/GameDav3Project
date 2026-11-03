using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public sealed class GameOverSequence : MonoBehaviour
{
    [Header("Game Over Fade")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 2f;
    [SerializeField, Min(0.01f)] private float menuFadeDuration = 0.7f;
    [SerializeField] private TMP_FontAsset font;
    [Header("Editable UI")]
    [SerializeField] private GameObject screenPrefab;
    [Header("Game Over Sound")]
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField, Range(0f, 1f)] private float gameOverVolume = 0.8f;

    private struct AmbientVoice
    {
        public AudioSource source;
        public float volume;
    }
    private readonly List<AmbientVoice> ambience = new List<AmbientVoice>();
    private GameObject screen;
    private Image black;
    private CanvasGroup menu;
    private AudioSource deathAudio;
    private string restartScene;
    private string mainMenuScene;
    private float elapsed;
    private bool started;
    private bool soundStarted;

    public void Show(string menuSceneName = "MainMenu")
    {
        if (started) return;
        started = true;
        mainMenuScene = string.IsNullOrEmpty(menuSceneName) ? "MainMenu" : menuSceneName;
        restartScene = string.IsNullOrEmpty(gameObject.scene.path) ? gameObject.scene.name : gameObject.scene.path;
        foreach (var source in FindObjectsByType<AudioSource>())
        {
            if (source.gameObject.scene == gameObject.scene && source.isPlaying)
                ambience.Add(new AmbientVoice { source = source, volume = source.volume });
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CreateScreen();
        CreateSound();
    }

    private void Update() { Advance(Time.unscaledDeltaTime); }

    private void Advance(float unscaledDeltaTime)
    {
        if (!started || black == null) return;
        elapsed += Mathf.Max(0f, unscaledDeltaTime);
        float fadeTime = Mathf.Max(0.01f, fadeDuration);
        if (!soundStarted && elapsed >= fadeTime)
        {
            soundStarted = true;
            if (deathAudio != null && deathAudio.clip != null) deathAudio.Play();
        }
        float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeTime));
        black.color = new Color(0f, 0f, 0f, amount);
        foreach (var voice in ambience)
        {
            if (voice.source == null) continue;
            voice.source.volume = voice.volume * (1f - amount);
            if (amount >= 1f && voice.source.isPlaying) voice.source.Stop();
        }
        menu.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - fadeTime) / Mathf.Max(0.01f, menuFadeDuration)));
        menu.interactable = menu.alpha >= 1f;
        menu.blocksRaycasts = menu.interactable;
    }

    private void CreateScreen()
    {
        var template = screenPrefab != null ? screenPrefab : Resources.Load<GameObject>("GameOver/GameOverScreen");
        if (template != null)
        {
            screen = Instantiate(template);
            screen.name = "Game Over Screen";
            SceneManager.MoveGameObjectToScene(screen, gameObject.scene);
            var view = screen.GetComponent<GameOverScreenView>();
            if (view != null && view.IsConfigured)
            {
                black = view.FadeImage;
                menu = view.MenuGroup;
                black.color = new Color(0f, 0f, 0f, 0f);
                menu.alpha = 0f;
                menu.interactable = false;
                menu.blocksRaycasts = false;
                view.RestartButton.onClick.AddListener(RestartGame);
                view.MainMenuButton.onClick.AddListener(ReturnToMainMenu);
                screen.SetActive(true);
                EnsureEventSystem();
                return;
            }
            if (Application.isPlaying) Destroy(screen);
            else DestroyImmediate(screen);
        }
        CreateDefaultScreen();
        EnsureEventSystem();
    }

    private void CreateDefaultScreen()
    {
        if (font == null) font = TMP_Settings.defaultFontAsset;
        screen = new GameObject("Game Over Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(screen, gameObject.scene);
        var canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var background = new GameObject("Game Over Fade", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(screen.transform, false);
        Stretch((RectTransform)background.transform);
        black = background.GetComponent<Image>();
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = true;

        var content = new GameObject("Game Over Menu", typeof(RectTransform), typeof(CanvasGroup));
        content.transform.SetParent(screen.transform, false);
        Stretch((RectTransform)content.transform);
        menu = content.GetComponent<CanvasGroup>();
        menu.alpha = 0f;
        menu.interactable = false;
        menu.blocksRaycasts = false;

        var title = CreateText("Game Over Title", content.transform, "Game Over", 144f, new Color(0.7f, 0.08f, 0.1f));
        title.rectTransform.sizeDelta = new Vector2(1100f, 230f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 185f);
        title.characterSpacing = 5f;
        CreateButton("Restart Button", "Restart", -30f, RestartGame);
        CreateButton("Main Menu Button", "Main Menu", -145f, ReturnToMainMenu);
    }

    private void EnsureEventSystem()
    {
        if (Application.isPlaying && EventSystem.current == null)
        {
            var events = new GameObject("Game Over Event System", typeof(EventSystem));
            events.transform.SetParent(screen.transform, false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float size, Color color)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = color;
        label.raycastTarget = false;
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return label;
    }

    private void CreateButton(string objectName, string text, float y, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(menu.transform, false);
        var rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(460f, 86f);
        rect.anchoredPosition = new Vector2(0f, y);
        var image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        var border = buttonObject.GetComponent<Outline>();
        border.effectColor = new Color(0.27f, 0.12f, 0.13f, 1f);
        border.effectDistance = new Vector2(1f, -1f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.045f, 0.045f, 0.05f);
        colors.highlightedColor = new Color(0.19f, 0.045f, 0.055f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.3f, 0.055f, 0.065f);
        button.colors = colors;
        button.onClick.AddListener(action);
        var label = CreateText("Label", buttonObject.transform, text, 44f, new Color(0.9f, 0.88f, 0.86f));
        Stretch(label.rectTransform);
    }

    private void CreateSound()
    {
        var voice = new GameObject("Game Over Audio");
        voice.transform.SetParent(screen.transform, false);
        deathAudio = voice.AddComponent<AudioSource>();
        deathAudio.playOnAwake = false;
        deathAudio.loop = false;
        deathAudio.spatialBlend = 0f;
        deathAudio.ignoreListenerPause = true;
        deathAudio.priority = 16;
        deathAudio.volume = gameOverVolume;
        deathAudio.clip = gameOverClip != null ? gameOverClip : Resources.Load<AudioClip>("GameOver/GameOverSting");
    }

    public void RestartGame()
    {
        if (menu == null || !menu.interactable) return;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(restartScene);
    }

    public void ReturnToMainMenu()
    {
        if (menu == null || !menu.interactable) return;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (screen == null) return;
        if (Application.isPlaying) Destroy(screen);
        else DestroyImmediate(screen);
    }
}
