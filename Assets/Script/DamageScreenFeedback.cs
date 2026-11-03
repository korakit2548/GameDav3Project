using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DamageScreenFeedback : MonoBehaviour
{
    [Header("Damage Fade")]
    [SerializeField, Range(0f, 1f)] private float maximumOpacity = 0.8f;
    [SerializeField, Min(0.01f)] private float attackDuration = 0.08f;
    [SerializeField, Min(0f)] private float holdDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

    [Header("Damage Border Colors")]
    [SerializeField, InspectorName("Inner Edge Color")]
    private Color innerEdgeColor = new Color(0.22f, 0.004f, 0.01f, 1f);
    [SerializeField, InspectorName("Outer Edge Color")]
    private Color outerEdgeColor = new Color(0.005f, 0f, 0.002f, 1f);

    [Header("Camera Shake Sound")]
    [SerializeField] private AudioClip smallShakeClip;
    [SerializeField] private AudioClip bigShakeClip;
    [SerializeField, Range(0f, 1f)] private float smallShakeVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float bigShakeVolume = 0.65f;
    [SerializeField, Min(0.01f)] private float minimumSoundInterval = 0.12f;

    private GameObject overlayObject;
    private CanvasGroup overlayGroup;
    private Texture2D vignetteTexture;
    private AudioSource shakeAudio;
    private float elapsed;
    private float attackStartOpacity;
    private float peakOpacity;
    private bool isFading;
    private float nextSmallSoundTime = float.NegativeInfinity;
    private float nextBigSoundTime = float.NegativeInfinity;

    public void Initialize(GameObject legacyHurtUI)
    {
        // Hide only the retired visual; other children and scripts remain available.
        if (legacyHurtUI != null)
        {
            foreach (Graphic graphic in legacyHurtUI.GetComponents<Graphic>())
                graphic.enabled = false;
        }

        if (overlayGroup == null) CreateOverlay();
        if (shakeAudio == null) CreateAudioSource();
    }

    public void ShowDamage(float strength = 1f)
    {
        if (strength <= 0f || !isActiveAndEnabled) return;
        Initialize(null);
        float requestedOpacity = maximumOpacity * Mathf.Clamp01(strength);
        attackStartOpacity = overlayGroup.alpha;
        peakOpacity = isFading
            ? Mathf.Min(maximumOpacity, Mathf.Max(attackStartOpacity, peakOpacity) + requestedOpacity * 0.3f)
            : requestedOpacity;
        elapsed = 0f;
        isFading = true;
    }

    public void PlayShakeSound(bool strong)
    {
        if (!isActiveAndEnabled) return;
        Initialize(null);
        float now = Time.unscaledTime;
        if (now < (strong ? nextBigSoundTime : nextSmallSoundTime)) return;
        AudioClip clip = strong ? bigShakeClip : smallShakeClip;
        if (clip == null) return;

        // A single dedicated voice prevents rapid impulses from stacking rumble.
        // Strong hits may replace a small hit immediately, but not the reverse.
        shakeAudio.Stop();
        shakeAudio.clip = clip;
        shakeAudio.volume = strong ? bigShakeVolume : smallShakeVolume;
        shakeAudio.Play();
        float interval = Mathf.Max(0.01f, minimumSoundInterval);
        nextSmallSoundTime = now + (strong ? Mathf.Max(interval, clip.length * 0.65f) : interval);
        nextBigSoundTime = now + interval;
        if (!strong) nextBigSoundTime = now;
    }

    private void Update()
    {
        UpdateFeedback(Time.unscaledDeltaTime);
    }

    private void UpdateFeedback(float unscaledDeltaTime)
    {
        if (!isFading || overlayGroup == null) return;
        elapsed += Mathf.Max(0f, unscaledDeltaTime);
        float attack = Mathf.Max(0.01f, attackDuration);
        float releaseStart = attack + Mathf.Max(0f, holdDuration);
        float release = Mathf.Max(0.01f, fadeDuration);
        if (elapsed < attack)
            overlayGroup.alpha = Mathf.Lerp(attackStartOpacity, peakOpacity, Mathf.SmoothStep(0f, 1f, elapsed / attack));
        else if (elapsed < releaseStart)
            overlayGroup.alpha = peakOpacity;
        else if (elapsed < releaseStart + release)
            overlayGroup.alpha = peakOpacity * (1f - Mathf.SmoothStep(0f, 1f, (elapsed - releaseStart) / release));
        else
        {
            overlayGroup.alpha = 0f;
            peakOpacity = 0f;
            isFading = false;
        }
    }

    private void CreateOverlay()
    {
        // A scene-root canvas keeps player scale and rotation out of the screen effect.
        overlayObject = new GameObject("Damage Screen Feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(overlayObject, gameObject.scene);
        var canvas = overlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        overlayGroup = overlayObject.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        var visual = new GameObject("Red Black Vignette", typeof(RectTransform), typeof(RawImage));
        visual.transform.SetParent(overlayObject.transform, false);
        var rect = (RectTransform)visual.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = visual.GetComponent<RawImage>();
        image.raycastTarget = false;
        vignetteTexture = CreateVignetteTexture();
        image.texture = vignetteTexture;
    }

    private Texture2D CreateVignetteTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Procedural Damage Vignette",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = y / (size - 1f) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = x / (size - 1f) * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.05f, radius));
                float darkness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1.35f, radius));
                Color borderColor = Color.Lerp(innerEdgeColor, outerEdgeColor, darkness);
                borderColor.a *= edge * 0.94f;
                pixels[y * size + x] = borderColor;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private void CreateAudioSource()
    {
        if (smallShakeClip == null) smallShakeClip = Resources.Load<AudioClip>("DamageFeedback/ShakeSmall");
        if (bigShakeClip == null) bigShakeClip = Resources.Load<AudioClip>("DamageFeedback/ShakeBig");
        var audioObject = new GameObject("Camera Shake Audio");
        audioObject.transform.SetParent(transform, false);
        shakeAudio = audioObject.AddComponent<AudioSource>();
        shakeAudio.playOnAwake = false;
        shakeAudio.loop = false;
        shakeAudio.spatialBlend = 0f;
    }

    private void OnDisable()
    {
        isFading = false;
        peakOpacity = 0f;
        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (shakeAudio != null) shakeAudio.Stop();
        nextSmallSoundTime = float.NegativeInfinity;
        nextBigSoundTime = float.NegativeInfinity;
    }

    private void OnDestroy()
    {
        if (overlayObject != null)
        {
            if (Application.isPlaying) Destroy(overlayObject);
            else DestroyImmediate(overlayObject);
        }
        if (vignetteTexture != null)
        {
            if (Application.isPlaying) Destroy(vignetteTexture);
            else DestroyImmediate(vignetteTexture);
        }
    }
}
