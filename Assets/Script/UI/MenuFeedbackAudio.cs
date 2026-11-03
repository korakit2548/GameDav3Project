using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuFeedbackAudio : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip completeClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float completeVolume = 0.7f;
    private static MenuFeedbackAudio instance;
    private AudioSource clicks;
    private AudioSource completion;
    private bool quitting;

    private static MenuFeedbackAudio GetInstance()
    {
        if (instance == null)
            instance = new GameObject("Menu Feedback Audio").AddComponent<MenuFeedbackAudio>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (clickClip == null) clickClip = Resources.Load<AudioClip>("MenuAudio/MenuClick");
        if (completeClip == null) completeClip = Resources.Load<AudioClip>("MenuAudio/LevelComplete");
        clicks = CreateSource();
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
        if (audio.clickClip != null) audio.clicks.PlayOneShot(audio.clickClip, audio.clickVolume);
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
        yield return new WaitForSecondsRealtime(clickClip != null ? clickClip.length : 0.12f);
        Application.Quit();
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
