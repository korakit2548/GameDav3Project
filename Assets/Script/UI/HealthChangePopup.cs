using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HealthChangePopup : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Min(0.1f)] private float lifetime = 0.85f;
    [SerializeField, Min(0f)] private float riseDistance = 55f;
    [SerializeField] private Color damageColor = new Color(1f, 0.12f, 0.16f);
    [SerializeField] private Color healingColor = new Color(0.25f, 1f, 0.4f);

    private sealed class Popup
    {
        public TextMeshProUGUI label;
        public float amount;
        public float age;
        public bool active;
    }

    private const int Capacity = 6;
    private readonly List<Popup> pool = new List<Popup>();
    private RectTransform popupRoot;

    public void ShowChange(float amount)
    {
        if (!isActiveAndEnabled || Mathf.Abs(amount) < 0.001f) return;
        Popup popup = pool.Find(entry => !entry.active);
        if (popup == null)
        {
            if (pool.Count < Capacity) popup = CreatePopup();
            else
            {
                popup = pool[0];
                foreach (var entry in pool) if (entry.age > popup.age) popup = entry;
            }
        }
        popup.amount = amount;
        popup.age = 0f;
        popup.active = true;
        popup.label.gameObject.SetActive(true);
        popup.label.transform.SetAsLastSibling();
        SetText(popup);
        Draw(popup);
    }

    private Popup CreatePopup()
    {
        if (popupRoot == null)
        {
            var root = new GameObject("Health Change Popups", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);
            popupRoot = (RectTransform)root.transform;
            popupRoot.anchorMin = Vector2.zero;
            popupRoot.anchorMax = Vector2.one;
            popupRoot.offsetMin = popupRoot.offsetMax = Vector2.zero;
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 101;
        }
        var child = new GameObject("Health Change", typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(popupRoot, false);
        var label = child.GetComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = 44f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        label.rectTransform.sizeDelta = new Vector2(220f, 80f);
        var popup = new Popup { label = label };
        pool.Add(popup);
        return popup;
    }

    private static void SetText(Popup popup)
    {
        popup.label.text = popup.amount.ToString("+0.###;-0.###", CultureInfo.InvariantCulture);
    }

    private void Update() { Advance(Time.unscaledDeltaTime); }

    private void Advance(float deltaTime)
    {
        foreach (var popup in pool)
        {
            if (!popup.active) continue;
            popup.age += Mathf.Max(0f, deltaTime);
            if (popup.age >= Mathf.Max(0.1f, lifetime))
            {
                popup.active = false;
                popup.label.gameObject.SetActive(false);
            }
            else Draw(popup);
        }
    }

    private void Draw(Popup popup)
    {
        float progress = Mathf.Clamp01(popup.age / Mathf.Max(0.1f, lifetime));
        float rise = 1f - Mathf.Pow(1f - progress, 3f);
        popup.label.rectTransform.anchoredPosition = new Vector2(popup.amount < 0f ? -75f : 75f, 14f + riseDistance * rise);
        float scale = popup.age < 0.14f
            ? Mathf.Lerp(0.7f, 1.2f, popup.age / 0.14f)
            : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((popup.age - 0.14f) / 0.16f));
        popup.label.rectTransform.localScale = Vector3.one * scale;
        var color = popup.amount < 0f ? damageColor : healingColor;
        color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, progress));
        popup.label.color = color;
    }

    private void OnDisable()
    {
        foreach (var popup in pool)
        {
            popup.active = false;
            if (popup.label != null) popup.label.gameObject.SetActive(false);
        }
    }
}
