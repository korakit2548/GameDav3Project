using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PauseButtonHoverGraphic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image artwork;
    private Image overlay;
    private Button button;
    private bool pointerInside;
    private float opacity;
    public Image Overlay => overlay;

    public Image Configure(Image source)
    {
        artwork = source;
        button = GetComponent<Button>();
        if (overlay == null)
        {
            var mask = new GameObject("Pause Button Hover Mask", typeof(RectTransform), typeof(RectMask2D));
            mask.layer = source.gameObject.layer;
            mask.transform.SetParent(transform, false);
            var rect = (RectTransform)mask.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = new GameObject("Pause Sign Dark Overlay", typeof(RectTransform), typeof(Image));
            image.layer = source.gameObject.layer;
            image.transform.SetParent(mask.transform, false);
            overlay = image.GetComponent<Image>();
            overlay.raycastTarget = false;
        }
        overlay.sprite = artwork.sprite;
        overlay.type = artwork.type;
        overlay.preserveAspect = artwork.preserveAspect;
        overlay.color = new Color(1f, 1f, 1f, artwork.color.a);
        overlay.rectTransform.anchorMin = overlay.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        SynchronizeArtwork();
        return overlay;
    }

    public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; }
    public void OnPointerExit(PointerEventData eventData) { pointerInside = false; }

    private void OnEnable() { ResetHover(); }
    private void OnDisable() { ResetHover(); }

    private void ResetHover()
    {
        pointerInside = false;
        opacity = 0f;
        if (overlay != null) overlay.canvasRenderer.SetColor(Color.clear);
    }

    private void LateUpdate()
    {
        SynchronizeArtwork();
        if (overlay == null) return;
        float target = pointerInside && button != null && button.IsActive() && button.IsInteractable() ? 0.59f : 0f;
        opacity = Mathf.MoveTowards(opacity, target, Time.unscaledDeltaTime * 5.9f);
        overlay.canvasRenderer.SetColor(new Color(0f, 0f, 0f, opacity));
    }

    private void SynchronizeArtwork()
    {
        if (artwork == null || overlay == null) return;
        var source = artwork.rectTransform;
        var rect = overlay.rectTransform;
        rect.pivot = source.pivot;
        rect.sizeDelta = source.rect.size;
        rect.position = source.position;
        rect.rotation = source.rotation;
        Vector3 sourceScale = source.lossyScale;
        Vector3 parentScale = rect.parent.lossyScale;
        rect.localScale = new Vector3(sourceScale.x / Mathf.Max(0.0001f, parentScale.x),
            sourceScale.y / Mathf.Max(0.0001f, parentScale.y), 1f);
    }
}
