using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CursedRoomChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures, phase, snapshot;
    private static float since;
    private static double started;
    private static PlayerController player;
    private static GameObject prefab;
    private static Room moving, reset, recurring;

    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged += OnPlay;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.EnterPlaymode();
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += Tick;
    }
    private static Room CreateRoom(string name)
    {
        var owner = new GameObject(name);
        owner.SetActive(false);
        var light = new GameObject(name + " light").AddComponent<Light>();
        light.transform.SetParent(owner.transform);
        light.intensity = 3.5f;
        light.color = Color.white;
        var room = owner.AddComponent<Room>();
        Set(room, "roomLight", light);
        Set(room, "cursedTrashPrefab", prefab);
        Set(room, "trashSpawnPoint", owner.transform);
        Set(room, "normalLightColor", Color.white);
        Set(room, "cursedDelay", 0.02f);
        Set(room, "cursedDuration", 0.18f);
        Set(room, "warningTime", 0.08f);
        Set(room, "damageInterval", 0.025f);
        Set(room, "instantDamageDelay", 0.05f);
        owner.SetActive(true);
        return room;
    }
    private static CursedTrash OwnedTrash(Room room)
    {
        foreach (var trash in UnityEngine.Object.FindObjectsByType<CursedTrash>(FindObjectsInactive.Include))
            if ((Room)Get(trash, "room") == room) return trash;
        return null;
    }
    private static void PickUp(Room room)
    {
        var trash = OwnedTrash(room);
        Check(room.name + " has a spawned bear linked to its room", trash != null);
        if (trash == null) return;
        var hold = trash.GetComponent<HoldDestroyableObject>();
        hold.BeginHold();
        hold.UpdateHold(0.02f);
        Check(room.name + " normalises immediately after actual hold pickup", room.IsNormal());
        var light = (Light)Get(room, "roomLight");
        Check(room.name + " restores normal light color and brightness", light.color == Color.white && Mathf.Approximately(light.intensity, 3.5f));
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (Time.frameCount < 3) return;
            if (EditorApplication.timeSinceStartup - started > 20d) throw new Exception("Room checks timed out at phase " + phase);
            Time.timeScale = 1f;
            if (phase == 0)
            {
                var actor = new GameObject("Moving player fixture");
                player = actor.AddComponent<PlayerController>();
                var body = actor.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.linearVelocity = Vector3.right;
                prefab = new GameObject("Bear trash template");
                prefab.SetActive(false);
                prefab.AddComponent<CursedTrash>();
                var hold = prefab.AddComponent<HoldDestroyableObject>();
                Set(hold, "destroyTime", 0.01f);
                Set(hold, "cleaningBar", prefab.AddComponent<CleaningBar>());
                moving = CreateRoom("Movement curse");
                moving.PlayerEnter();
                moving.SetWarning();
                moving.StartCursedImmediately();
                since = Time.time; phase = 1;
            }
            else if (phase == 1 && player.damageCalls > 0)
            {
                Check("Movement curse deals damage before cleaning", player.damageCalls > 0 && moving.IsCursed());
                PickUp(moving);
                Check("Cleaning cancels the movement damage coroutine", Get(moving, "cursedDamageCoroutine") == null);
                Check("Cleaning cancels all pending instant damage", Get(moving, "instantDamageCoroutine") == null);
                Check("Cleaning clears warning and immediate-curse flags", !(bool)Get(moving, "isWarning") && !(bool)Get(moving, "CursedImmediately"));
                snapshot = player.damageCalls;
                since = Time.time; phase = 2;
            }
            else if (phase == 2 && Time.time - since > 0.4f)
            {
                Check("No damage continues after picking up the bear", player.damageCalls == snapshot);
                Check("Old timers never turn the cleaned room red again", moving.IsNormal() && ((Light)Get(moving, "roomLight")).color == Color.white);
                Check("Old timers never respawn a cleaned cursed pile", OwnedTrash(moving) == null);
                moving.SetWarning();
                PickUp(moving);
                reset = CreateRoom("Warning reset"); reset.SetWarning(); reset.SetNormal();
                recurring = CreateRoom("Repeating curse");
                recurring.SetWarning(); recurring.SetCursed(); recurring.PlayerEnter();
                snapshot = player.damageCalls;
                since = Time.time; phase = 3;
            }
            else if (phase == 3 && Time.time - since > 0.12f)
            {
                Check("Repeating curse is active before its bear is cleaned", player.damageCalls > snapshot);
                PickUp(recurring);
                Check("Repeat damage coroutine is stopped by pickup", Get(recurring, "instantDamageCoroutine") == null);
                snapshot = player.damageCalls;
                since = Time.time; phase = 4;
            }
            else if (phase == 4 && Time.time - since > 0.18f)
            {
                Check("Repeating damage stays stopped after its deadline", player.damageCalls == snapshot);
                Check("Cleaning during warning prevents its delayed curse", moving.IsNormal());
                Check("SetNormal cancels any pending warning timer", reset.IsNormal());
                moving.SetWarning();
                since = Time.time; phase = 5;
            }
            else if (phase == 5 && Time.time - since > 0.12f)
            {
                Check("A genuinely new curse can still run after cleaning", moving.IsCursed() && OwnedTrash(moving) != null);
                PickUp(moving);
                Complete();
            }
        }
        catch (Exception error) { failures++; Report.AppendLine("ERROR " + error); Complete(); }
    }
    private static object Get(object owner, string field) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    private static void Check(string label, bool pass) { Report.AppendLine((pass ? "PASS " : "FAIL ") + label); if (!pass) failures++; }
    private static void Complete()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlay;
        Time.timeScale = 1f;
        File.WriteAllText(Environment.GetEnvironmentVariable("CURSED_ROOM_CHECK_REPORT"), Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
