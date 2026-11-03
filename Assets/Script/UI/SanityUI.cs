using UnityEngine;
using UnityEngine.UI;

public class SanityUI : MonoBehaviour
{
    [SerializeField]
    private PlayerController player;

    [SerializeField]
    private Slider sanitySlider;
    private HealthChangePopup changePopup;

    private void OnEnable()
    {
        if (changePopup != null) BindHealthFeedback();
    }

    private void OnDisable()
    {
        if (player != null) player.SanityChanged -= ShowHealthChange;
    }

    private void BindHealthFeedback()
    {
        if (player == null) return;
        player.SanityChanged -= ShowHealthChange;
        player.SanityChanged += ShowHealthChange;
    }

    private void ShowHealthChange(float amount)
    {
        if (changePopup != null) changePopup.ShowChange(amount);
    }

    private void Start()
    {
        ConfigureSlider();

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player != null && sanitySlider != null)
        {
            sanitySlider.maxValue = player.GetMaxSanity();
            sanitySlider.value = player.GetSanity();
            changePopup = sanitySlider.GetComponent<HealthChangePopup>();
            if (changePopup == null) changePopup = sanitySlider.gameObject.AddComponent<HealthChangePopup>();
            BindHealthFeedback();
        }
    }

    private void ConfigureSlider()
    {
        if (sanitySlider == null) return;

        float currentValue = sanitySlider.value;
        // The golden center artwork is decoration, not a moving slider handle.
        // Keep its authored full-health pose before releasing Slider's control.
        sanitySlider.SetValueWithoutNotify(sanitySlider.maxValue);
        sanitySlider.handleRect = null;
        sanitySlider.minValue = 0f;

        var fill = sanitySlider.fillRect != null ? sanitySlider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            // Clip the original full-size art instead of shrinking a rect with
            // fixed margins, which made the bar look empty before health was zero.
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = sanitySlider.normalizedValue;
        }
        sanitySlider.SetValueWithoutNotify(currentValue);
    }

    private void Update()
    {
        UpdateSanityUI();
    }

    private void UpdateSanityUI()
    {
        if (player == null || sanitySlider == null)
            return;

        sanitySlider.value = player.GetSanity();
    }
}
