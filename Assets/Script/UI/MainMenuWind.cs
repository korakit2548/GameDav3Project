using UnityEngine;

[DisallowMultipleComponent]
public sealed class MainMenuWind : MonoBehaviour
{
    [SerializeField] private AudioClip windClip;
    [SerializeField, Range(0f, 1f)] private float windVolume = 0.28f;
    [SerializeField] private Vector2 silentInterval = new Vector2(8f, 16f);
    private AudioSource source;
    private float remaining;
    private float elapsed;
    private float gustDuration;
    private bool gustPlaying;

    private void Awake()
    {
        if (windClip == null) windClip = Resources.Load<AudioClip>("MenuAudio/WindGust");
        var child = new GameObject("Intermittent Menu Wind");
        child.transform.SetParent(transform, false);
        source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.clip = windClip;
        source.volume = 0f;
        remaining = Random.Range(2f, 4f);
    }

    private void Update() { Advance(Time.unscaledDeltaTime); }

    private void Advance(float deltaTime)
    {
        if (source == null || windClip == null || deltaTime <= 0f) return;
        if (!gustPlaying)
        {
            remaining -= deltaTime;
            if (remaining > 0f) return;
            source.pitch = Random.Range(0.93f, 1.05f);
            source.panStereo = Random.Range(-0.3f, 0.3f);
            source.volume = 0f;
            gustDuration = windClip.length / source.pitch;
            elapsed = 0f;
            gustPlaying = true;
            source.Play();
        }
        elapsed += deltaTime;
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1f));
        float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((gustDuration - elapsed) / 1.3f));
        source.volume = windVolume * fadeIn * fadeOut;
        if (elapsed < gustDuration) return;
        source.Stop();
        gustPlaying = false;
        float minimum = Mathf.Max(1f, silentInterval.x);
        remaining = Random.Range(minimum, Mathf.Max(minimum, silentInterval.y));
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
        gustPlaying = false;
        remaining = Mathf.Max(1f, silentInterval.x);
    }
}
