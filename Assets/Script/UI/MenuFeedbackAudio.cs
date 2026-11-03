using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuFeedbackAudio : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip completeClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.22f;
    [SerializeField, Range(0f, 1f)] private float completeVolume = 0.7f;
    private static MenuFeedbackAudio instance;
    private AudioSource clicks;
    private AudioSource hovers;
    private AudioSource completion;
    private bool quitting;

    private static MenuFeedbackAudio GetInstance()
    {
        if (instance == null)
        {
            var template = Resources.Load<GameObject>("MenuAudio/MenuFeedbackAudio");
            if (template != null && template.GetComponent<MenuFeedbackAudio>() != null)
            {
                var voice = Instantiate(template);
                voice.name = "Menu Feedback Audio";
                instance = voice.GetComponent<MenuFeedbackAudio>();
            }
            else instance = new GameObject("Menu Feedback Audio").AddComponent<MenuFeedbackAudio>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (clickClip == null) clickClip = Resources.Load<AudioClip>("MenuAudio/MenuClick");
        if (hoverClip == null) hoverClip = Resources.Load<AudioClip>("MenuAudio/MenuHover");
        if (completeClip == null) completeClip = Resources.Load<AudioClip>("MenuAudio/LevelComplete");
        clicks = CreateSource();
        hovers = CreateSource();
        completion = CreateSource();
    }

    private AudioSource CreateSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.priority = 32;
        return source;
    }

    public static void PlayClick()
    {
        var audio = GetInstance();
        if (audio.clickClip == null) return;
        audio.clicks.Stop();
        audio.clicks.PlayOneShot(audio.clickClip, audio.clickVolume);
    }

    public static void PlayHover()
    {
        var audio = GetInstance();
        if (audio.hoverClip == null) return;
        audio.hovers.Stop();
        audio.hovers.pitch = Random.Range(0.96f, 1.04f);
        audio.hovers.PlayOneShot(audio.hoverClip, audio.hoverVolume);
    }

    public static void PlayLevelComplete()
    {
        var audio = GetInstance();
        if (audio.completeClip != null) audio.completion.PlayOneShot(audio.completeClip, audio.completeVolume);
    }

    public static void QuitAfterClick()
    {
        var audio = GetInstance();
        if (audio.quitting) return;
        audio.quitting = true;
        PlayClick();
        audio.StartCoroutine(audio.QuitWhenClickFinishes());
    }

    private IEnumerator QuitWhenClickFinishes()
    {
        yield return new WaitForSecondsRealtime(clickClip != null ? Mathf.Min(clickClip.length, 0.25f) : 0.12f);
        Application.Quit();
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
