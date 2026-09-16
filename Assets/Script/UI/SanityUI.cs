using UnityEngine;
using UnityEngine.UI;

public class SanityUI : MonoBehaviour
{
    [SerializeField]
    private PlayerController player;

    [SerializeField]
    private Slider sanitySlider;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null && sanitySlider != null)
        {
            sanitySlider.maxValue = player.GetMaxSanity();
            sanitySlider.value = player.GetSanity();
        }
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