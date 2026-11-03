using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FootstepEffectsChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    private static Type effectsType;

    public static void Run()
    {
        try
        {
            effectsType = Type.GetType("PlayerFootsteps, Assembly-CSharp");
            Check("Walking effects component is available", effectsType != null);
            if (effectsType != null) CheckBehavior();
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); }
        string output = Environment.GetEnvironmentVariable("FOOTSTEP_CHECK_REPORT");
        if (string.IsNullOrEmpty(output)) output = "footstep-check-results.txt";
        File.WriteAllText(output, Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void CheckBehavior()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0f, -0.1f, 0f);
        ground.transform.localScale = new Vector3(1000f, 0.2f, 1000f);
        var player = new GameObject("Footstep test player");
        var body = player.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var collider = player.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.6f, 2f, 0.6f);
        player.transform.position = Vector3.up;
        var effects = player.AddComponent(effectsType);
        var material = new Material(Shader.Find("Sprites/Default"));
        Set(effects, "footprintMaterial", material);
        Set(effects, "stepDistance", 0.5f);
        Set(effects, "minimumStepInterval", 0f);
        Set(effects, "footprintLifetime", 2f);
        Set(effects, "footprintFadeDuration", 1f);
        Set(effects, "maxFootprints", 4);
        Invoke(effects, "Awake");
        Invoke(effects, "SetMoving", true);
        Tick(effects, 0.1f);
        Check("Standing with movement input produces no marks", ActiveMarks(effects) == 0);
        Move(player, effects, 0.6f);
        Check("Actual grounded walking emits both shoe prints", ActiveMarks(effects) == 2);
        var trail = (Component)Get(effects, "trail");
        var first = ActiveRenderers(trail)[0];
        Vector3 firstPosition = first.transform.position;
        Move(player, effects, 0.6f);
        Check("Walking emits another pair of shoe prints", ActiveMarks(effects) == 4);
        var prints = ActiveRenderers(trail);
        Check("Both feet leave distinct left and right marks", prints[0].transform.position.x * prints[1].transform.position.x < 0f);
        Check("Earlier print stays in world space", first.transform.position == firstPosition);
        Check("Marks sit just above the floor", prints[0].transform.position.y > 0f && prints[0].transform.position.y < 0.03f);
        var audio = player.GetComponentsInChildren<AudioSource>();
        Check("Step audio source does not autoplay or loop", audio.Length == 1 && !audio[0].playOnAwake && !audio[0].loop);
        Check("Walking starts a footstep sound", audio.Length == 1 && audio[0].isPlaying);
        Check("Original footstep audio assets import", Resources.LoadAll<AudioClip>("PlayerFootsteps/Audio").Length == 4);
        foreach (var clip in Resources.LoadAll<AudioClip>("PlayerFootsteps/Audio"))
        {
            var samples = new float[clip.samples * clip.channels];
            clip.LoadAudioData();
            bool nonzero = clip.GetData(samples, 0) && Array.Exists(samples, sample => Mathf.Abs(sample) > 0.01f);
            Check("Footstep clip has audible samples: " + clip.name, clip.length > 0.05f && clip.length < 0.4f && nonzero);
        }
        var texture = Resources.Load<Texture2D>("PlayerFootsteps/Shoeprint");
        Check("Shoe tread texture imports", texture != null && texture.width == 128 && texture.height == 256);
        for (int i = 0; i < 8; i++) Move(player, effects, 0.6f);
        Check("Footprint pool stays bounded", ActiveMarks(effects) == 4 && trail.GetComponentsInChildren<Renderer>(true).Length == 4);
        var savedPositions = Array.ConvertAll(ActiveRenderers(trail), mark => mark.transform.position);
        Invoke(effects, "SetMoving", false);
        Move(player, effects, 0.6f);
        var unmovedPrints = ActiveRenderers(trail);
        Check("Movement without player intent does not stamp", ActiveMarks(effects) == 4 &&
            Array.TrueForAll(Array.ConvertAll(unmovedPrints, mark => Array.Exists(savedPositions,
                position => position == mark.transform.position)), samePosition => samePosition));
        Invoke(trail, "FadeFootprints", 1.5f);
        var properties = new MaterialPropertyBlock();
        ActiveRenderers(trail)[0].GetPropertyBlock(properties);
        float alpha = properties.GetColor("_BaseColor").a;
        Check("Existing footprints fade while the player is stopped", alpha > 0f && alpha < 0.5f);
        Invoke(trail, "FadeFootprints", 0f);
        ActiveRenderers(trail)[0].GetPropertyBlock(properties);
        Check("Pause does not age footprints", Mathf.Approximately(alpha, properties.GetColor("_BaseColor").a));
        Invoke(trail, "FadeFootprints", 0.6f);
        Check("Old footprints disappear after their lifetime", ActiveMarks(effects) == 0);
        Invoke(effects, "SetMoving", true);
        for (int i = 0; i < 5; i++) Tick(effects, 0.1f);
        Check("Walking into a wall without displacement emits nothing", ActiveMarks(effects) == 0);
        player.transform.position += Vector3.forward * 0.6f;
        Tick(effects, 0f);
        Check("Paused movement does not emit", ActiveMarks(effects) == 0);
        player.transform.position += Vector3.forward * 100f;
        Tick(effects, 0.1f);
        Check("Teleport does not create a trail", ActiveMarks(effects) == 0);
        player.transform.position = new Vector3(0f, 1.2f, 0f);
        Invoke(effects, "SetMoving", false);
        Tick(effects, 0.1f);
        Invoke(effects, "SetMoving", true);
        Move(player, effects, 0.6f);
        Check("A short fall above the floor does not emit before landing", ActiveMarks(effects) == 0);
        player.transform.position += Vector3.up * 5f;
        Tick(effects, 0.1f);
        Move(player, effects, 0.6f);
        Check("Airborne movement does not leave floor prints", ActiveMarks(effects) == 0);
        player.transform.position = new Vector3(0f, 1f, 0f);
        Invoke(effects, "SetMoving", false);
        Tick(effects, 0.1f);
        Invoke(effects, "SetMoving", true);
        Move(player, effects, 0.6f);
        Check("Walking can create a fresh pair after earlier marks expire", ActiveMarks(effects) == 2);
        // Edit-mode fixtures invoke lifecycle callbacks explicitly, like Awake above.
        Invoke(effects, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(player);
        Check("Player cleanup removes its world-space trail", trail == null);
        UnityEngine.Object.DestroyImmediate(material);
        UnityEngine.Object.DestroyImmediate(ground);
    }

    private static Renderer[] ActiveRenderers(Component trail)
    {
        return Array.FindAll(trail.GetComponentsInChildren<Renderer>(), renderer => renderer.gameObject.activeSelf);
    }
    private static int ActiveMarks(Component effects)
    {
        var trail = Get(effects, "trail") as Component;
        return trail == null ? 0 : ActiveRenderers(trail).Length;
    }
    private static void Move(GameObject player, Component effects, float distance)
    {
        player.transform.position += Vector3.forward * distance;
        Tick(effects, 0.2f);
    }
    private static void Tick(Component effects, float delta)
    {
        Physics.SyncTransforms();
        Invoke(effects, "UpdateFootsteps", delta);
    }
    private static object Get(object target, string field) { return target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
    private static void Set(object target, string field, object value) { target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value); }
    private static void Invoke(object target, string method, params object[] values) { target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, values); }
    private static void Check(string name, bool passes)
    {
        Report.AppendLine((passes ? "PASS: " : "FAIL: ") + name);
        if (!passes) failures++;
    }
}
