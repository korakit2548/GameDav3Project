using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class RandomHouseAudioChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    private static Type effectType;

    public static void Run()
    {
        try
        {
            effectType = Type.GetType("RandomHouseAudioEffects, Assembly-CSharp");
            Check("Random house effect component exists", effectType != null);
            if (effectType != null) Behavior();
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); }
        File.WriteAllText(Environment.GetEnvironmentVariable("HOUSE_AUDIO_CHECK_REPORT"), Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Behavior()
    {
        AudioClip knock = LoadClip("DoorKnock1", 2f, 4f);
        AudioClip drop = LoadClip("ObjectDrop1", 2f, 4f);
        AudioClip scratch = LoadClip("WallScratch", 2f, 5f);
        AudioClip haunt = LoadClip("RareHaunt", 3f, 7f);
        string[] clipFields = { "doorKnockClip", "objectDropClip", "wallScratchClip", "rareHauntClip" };
        bool singleClips = Array.TrueForAll(clipFields, name => effectType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType == typeof(AudioClip));
        Check("Existing clip slots retain their serialized AudioClip fields", singleClips);
        if (!singleClips || knock == null || drop == null || scratch == null || haunt == null) return;

        var root = new GameObject("Random house audio fixture");
        var door = new GameObject("Door point").transform;
        var room = new GameObject("Drop point").transform;
        var wall = new GameObject("Wall point").transform;
        var corner = new GameObject("Haunt point").transform;
        door.position = new Vector3(-4f, 1.2f, -6f);
        room.position = new Vector3(3f, 0.3f, -10f);
        wall.position = new Vector3(-7f, 1.6f, -3f);
        corner.position = new Vector3(8f, 1f, 2f);
        var effects = root.AddComponent(effectType);
        var alternateDrop = AudioClip.Create("Alternate object drop", 4410, 1, 44100, false);
        var alternateData = new float[4410];
        for (int i = 0; i < alternateData.Length; i++) alternateData[i] = Mathf.Sin(i * 0.1f) * 0.2f;
        alternateDrop.SetData(alternateData, 0);
        var alternateHaunt = AudioClip.Create("Alternate rare haunt", 4410, 1, 44100, false);
        alternateHaunt.SetData(alternateData, 0);
        var secondRareSlot = effectType.GetField("rareHauntClip2", BindingFlags.Instance | BindingFlags.NonPublic);
        secondRareSlot?.SetValue(effects, alternateHaunt);
        var secondDropSlot = effectType.GetField("objectDropClip2", BindingFlags.Instance | BindingFlags.NonPublic);
        secondDropSlot?.SetValue(effects, alternateDrop);
        Set(effects, "doorKnockClip", knock);
        Set(effects, "objectDropClip", drop);
        Set(effects, "wallScratchClip", scratch);
        Set(effects, "rareHauntClip", haunt);
        Set(effects, "doorPoints", new[] { door });
        Set(effects, "dropPoints", new[] { room });
        Set(effects, "wallPoints", new[] { wall });
        Set(effects, "hauntPoints", new[] { corner });
        Invoke(effects, "ScheduleNextEvent");
        Check("Default quiet delay is between twenty and forty seconds", (float)Get(effects, "timeUntilEvent") >= 20f && (float)Get(effects, "timeUntilEvent") <= 40f);
        Set(effects, "minimumDelay", 0.5f);
        Set(effects, "maximumDelay", 0.5f);
        Invoke(effects, "Awake");
        Set(effects, "random", new FixedRandom(0));
        var source = root.GetComponentInChildren<AudioSource>();
        Check("Emitter is spatial, not looping, and does not autoplay", source != null && source.spatialBlend == 1f && !source.loop && !source.playOnAwake && !source.isPlaying);
        Check("Emitter preserves indoor attenuation and disables doppler", source.rolloffMode == AudioRolloffMode.Linear && Mathf.Approximately(source.minDistance, 3f) && Mathf.Approximately(source.maxDistance, 32f) && source.dopplerLevel == 0f);
        Invoke(effects, "UpdateEvents", 0.2f);
        Check("No effect plays before its delay", source.clip == null);
        float waiting = (float)Get(effects, "timeUntilEvent");
        float rareWaiting = (float)Get(effects, "timeUntilRareEvent");
        Invoke(effects, "UpdateEvents", 0f);
        Check("Paused time neither counts down nor plays", source.clip == null && Mathf.Approximately(waiting, (float)Get(effects, "timeUntilEvent")) && Mathf.Approximately(rareWaiting, (float)Get(effects, "timeUntilRareEvent")));
        var randomState = UnityEngine.Random.state;
        Invoke(effects, "UpdateEvents", 0.31f);
        Check("A due ordinary effect starts playing", source.clip != null && source.clip != haunt && source.clip != alternateHaunt && source.isPlaying);
        Check("Audio events preserve gameplay random state", UnityEngine.Random.state.Equals(randomState));
        var firstClip = source.clip;
        Vector3 firstPosition = source.transform.position;
        float firstPitch = source.pitch;
        float silentGap = (float)Get(effects, "timeUntilEvent");
        rareWaiting = (float)Get(effects, "timeUntilRareEvent");
        Invoke(effects, "UpdateEvents", 100f);
        Check("A playing effect is not interrupted or relocated", source.clip == firstClip && source.transform.position == firstPosition && source.pitch == firstPitch);
        Check("Playback does not consume the quiet gap before the next effect", Mathf.Approximately(silentGap, (float)Get(effects, "timeUntilEvent")));
        Check("Rare cooldown still counts gameplay time during playback", Mathf.Approximately(rareWaiting - 100f, (float)Get(effects, "timeUntilRareEvent")));

        Set(effects, "timeUntilRareEvent", 300f);
        Set(effects, "random", new FixedRandom(0));
        Fire(effects, source, 299f);
        Check("Even a winning rare roll cannot play before five minutes", source.clip != haunt && source.clip != alternateHaunt);
        Fire(effects, source, 1f);
        Check("A winning rare roll can play at the five-minute boundary", source.clip == haunt && source.transform.position == corner.position);
        Check("Rare playback restarts its five-minute cooldown", Mathf.Approximately((float)Get(effects, "timeUntilRareEvent"), 300f));
        Fire(effects, source, 299f);
        Check("A second rare event is blocked during its cooldown", source.clip != haunt && source.clip != alternateHaunt);
        Set(effects, "timeUntilRareEvent", 0f);
        Set(effects, "random", new FixedRandom(0.01));
        Fire(effects, source, 0.01f);
        Check("A one-percent roll does not trigger the rare event", source.clip != haunt && source.clip != alternateHaunt);
        Set(effects, "random", new FixedRandom(0.009));
        Fire(effects, source, 0.01f);
        Check("A roll just below one percent triggers the rare event", source.clip == haunt);
        Set(effects, "hauntPoints", null);
        Set(effects, "timeUntilRareEvent", 0f);
        Fire(effects, source, 0.01f);
        Check("Rare haunt falls back to room anchors", source.clip == haunt && source.transform.position == room.position);
        Set(effects, "hauntPoints", new[] { corner });

        int knockCount = 0, dropCount = 0, scratchCount = 0, hauntCount = 0;
        bool correctPositions = true, validLevels = true;
        Set(effects, "random", new System.Random(90210));
        for (int i = 0; i < 1500; i++)
        {
            Set(effects, "timeUntilRareEvent", 0f);
            Fire(effects, source, 0.01f);
            if (source.clip == knock) { knockCount++; correctPositions &= source.transform.position == door.position; }
            else if (source.clip == drop || source.clip == alternateDrop) { dropCount++; correctPositions &= source.transform.position == room.position; }
            else if (source.clip == scratch) { scratchCount++; correctPositions &= source.transform.position == wall.position; }
            else if (source.clip == haunt || source.clip == alternateHaunt) { hauntCount++; correctPositions &= source.transform.position == corner.position; }
            else correctPositions = false;
            validLevels &= source.volume >= 0.45f && source.volume <= 0.6f && source.pitch >= 0.94f && source.pitch <= 1.06f;
        }
        Check("All three ordinary categories appear roughly equally", knockCount > 400 && knockCount < 600 && dropCount > 400 && dropCount < 600 && scratchCount > 400 && scratchCount < 600);
        Check("Rare clip stays around one percent of eligible draws", hauntCount >= 5 && hauntCount <= 30);
        Check("Each category uses its own spatial anchors", correctPositions);
        Check("Pitch and loudness stay within the configured range", validLevels);
        Invoke(effects, "OnDisable");
        Check("Disabling the effect stops its audio", !source.isPlaying);

        Set(effects, "doorKnockClip", null);
        Set(effects, "objectDropClip", null);
        secondDropSlot?.SetValue(effects, null);
        Set(effects, "rareHauntClip", null);
        secondRareSlot?.SetValue(effects, null);
        Set(effects, "wallPoints", new Transform[] { null });
        Set(effects, "random", new System.Random(4));
        bool scratchOnly = true;
        for (int i = 0; i < 12; i++)
        {
            Fire(effects, source, 1f);
            scratchOnly &= source.clip == scratch && source.transform.position == door.position;
        }
        Check("A single configured category always plays and scratch falls back to door anchors", scratchOnly);
        Set(effects, "doorPoints", null);
        Fire(effects, source, 1f);
        Check("Missing all relevant anchors uses the root position", source.clip == scratch && source.transform.position == root.transform.position);
        Set(effects, "wallScratchClip", null);
        source.Stop();
        source.clip = null;
        Invoke(effects, "UpdateEvents", 10f);
        Check("Missing clips stay silent without throwing", !source.isPlaying && source.clip == null);

        Set(effects, "objectDropClip", drop);
        secondDropSlot?.SetValue(effects, alternateDrop);
        Set(effects, "random", new System.Random(41));
        int firstDrops = 0, secondDrops = 0;
        for (int i = 0; i < 200; i++)
        {
            Fire(effects, source, 1f);
            if (source.clip == drop) firstDrops++;
            if (source.clip == alternateDrop) secondDrops++;
        }
        Check("Falling object events randomly use both recordings equally", firstDrops >= 75 && firstDrops <= 125 && secondDrops >= 75 && secondDrops <= 125);
        Check("The existing first drop recording stays assigned", (AudioClip)Get(effects, "objectDropClip") == drop);
        Set(effects, "objectDropClip", null);
        Fire(effects, source, 1f);
        Check("Only the second drop slot can be filled", source.isPlaying && source.clip == alternateDrop);
        Set(effects, "objectDropClip", drop);
        secondDropSlot?.SetValue(effects, null);
        Fire(effects, source, 1f);
        Check("Only the first drop slot can be filled", source.isPlaying && source.clip == drop);
        Set(effects, "objectDropClip", null);
        source.Stop();
        source.clip = null;
        Fire(effects, source, 1f);
        Check("Empty drop slots stay silent", !source.isPlaying && source.clip == null);

        Set(effects, "rareHauntClip", haunt);
        secondRareSlot?.SetValue(effects, alternateHaunt);
        Set(effects, "random", new WinningRareRandom(73));
        int firstHaunts = 0, secondHaunts = 0;
        for (int i = 0; i < 200; i++)
        {
            Set(effects, "timeUntilRareEvent", 0f);
            Fire(effects, source, 0.01f);
            if (source.clip == haunt) firstHaunts++;
            if (source.clip == alternateHaunt) secondHaunts++;
        }
        Check("A winning rare event randomly chooses either recording equally", firstHaunts >= 75 && firstHaunts <= 125 && secondHaunts >= 75 && secondHaunts <= 125);
        Check("The existing first rare recording stays assigned", (AudioClip)Get(effects, "rareHauntClip") == haunt);
        Set(effects, "rareHauntClip", null);
        Set(effects, "timeUntilRareEvent", 300f);
        Fire(effects, source, 299f);
        Check("The second rare slot also waits five minutes", !source.isPlaying);
        Fire(effects, source, 1f);
        Check("Only the second rare slot can be filled", source.isPlaying && source.clip == alternateHaunt);
        Set(effects, "rareHauntClip", haunt);
        Fire(effects, source, 299f);
        Check("Both rare recordings share the same five-minute cooldown", !source.isPlaying && Mathf.Approximately((float)Get(effects, "timeUntilRareEvent"), 1f));
        secondRareSlot?.SetValue(effects, null);
        Set(effects, "timeUntilRareEvent", 0f);
        Fire(effects, source, 0.01f);
        Check("Only the first rare slot can be filled", source.isPlaying && source.clip == haunt);
        Set(effects, "rareHauntClip", null);
        source.Stop();
        source.clip = null;
        Set(effects, "timeUntilRareEvent", 0f);
        Fire(effects, source, 0.01f);
        Check("Empty rare slots stay silent", !source.isPlaying && source.clip == null);
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(alternateDrop);
        UnityEngine.Object.DestroyImmediate(alternateHaunt);
        UnityEngine.Object.DestroyImmediate(door.gameObject);
        UnityEngine.Object.DestroyImmediate(room.gameObject);
        UnityEngine.Object.DestroyImmediate(wall.gameObject);
        UnityEngine.Object.DestroyImmediate(corner.gameObject);
    }

    private static void Fire(object effects, AudioSource source, float deltaTime)
    {
        source.Stop();
        Set(effects, "timeUntilEvent", 0f);
        Invoke(effects, "UpdateEvents", deltaTime);
    }

    private static AudioClip LoadClip(string name, float minimumLength, float maximumLength)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RandomHouseAudio/" + name + ".wav");
        bool audible = false;
        if (clip != null)
        {
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            audible = clip.GetData(data, 0) && Array.Exists(data, sample => Mathf.Abs(sample) > 0.02f);
        }
        Check("Effect clip imports with audio: " + name, clip != null && clip.length > minimumLength && clip.length < maximumLength && audible);
        return clip;
    }

    private sealed class WinningRareRandom : System.Random
    {
        public WinningRareRandom(int seed) : base(seed) { }
        public override double NextDouble() { return 0; }
    }

    private sealed class FixedRandom : System.Random
    {
        private readonly double value;
        public FixedRandom(double value) { this.value = value; }
        public override double NextDouble() { return value; }
        public override int Next(int maximum) { return Math.Min(maximum - 1, (int)(value * maximum)); }
    }

    private static object Get(object target, string field) { return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    private static void Set(object target, string field, object value) { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Invoke(object target, string method, params object[] values) { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, values); }
    private static void Check(string message, bool result) { Report.AppendLine((result ? "PASS: " : "FAIL: ") + message); if (!result) failures++; }
}
