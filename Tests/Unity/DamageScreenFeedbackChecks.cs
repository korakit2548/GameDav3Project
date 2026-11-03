using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DamageScreenFeedbackChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    private static Type feedbackType;
    private static GameObject runtimeOwner;
    private static CanvasGroup runtimeGroup;
    private static double runtimeStart;
    private static bool checkedAttack;
    private static bool destroyedOwner;

    public static void Run()
    {
        try
        {
            feedbackType = Type.GetType("DamageScreenFeedback, Assembly-CSharp");
            Check("Damage feedback controller exists", feedbackType != null);
            if (feedbackType != null)
            {
                Behavior(feedbackType);
                ColorConfiguration(feedbackType);
            }
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); }
        if (failures == 0)
        {
            // This disposable project may enter play mode without resetting our runner.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            EditorApplication.EnterPlaymode();
        }
        else Complete();
    }

    private static void Complete()
    {
        EditorApplication.update -= CheckRuntimeFade;
        EditorApplication.update -= WaitForRuntimeFrames;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        Time.timeScale = 1f;
        File.WriteAllText(Environment.GetEnvironmentVariable("DAMAGE_FEEDBACK_CHECK_REPORT"), Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Behavior(Type type)
    {
        var root = new GameObject("Damage feedback fixture");
        var existingAudio = root.AddComponent<AudioSource>();
        existingAudio.playOnAwake = false;
        existingAudio.volume = 0.21f;
        var legacy = new GameObject("Legacy hurt image", typeof(RectTransform), typeof(Image));
        var child = new GameObject("Unrelated UI child", typeof(RectTransform), typeof(Image));
        child.transform.SetParent(legacy.transform, false);
        var feedback = root.AddComponent(type);
        Call(feedback, "Initialize", legacy);
        Call(feedback, "Initialize", legacy);

        Check("Replacing the legacy image preserves its object and UI children",
            legacy.activeSelf && !legacy.GetComponent<Image>().enabled && child.GetComponent<Image>().enabled);
        var overlays = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        var sources = root.GetComponentsInChildren<AudioSource>(true);
        Check("Initialization creates a single overlay and separate audio source", overlays.Length == 1 && sources.Length == 2);
        var group = overlays[0].GetComponent<CanvasGroup>();
        var image = overlays[0].GetComponentInChildren<RawImage>(true);
        Check("The transparent overlay does not intercept UI input", group != null && group.alpha == 0f &&
            !group.blocksRaycasts && !group.interactable && !image.raycastTarget &&
            overlays[0].GetComponentInChildren<GraphicRaycaster>(true) == null);
        var texture = image.texture as Texture2D;
        Check("The vignette keeps the center clear with red edges and black corners", texture != null &&
            texture.GetPixel(texture.width / 2, texture.height / 2).a < 0.01f &&
            texture.GetPixel(0, texture.height / 2).a > 0.5f &&
            texture.GetPixel(0, texture.height / 2).r > 0.1f && texture.GetPixel(0, 0).r < 0.05f);
        Color defaultEdge = texture.GetPixel(0, texture.height / 2);
        Color defaultCorner = texture.GetPixel(0, 0);
        Check("Default damage colors are dark blood-red with near-black outer corners", defaultEdge.r > 0.1f &&
            defaultEdge.r < 0.2f && defaultEdge.g < 0.02f && defaultEdge.b < 0.03f &&
            defaultCorner.r < 0.02f && defaultCorner.g < 0.02f && defaultCorner.b < 0.02f);

        Call(feedback, "ShowDamage", 1f);
        Check("Damage starts a smooth attack instead of appearing fully opaque", group.alpha < 0.05f);
        Step(feedback, 0.08f);
        Check("The damage attack reaches a visible peak", group.alpha > 0.5f && group.alpha < 0.95f);
        Step(feedback, 0.24f);
        float fadingAlpha = group.alpha;
        Call(feedback, "ShowDamage", 0.4f);
        Step(feedback, 0.08f);
        Check("Repeated damage reinforces the fade without an older timer hiding it", group.alpha >= fadingAlpha);
        Step(feedback, 0.4f);
        Check("A retriggered hit remains visible for its own fade", group.alpha > 0f);
        Step(feedback, 1f);
        Check("Damage always fades completely after its release", group.alpha == 0f);
        Call(feedback, "ShowDamage", 0f);
        Call(feedback, "ShowDamage", -2f);
        Step(feedback, 0.08f);
        Check("Zero and negative damage do not flash the screen", group.alpha == 0f);

        AudioSource shakeAudio = null;
        foreach (var candidate in sources) if (candidate != existingAudio) shakeAudio = candidate;
        Check("Shake audio is a non-looping 2D source", shakeAudio != null && !shakeAudio.loop &&
            !shakeAudio.playOnAwake && shakeAudio.spatialBlend == 0f);
        Call(feedback, "PlayShakeSound", false);
        var smallClip = shakeAudio.clip;
        Check("A small shake selects its shipped sound", smallClip != null && smallClip == Resources.Load<AudioClip>("DamageFeedback/ShakeSmall"));
        Call(feedback, "PlayShakeSound", true);
        var bigClip = shakeAudio.clip;
        Check("A strong shake immediately takes priority", bigClip != null && bigClip != smallClip &&
            bigClip == Resources.Load<AudioClip>("DamageFeedback/ShakeBig"));
        Call(feedback, "PlayShakeSound", false);
        Check("Rapid small shakes do not interrupt or stack over a strong shake", shakeAudio.clip == bigClip &&
            root.GetComponentsInChildren<AudioSource>(true).Length == 2);
        Check("Shake feedback leaves the player's other audio settings intact", existingAudio.volume == 0.21f && existingAudio.clip == null);
        ClipQuality(smallClip, "Small shake", 0.15f, 0.4f);
        ClipQuality(bigClip, "Strong shake", 0.4f, 0.9f);

        Call(feedback, "ShowDamage", 1f);
        Step(feedback, 0.08f);
        Call(feedback, "OnDisable");
        Check("Disabling feedback clears its visual and sound", group.alpha == 0f && !shakeAudio.isPlaying);
        // Edit-mode objects do not receive normal MonoBehaviour lifecycle callbacks.
        Call(feedback, "OnDestroy");
        Check("Owned-resource cleanup removes the standalone screen overlay", group == null);
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(legacy);
    }

    private static void ColorConfiguration(Type type)
    {
        var root = new GameObject("Configured vignette fixture");
        var feedback = root.AddComponent(type);
        var settings = new SerializedObject(feedback);
        var inner = settings.FindProperty("innerEdgeColor");
        var outer = settings.FindProperty("outerEdgeColor");
        Check("Both vignette edge colors are editable serialized settings", inner != null && outer != null);
        if (inner != null && outer != null)
        {
            inner.colorValue = new Color(0.12f, 0.24f, 0.36f, 0.8f);
            outer.colorValue = new Color(0.02f, 0.04f, 0.06f, 0.5f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            Call(feedback, "Initialize", new object[] { null });
            var image = UnityEngine.Object.FindAnyObjectByType<RawImage>();
            var texture = (Texture2D)image.texture;
            Color innerPixel = texture.GetPixel(texture.width / 2, 28);
            Color cornerPixel = texture.GetPixel(0, 0);
            Check("Configured inner and outer colors change the rendered vignette texture",
                Mathf.Abs(innerPixel.r - 0.12f) < 0.005f && Mathf.Abs(innerPixel.g - 0.24f) < 0.005f &&
                Mathf.Abs(innerPixel.b - 0.36f) < 0.005f && Mathf.Abs(cornerPixel.r - 0.02f) < 0.005f &&
                Mathf.Abs(cornerPixel.g - 0.04f) < 0.005f && Mathf.Abs(cornerPixel.b - 0.06f) < 0.005f &&
                Mathf.Abs(cornerPixel.a - 0.47f) < 0.005f);
            Check("Configured vignette colors preserve a transparent center",
                texture.GetPixel(texture.width / 2, texture.height / 2).a < 0.01f);
            Call(feedback, "OnDestroy");
        }
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        runtimeStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += WaitForRuntimeFrames;
    }

    private static void WaitForRuntimeFrames()
    {
        // The fresh editor can pause on its own search-index startup exception.
        // Unpause before creating the fixture, so actual test failures stay visible.
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (Time.frameCount < 3)
        {
            if (EditorApplication.timeSinceStartup - runtimeStart > 10d)
            {
                Check("The disposable editor starts a runtime player loop", false);
                Complete();
            }
            return;
        }
        EditorApplication.update -= WaitForRuntimeFrames;
        try
        {
            runtimeOwner = new GameObject("Paused damage fixture");
            runtimeOwner.AddComponent<AudioListener>();
            var feedback = runtimeOwner.AddComponent(feedbackType);
            Call(feedback, "Initialize", new object[] { null });
            runtimeGroup = UnityEngine.Object.FindAnyObjectByType<CanvasGroup>();
            Time.timeScale = 0f;
            Call(feedback, "ShowDamage", 1f);
            runtimeStart = EditorApplication.timeSinceStartup;
            EditorApplication.update += CheckRuntimeFade;
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); Complete(); }
    }

    private static void CheckRuntimeFade()
    {
        try
        {
            double duration = EditorApplication.timeSinceStartup - runtimeStart;
            if (!checkedAttack && duration >= 0.15d)
            {
                Check("A real Update displays damage while game time is paused", Time.timeScale == 0f && runtimeGroup.alpha > 0.1f);
                checkedAttack = true;
            }
            if (!destroyedOwner && duration >= 0.9d)
            {
                Check("A real Update finishes the fade while game time is paused", Time.timeScale == 0f && runtimeGroup.alpha == 0f);
                UnityEngine.Object.Destroy(runtimeOwner);
                destroyedOwner = true;
            }
            if (destroyedOwner && duration >= 1.1d)
            {
                Check("The runtime owner lifecycle removes its standalone overlay", runtimeGroup == null);
                Complete();
            }
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); Complete(); }
    }

    private static void ClipQuality(AudioClip clip, string name, float minDuration, float maxDuration)
    {
        Check(name + " clip is present with a short effect duration", clip != null && clip.length >= minDuration && clip.length <= maxDuration);
        if (clip == null) return;
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        float peak = 0f;
        double sum = 0d;
        foreach (float value in data) { peak = Mathf.Max(peak, Mathf.Abs(value)); sum += value * value; }
        Check(name + " is audible, finite, and not clipped", peak > 0.1f && peak < 0.99f && sum > 1d && !double.IsNaN(sum));
        Check(name + " fades out without a hard sample cut", Mathf.Abs(data[data.Length - 1]) < 0.002f);
    }

    private static void Step(object target, float seconds)
    {
        // Exercise the same unscaled step used by Update, including large frame gaps.
        Call(target, "UpdateFeedback", seconds);
    }
    private static void Call(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    }
    private static void Check(string message, bool result)
    {
        Report.AppendLine((result ? "PASS: " : "FAIL: ") + message);
        if (!result) failures++;
    }
}
