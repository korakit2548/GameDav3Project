using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PausePanelSlide : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float enterDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.35f;
    [Header("Wood Sign Sound")]
    [SerializeField] private AudioClip slideDownClip;
    [SerializeField] private AudioClip slideUpClip;
    [SerializeField, Range(0f, 1f)] private float woodVolume = 0.55f;
    private AudioSource signAudio;
    private readonly List<RectTransform> signs = new List<RectTransform>();
    private readonly List<Vector2> shownPositions = new List<Vector2>();
    private GameObject panel;
    private CanvasGroup group;
    private float hiddenOffset;
    private float offset;
    private float from;
    private float target;
    private float elapsed;
    private float duration;
    private Action onHidden;
    public bool IsAnimating { get; private set; }

    public void Initialize(GameObject pausePanel)
    {
        if (panel == pausePanel && group != null) return;
        panel = pausePanel;
        if (panel == null) return;
        MenuButtonHoverAudio.Bind(panel);
        ApplyButtonHoverStyle();
        group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        signs.Clear();
        shownPositions.Clear();
        var panelRect = panel.GetComponent<RectTransform>();
        float lowest = panelRect != null ? panelRect.rect.yMin : -540f;
        foreach (Transform child in panel.transform)
        {
            if (!(child is RectTransform rect)) continue;
            signs.Add(rect);
            shownPositions.Add(rect.anchoredPosition);
            lowest = Mathf.Min(lowest, RectTransformUtility.CalculateRelativeRectTransformBounds(panel.transform, rect).min.y);
        }
        hiddenOffset = (panelRect != null ? panelRect.rect.yMax : 540f) - lowest + 80f;
        hiddenOffset = Mathf.Max(300f, hiddenOffset);
    }

    public void Show()
    {
        if (panel == null || (panel.activeSelf && target == 0f)) return;
        ApplyButtonHoverStyle();
        onHidden = null;
        panel.SetActive(true);
        Begin(0f, enterDuration);
    }

    private void ApplyButtonHoverStyle()
    {
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            var hover = button.GetComponent<PauseButtonHoverGraphic>();
            var image = button.targetGraphic as Image;
            if (hover == null && image != null && image.color.a <= 0.01f)
            {
                for (Transform parent = button.transform.parent; parent != null; parent = parent.parent)
                {
                    Image artwork = null;
                    foreach (var candidate in parent.GetComponentsInChildren<Image>(true))
                    {
                        if (candidate.sprite == null || candidate.color.a <= 0.01f
                            || candidate.GetComponentInParent<Button>() != null) continue;
                        Vector3 center = button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
                        if (!candidate.rectTransform.rect.Contains(candidate.rectTransform.InverseTransformPoint(center))) continue;
                        artwork = candidate;
                        break;
                    }
                    if (artwork == null) continue;
                    hover = button.gameObject.AddComponent<PauseButtonHoverGraphic>();
                    hover.Configure(artwork);
                    break;
                }
            }
            bool maskedOverlay = hover != null && hover.Overlay != null;
            if (maskedOverlay) button.targetGraphic = hover.Overlay;
            if (button.targetGraphic == null) button.targetGraphic = button.GetComponent<Graphic>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = maskedOverlay ? Color.clear : Color.white;
            colors.highlightedColor = maskedOverlay ? new Color(0f, 0f, 0f, 0.59f)
                : new Color(0.4113207f, 0.4113207f, 0.4113207f, 1f);
            colors.pressedColor = maskedOverlay ? new Color(0f, 0f, 0f, 0.8f) : Color.black;
            colors.selectedColor = colors.highlightedColor;
            if (maskedOverlay) colors.disabledColor = Color.clear;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
        }
    }

    public void Hide(Action completed)
    {
        if (panel == null || !panel.activeSelf) { completed?.Invoke(); return; }
        if (IsAnimating && target == hiddenOffset) return;
        onHidden = completed;
        Begin(hiddenOffset, exitDuration);
    }

    public void HideImmediate()
    {
        onHidden = null;
        IsAnimating = false;
        StopSignSound();
        if (panel == null) return;
        SetOffset(hiddenOffset);
        group.interactable = false;
        group.blocksRaycasts = false;
        panel.SetActive(false);
    }

    private void Begin(float destination, float seconds)
    {
        from = offset;
        target = destination;
        elapsed = 0f;
        duration = Mathf.Max(0.01f, seconds);
        IsAnimating = true;
        group.interactable = false;
        group.blocksRaycasts = true;
        PlaySignSound(destination == 0f);
    }

    private void Update() { Advance(Time.unscaledDeltaTime); }

    private void Advance(float unscaledDeltaTime)
    {
        if (!IsAnimating || panel == null) return;
        elapsed += Mathf.Max(0f, unscaledDeltaTime);
        if (signAudio != null && signAudio.isPlaying)
            signAudio.volume = woodVolume * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - elapsed) / 0.06f));
        float progress = Mathf.Clamp01(elapsed / duration);
        float eased = target == 0f ? 1f - Mathf.Pow(1f - progress, 3f) : Mathf.SmoothStep(0f, 1f, progress);
        SetOffset(progress >= 1f ? target : Mathf.Lerp(from, target, eased));
        if (progress < 1f) return;
        IsAnimating = false;
        StopSignSound();
        if (target == 0f)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        else
        {
            panel.SetActive(false);
            group.blocksRaycasts = false;
            var completed = onHidden;
            onHidden = null;
            completed?.Invoke();
        }
    }

    private void SetOffset(float value)
    {
        offset = value;
        for (int i = 0; i < signs.Count; i++)
            if (signs[i] != null) signs[i].anchoredPosition = shownPositions[i] + Vector2.up * value;
    }

    private void PlaySignSound(bool opening)
    {
        if (slideDownClip == null) slideDownClip = Resources.Load<AudioClip>("PausePanel/dragon-studio-wooden");
        if (slideUpClip == null) slideUpClip = Resources.Load<AudioClip>("PausePanel/dragon-studio-wooden");
        var clip = opening ? slideDownClip : slideUpClip;
        if (clip == null) return;
        if (signAudio == null)
        {
            // Keep the voice outside the panel so its final closing sound is
            // controlled by the animation, rather than the panel's active state.
            var voice = new GameObject("Pause Sign Wood Audio");
            voice.transform.SetParent(transform, false);
            signAudio = voice.AddComponent<AudioSource>();
            signAudio.playOnAwake = false;
            signAudio.loop = false;
            signAudio.spatialBlend = 0f;
            signAudio.ignoreListenerPause = true;
            signAudio.priority = 64;
        }
        signAudio.Stop();
        signAudio.clip = clip;
        signAudio.volume = woodVolume;
        signAudio.pitch = 1f;
        signAudio.Play();
    }

    private void StopSignSound()
    {
        if (signAudio != null) signAudio.Stop();
    }

    private void OnDisable() { StopSignSound(); }

    private void OnDestroy()
    {
        if (signAudio == null) return;
        if (Application.isPlaying) Destroy(signAudio.gameObject);
        else DestroyImmediate(signAudio.gameObject);
    }
}
