using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TrashDustAudioChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;

    public static void Run()
    {
        try
        {
            var type = Type.GetType("TrashDustAudio, Assembly-CSharp");
            Check("Dust audio controller exists", type != null);
            if (type != null) Behavior(type);
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); }
        File.WriteAllText(Environment.GetEnvironmentVariable("DUST_AUDIO_CHECK_REPORT"), Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Behavior(Type type)
    {
        var clip = AudioClip.Create("Dust test tone", 44100, 1, 44100, false);
        var data = new float[44100];
        for (int i = 0; i < data.Length; i++) data[i] = Mathf.Sin(i * 0.05f) * 0.2f;
        clip.SetData(data, 0);
        var player = new GameObject("Player fixture");
        player.AddComponent<PlayerController>();
        var root = new GameObject("Dust fixture");
        var source = root.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 0.15f;
        source.loop = true;
        source.playOnAwake = false;
        var controller = root.AddComponent(type);
        Invoke(controller, "Awake");
        Invoke(controller, "OnEnable");
        Check("A clean scene starts without dust audio", !source.isPlaying);

        var trash = new GameObject("Trash fixture") { tag = "Trash" };
        Invoke(controller, "RefreshPlayback");
        var nearSource = trash.GetComponentInChildren<AudioSource>();
        Check("Nearby trash owns a spatial loop", nearSource != null && nearSource.isPlaying && nearSource.loop && nearSource.spatialBlend == 1f);
        Check("The global dust source stays stopped", !source.isPlaying);
        Invoke(controller, "RefreshPlayback");
        Check("Nearby trash reuses the configured clip and volume", nearSource != null && nearSource.clip == clip && nearSource.volume == 0.15f && trash.GetComponentsInChildren<AudioSource>().Length == 1);
        var farTrash = new GameObject("Far trash fixture") { tag = "Trash" };
        farTrash.transform.position = Vector3.right * 8f;
        Invoke(controller, "RefreshPlayback");
        var farSource = farTrash.GetComponentInChildren<AudioSource>();
        Check("Distant trash does not play", farSource == null || !farSource.isPlaying);
        player.transform.position = farTrash.transform.position;
        Invoke(controller, "RefreshPlayback");
        farSource = farTrash.GetComponentInChildren<AudioSource>();
        Check("Leaving a pile stops its loop", nearSource != null && !nearSource.isPlaying);
        Check("Approaching another pile starts that pile", farSource != null && farSource.isPlaying);
        player.transform.position = Vector3.right * 20f;
        Invoke(controller, "RefreshPlayback");
        Check("Being far from every pile is silent", !source.isPlaying && nearSource != null && !nearSource.isPlaying && farSource != null && !farSource.isPlaying);
        var otherRoot = new GameObject("Other ambience");
        var otherSource = otherRoot.AddComponent<AudioSource>();
        otherSource.clip = clip;
        otherSource.loop = true;
        otherSource.Play();

        UnityEngine.Object.DestroyImmediate(trash);
        UnityEngine.Object.DestroyImmediate(farTrash);
        Invoke(controller, "RefreshPlayback");
        Check("Cleaning the last trash stops dust audio", !source.isPlaying);
        Check("Other house ambience keeps playing", otherSource.isPlaying);

        var cursed = new GameObject("Cursed fixture") { tag = "CursedTrash" };
        player.transform.position = Vector3.zero;
        Invoke(controller, "RefreshPlayback");
        var cursedSource = cursed.GetComponentInChildren<AudioSource>();
        Check("Nearby cursed trash plays its own loop", cursedSource != null && cursedSource.isPlaying);
        cursed.SetActive(false);
        Invoke(controller, "RefreshPlayback");
        Check("Inactive trash does not keep playing", cursedSource == null || !cursedSource.isPlaying);
        cursed.SetActive(true);
        Invoke(controller, "RefreshPlayback");
        cursedSource = cursed.GetComponentInChildren<AudioSource>();
        Check("Returning to an active pile resumes playback", cursedSource != null && cursedSource.isPlaying);
        Invoke(controller, "OnDisable");
        Check("Disabling the controller stops all owned sources", !source.isPlaying && (cursedSource == null || !cursedSource.isPlaying));

        UnityEngine.Object.DestroyImmediate(cursed);
        UnityEngine.Object.DestroyImmediate(otherRoot);
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(player);
        UnityEngine.Object.DestroyImmediate(clip);
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
    private static void Check(string message, bool result)
    {
        Report.AppendLine((result ? "PASS: " : "FAIL: ") + message);
        if (!result) failures++;
    }
}
