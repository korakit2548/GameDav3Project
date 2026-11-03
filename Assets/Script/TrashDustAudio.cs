using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class TrashDustAudio : MonoBehaviour
{
    private const float CheckInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float hearingDistance = 2f;
    [SerializeField] private Transform player;
    private readonly Dictionary<GameObject, AudioSource> pileSources = new Dictionary<GameObject, AudioSource>();
    private readonly HashSet<GameObject> activePiles = new HashSet<GameObject>();
    private readonly List<GameObject> removedPiles = new List<GameObject>();
    private AudioSource dustSource;
    private float timeUntilCheck;

    private void Awake()
    {
        dustSource = GetComponent<AudioSource>();
        dustSource.playOnAwake = false;
        dustSource.Stop();
    }

    private void OnEnable()
    {
        if (dustSource == null) dustSource = GetComponent<AudioSource>();
        RefreshPlayback();
        timeUntilCheck = CheckInterval;
    }

    private void Update()
    {
        timeUntilCheck -= Time.unscaledDeltaTime;
        if (timeUntilCheck > 0f) return;
        timeUntilCheck = CheckInterval;
        RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        // This source supplies the clip/settings; only the individual piles play it.
        if (dustSource.isPlaying) dustSource.Stop();
        if (player == null)
        {
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        activePiles.Clear();
        RefreshPiles(GameObject.FindGameObjectsWithTag("Trash"));
        RefreshPiles(GameObject.FindGameObjectsWithTag("CursedTrash"));
        removedPiles.Clear();
        foreach (var entry in pileSources)
        {
            if (entry.Key == null || !activePiles.Contains(entry.Key)) removedPiles.Add(entry.Key);
        }
        foreach (var pile in removedPiles)
        {
            RemoveSource(pileSources[pile]);
            pileSources.Remove(pile);
        }
    }

    private void RefreshPiles(GameObject[] piles)
    {
        foreach (var pile in piles)
        {
            activePiles.Add(pile);
            var surface = pile.GetComponentInChildren<Collider>();
            Vector3 closestPoint = surface != null && player != null
                ? surface.ClosestPoint(player.position) : pile.transform.position;
            bool nearby = player != null && dustSource.clip != null
                && (player.position - closestPoint).sqrMagnitude <= hearingDistance * hearingDistance;
            pileSources.TryGetValue(pile, out var source);
            if (!nearby)
            {
                if (source != null && source.isPlaying) source.Stop();
                continue;
            }
            if (source == null)
            {
                var voice = new GameObject("Trash Pile Audio");
                voice.transform.SetParent(pile.transform, false);
                if (surface != null) voice.transform.position = surface.bounds.center;
                source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                // The third-person camera/listener sits behind the player.
                // Player distance above controls audibility, independent of that offset.
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 6f;
                source.maxDistance = 25f;
                pileSources[pile] = source;
            }
            source.clip = dustSource.clip;
            source.volume = dustSource.volume;
            source.outputAudioMixerGroup = dustSource.outputAudioMixerGroup;
            if (!source.isPlaying) source.Play();
        }
    }

    private void OnDisable()
    {
        if (dustSource != null) dustSource.Stop();
        foreach (var source in pileSources.Values)
            if (source != null) source.Stop();
    }

    private void OnDestroy()
    {
        foreach (var source in pileSources.Values) RemoveSource(source);
        pileSources.Clear();
    }

    private static void RemoveSource(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        if (Application.isPlaying) Destroy(source.gameObject);
        else DestroyImmediate(source.gameObject);
    }
}
