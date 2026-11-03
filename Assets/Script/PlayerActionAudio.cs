using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerActionAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float cleaningVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float boxVolume = 0.7f;

    [Header("Action Clips")]
    [SerializeField] private AudioClip sweepClip;
    [SerializeField] private AudioClip wipeClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip placeClip;

    private AudioSource cleaningSource;
    private AudioSource boxSource;

    public static PlayerActionAudio GetOrCreate(GameObject player)
    {
        var audio = player.GetComponent<PlayerActionAudio>();
        return audio != null ? audio : player.AddComponent<PlayerActionAudio>();
    }

    private void Awake()
    {
        cleaningSource = CreateSource("Cleaning Audio");
        boxSource = CreateSource("Box Audio");
        if (sweepClip == null) sweepClip = Resources.Load<AudioClip>("PlayerActions/Audio/Sweep");
        if (wipeClip == null) wipeClip = Resources.Load<AudioClip>("PlayerActions/Audio/Wipe");
        if (pickupClip == null) pickupClip = Resources.Load<AudioClip>("PlayerActions/Audio/BoxPickup");
        if (placeClip == null) placeClip = Resources.Load<AudioClip>("PlayerActions/Audio/BoxPlace");
    }

    private AudioSource CreateSource(string sourceName)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.65f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 12f;
        return source;
    }

    public void PlayCleaning(CleaningAction action)
    {
        StopCleaning();
        cleaningSource.clip = action == CleaningAction.Sweeping ? sweepClip : wipeClip;
        if (cleaningSource.clip == null) return;
        cleaningSource.volume = cleaningVolume;
        cleaningSource.loop = true;
        cleaningSource.Play();
    }

    public void StopCleaning()
    {
        if (cleaningSource != null) cleaningSource.Stop();
    }

    public void PlayPickup() { PlayBox(pickupClip); }
    public void PlayPlace() { PlayBox(placeClip); }

    private void PlayBox(AudioClip clip)
    {
        if (clip == null) return;
        boxSource.pitch = Random.Range(0.96f, 1.04f);
        boxSource.PlayOneShot(clip, boxVolume);
    }

    private void OnDisable()
    {
        StopCleaning();
        if (boxSource != null) boxSource.Stop();
    }
}
