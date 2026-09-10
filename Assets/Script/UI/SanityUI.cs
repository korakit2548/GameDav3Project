using UnityEngine;
using TMPro;

public class SanityUI : MonoBehaviour
{
    [SerializeField]
    private PlayerController player;

    [SerializeField]
    private TMP_Text sanityText;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        UpdateSanityUI();
    }

    private void Update()
    {
        UpdateSanityUI();
    }

    private void UpdateSanityUI()
    {
        if (player == null || sanityText == null)
            return;

        sanityText.text =
            "Sanity: " + player.GetSanity().ToString("0");
    }
}