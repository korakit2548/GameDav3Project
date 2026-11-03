using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PausePanelSlideChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    private static Type type;
    private static GameObject owner;
    private static GameObject panel;
    private static RectTransform sign;
    private static Component slide;
    private static double start;
    private static int phase;
    private static bool closed;

    public static void Run()
    {
        try
        {
            type = Type.GetType("PausePanelSlide, Assembly-CSharp");
            Check("Pause sign animation exists", type != null);
            if (type == null) { Complete(); return; }
            Create();
            Call("HideImmediate");
            Check("The sign starts hidden outside the screen", !panel.activeSelf && Offscreen());
            Time.timeScale = 0f;
            Call("Show");
            Check("Opening uses dragon-studio-wooden on a dedicated 2D voice", WoodAudio() != null && WoodAudio().clip != null && WoodAudio().clip == Resources.Load<AudioClip>("PausePanel/dragon-studio-wooden") && !WoodAudio().loop && WoodAudio().spatialBlend == 0f && WoodAudio().ignoreListenerPause);
            Check("The requested recording keeps its natural pitch", WoodAudio() != null && WoodAudio().pitch == 1f);
            var requestedClip = Resources.Load<AudioClip>("PausePanel/dragon-studio-wooden");
            bool audibleStart = false;
            if (requestedClip != null)
            {
                var beginning = new float[Mathf.Min(requestedClip.samples, Mathf.RoundToInt(requestedClip.frequency * 0.25f)) * requestedClip.channels];
                if (requestedClip.GetData(beginning, 0))
                    foreach (float sample in beginning) if (Mathf.Abs(sample) > 0.005f) { audibleStart = true; break; }
            }
            Check("The selected recording is audible within the animation", audibleStart);
            var group = panel.GetComponent<CanvasGroup>();
            Check("Opening activates the panel but blocks early button clicks", panel.activeSelf && !group.interactable && group.blocksRaycasts);
            float hidden = sign.anchoredPosition.y;
            Call("Advance", 0.15f);
            Check("All sign content slides down gradually while time is paused", sign.anchoredPosition.y > 100f && sign.anchoredPosition.y < hidden && Time.timeScale == 0f);
            Call("Advance", 1f);
            Check("Opening preserves the exact authored layout", sign.anchoredPosition == new Vector2(17f, 100f) && group.interactable);
            Call("Hide", new Action(() => closed = true));
            Check("Closing uses the requested wooden recording too", WoodAudio() != null && WoodAudio().clip != null && WoodAudio().clip == requestedClip && WoodAudio().pitch == 1f);
            Call("Advance", 0.1f);
            Check("Continue moves the sign upwards before resuming", panel.activeSelf && sign.anchoredPosition.y > 100f && !closed && !group.interactable);
            Call("Advance", 1f);
            Check("The sign leaves the screen before closing completes", !panel.activeSelf && closed && Offscreen());
            closed = false;
            Call("Show"); Call("Advance", 1f);
            Call("Hide", new Action(() => closed = true));
            Call("HideImmediate"); Call("Advance", 1f);
            Check("Game Over can cancel a closing animation without resuming gameplay", !closed && !panel.activeSelf);
            Check("Cancelling the slide stops its sound", WoodAudio() != null && !WoodAudio().isPlaying);
            Check("Sign sounds use one private voice and preserve existing audio", owner.GetComponentsInChildren<AudioSource>(true).Length == 2 && owner.GetComponent<AudioSource>().volume == 0.23f);
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(panel);
            Time.timeScale = 1f;
            if (failures != 0) { Complete(); return; }
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.playModeStateChanged += OnPlay;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception e) { failures++; Report.AppendLine("ERROR " + e); Complete(); }
    }
    private static void Create()
    {
        owner = new GameObject("Pause animation owner");
        owner.AddComponent<AudioListener>();
        owner.AddComponent<AudioSource>().volume = 0.23f;
        panel = new GameObject("Pause panel fixture", typeof(RectTransform), typeof(Image));
        ((RectTransform)panel.transform).sizeDelta = new Vector2(1200f, 800f);
        sign = (RectTransform)new GameObject("Wooden sign and all labels", typeof(RectTransform)).transform;
        sign.SetParent(panel.transform, false);
        sign.sizeDelta = new Vector2(500f, 300f);
        sign.anchoredPosition = new Vector2(17f, 100f);
        slide = owner.AddComponent(type);
        Call("Initialize", panel);
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        phase = 0; start = EditorApplication.timeSinceStartup;
        EditorApplication.update += Runtime;
    }
    private static void Runtime()
    {
        try
        {
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (Time.frameCount < 3) return;
            if (EditorApplication.timeSinceStartup - start > 10d) throw new Exception("Paused animation timeout");
            if (phase == 0)
            {
                Create(); Call("HideImmediate"); Time.timeScale = 0f; Call("Show");
                Check("Downward wood sound actually plays while paused", WoodAudio() != null && WoodAudio().isPlaying && Time.timeScale == 0f);
                if (WoodAudio() != null)
                {
                    WoodAudio().timeSamples = 2100;
                    Call("Show");
                    Check("Repeated open requests do not restart or stack the sound", WoodAudio().timeSamples >= 2100 && owner.GetComponentsInChildren<AudioSource>(true).Length == 2);
                }
                start = EditorApplication.timeSinceStartup; phase = 1;
            }
            else if (phase == 1 && !(bool)type.GetProperty("IsAnimating").GetValue(slide))
            {
                Check("Real Update finishes entering with Time.timeScale zero", Time.timeScale == 0f && panel.activeSelf && sign.anchoredPosition == new Vector2(17f, 100f));
                closed = false; Call("Hide", new Action(() => closed = true));
                Check("Upward wood sound actually plays while paused", WoodAudio() != null && WoodAudio().isPlaying && WoodAudio().clip == Resources.Load<AudioClip>("PausePanel/dragon-studio-wooden") && Time.timeScale == 0f);
                start = EditorApplication.timeSinceStartup; phase = 2;
            }
            else if (phase == 2 && closed)
            {
                Check("Real paused frames hide the sign and invoke Continue once", Time.timeScale == 0f && !panel.activeSelf && closed);
                Check("Wood sound finishes with the animation", WoodAudio() != null && !WoodAudio().isPlaying);
                Complete();
            }
        }
        catch (Exception e) { failures++; Report.AppendLine("ERROR " + e); Complete(); }
    }
    private static bool Offscreen() => RectTransformUtility.CalculateRelativeRectTransformBounds(panel.transform, sign).min.y > ((RectTransform)panel.transform).rect.yMax;
    private static AudioSource WoodAudio() { var voice = owner.transform.Find("Pause Sign Wood Audio"); return voice != null ? voice.GetComponent<AudioSource>() : null; }
    private static void Call(string method, params object[] args) => type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(slide, args);
    private static void Check(string name, bool pass) { Report.AppendLine((pass ? "PASS " : "FAIL ") + name); if (!pass) failures++; }
    private static void Complete()
    {
        EditorApplication.update -= Runtime;
        EditorApplication.playModeStateChanged -= OnPlay;
        Time.timeScale = 1f;
        File.WriteAllText(Environment.GetEnvironmentVariable("PAUSE_SLIDE_CHECK_REPORT"), Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
