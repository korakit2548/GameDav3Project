using UnityEngine;
using UnityEngine.UI;

public class CleaningBar : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;

    public void SetProgress(float progress)
    {
        if (fillImage == null)
            return;

        float amount = Mathf.Clamp01(progress);
        fillImage.type = Image.Type.Filled;
        var slider = GetComponent<Slider>();
        if (slider != null && slider.fillRect == fillImage.rectTransform)
        {
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = Vector2.one;
            slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, amount));
        }
        fillImage.fillAmount = amount;
    }
}
