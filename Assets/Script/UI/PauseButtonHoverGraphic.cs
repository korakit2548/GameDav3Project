using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseButtonHoverGraphic : MonoBehaviour
{
    private Image artwork;
    private Image overlay;
    public Image Overlay => overlay;

    public Image Configure(Image source)
    {
        artwork = source;
        if (overlay == null)
        {
            var mask = new GameObject("Pause Button Hover Mask", typeof(RectTransform), typeof(RectMask2D));
            mask.transform.SetParent(transform, false);
            var rect = (RectTransform)mask.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = new GameObject("Pause Sign Dark Overlay", typeof(RectTransform), typeof(Image));
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

    private void LateUpdate() { SynchronizeArtwork(); }

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
