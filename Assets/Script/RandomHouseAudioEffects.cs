using UnityEngine;

[DisallowMultipleComponent]
public sealed class RandomHouseAudioEffects : MonoBehaviour
{
    [Header("Random Timing")]
    [SerializeField, Min(0.1f)] private float minimumDelay = 20f;
    [SerializeField, Min(0.1f)] private float maximumDelay = 40f;
    [SerializeField, Range(0f, 1f)] private float minimumVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.6f;

    [Header("Door Knocks")]
    [SerializeField] private AudioClip doorKnockClip;
    [SerializeField] private Transform[] doorPoints;

    [Header("Falling Objects")]
    [SerializeField, InspectorName("Object Drop Clip 1")] private AudioClip objectDropClip;
    [SerializeField, InspectorName("Object Drop Clip 2")] private AudioClip objectDropClip2;
    [SerializeField] private Transform[] dropPoints;

    [Header("Wall Scratches")]
    [SerializeField] private AudioClip wallScratchClip;
    [SerializeField] private Transform[] wallPoints;

    [Header("Very Rare Haunt")]
    [SerializeField, InspectorName("Rare Haunt Clip 1")] private AudioClip rareHauntClip;
    [SerializeField, InspectorName("Rare Haunt Clip 2")] private AudioClip rareHauntClip2;
    [SerializeField] private Transform[] hauntPoints;

    // One percent of due events, with at least five minutes before the first
    // haunt and between haunts. This clock uses gameplay time, including playback.
    private const double RareEventChance = 0.01;
    private const float RareEventCooldown = 300f;

    private System.Random random = new System.Random();
    private AudioSource audioSource;
    private float timeUntilEvent;
    private float timeUntilRareEvent;

    private void Awake()
    {
        var emitter = new GameObject("Random House Effects");
        emitter.transform.SetParent(transform, false);
        audioSource = emitter.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 32f;
        audioSource.dopplerLevel = 0f;
        audioSource.priority = 120;
        timeUntilRareEvent = RareEventCooldown;
        ScheduleNextEvent();
    }

    private void Update() { UpdateEvents(Time.deltaTime); }

    private void UpdateEvents(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        timeUntilRareEvent = Mathf.Max(0f, timeUntilRareEvent - deltaTime);
        if (audioSource.isPlaying) return;
        timeUntilEvent -= deltaTime;
        if (timeUntilEvent > 0f) return;
        PlayRandomEvent();
        ScheduleNextEvent();
    }

    private void ScheduleNextEvent()
    {
        float minimum = Mathf.Max(0.1f, minimumDelay);
        float maximum = Mathf.Max(minimum, maximumDelay);
        timeUntilEvent = minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    private void PlayRandomEvent()
    {
        AudioClip clip;
        Transform point;
        bool hasRareHaunts = rareHauntClip != null || rareHauntClip2 != null;
        if (hasRareHaunts && timeUntilRareEvent <= 0f && random.NextDouble() < RareEventChance)
        {
            clip = ChooseRareHauntClip();
            point = ChoosePoint(hauntPoints);
            if (point == null) point = ChoosePoint(dropPoints);
            timeUntilRareEvent = RareEventCooldown;
        }
        else
        {
            bool hasDrops = objectDropClip != null || objectDropClip2 != null;
            int available = (doorKnockClip != null ? 1 : 0)
                + (hasDrops ? 1 : 0)
                + (wallScratchClip != null ? 1 : 0);
            if (available == 0) return;
            int category = random.Next(available);
            if (doorKnockClip != null && category-- == 0)
            {
                clip = doorKnockClip;
                point = ChoosePoint(doorPoints);
            }
            else if (hasDrops && category-- == 0)
            {
                clip = ChooseObjectDropClip();
                point = ChoosePoint(dropPoints);
            }
            else
            {
                clip = wallScratchClip;
                point = ChoosePoint(wallPoints);
                if (point == null) point = ChoosePoint(doorPoints);
            }
        }

        audioSource.transform.position = point != null ? point.position : transform.position;
        audioSource.clip = clip;
        audioSource.pitch = 0.94f + (float)random.NextDouble() * 0.12f;
        float quiet = Mathf.Clamp01(minimumVolume);
        float loud = Mathf.Clamp(maximumVolume, quiet, 1f);
        audioSource.volume = quiet + (float)random.NextDouble() * (loud - quiet);
        audioSource.Play();
    }

    private AudioClip ChooseObjectDropClip()
    {
        if (objectDropClip == null) return objectDropClip2;
        if (objectDropClip2 == null) return objectDropClip;
        return random.Next(2) == 0 ? objectDropClip : objectDropClip2;
    }

    private AudioClip ChooseRareHauntClip()
    {
        if (rareHauntClip == null) return rareHauntClip2;
        if (rareHauntClip2 == null) return rareHauntClip;
        return random.Next(2) == 0 ? rareHauntClip : rareHauntClip2;
    }

    private Transform ChoosePoint(Transform[] points)
    {
        if (points == null || points.Length == 0) return null;
        int start = random.Next(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[(start + i) % points.Length];
            if (point != null) return point;
        }
        return null;
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }
}
