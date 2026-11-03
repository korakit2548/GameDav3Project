
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Image toolImage;

    [SerializeField]
    private TMP_Text toolText;

    [Header("Tool Icons")]
    [SerializeField]
    private Sprite spongeIcon;

    [SerializeField]
    private Sprite broomIcon;

    private PlayerController player;
    private CleaningTool previousTool;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        UpdateUI();
    }

    private void Update()
    {
        if (player == null)
            return;

        CleaningTool currentTool = player.GetCurrentTool();

        if (currentTool != previousTool)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (player == null)
            return;

        CleaningTool currentTool = player.GetCurrentTool();

        switch (currentTool)
        {
            case CleaningTool.sponge:
                toolImage.sprite = spongeIcon;
                toolText.text = "Sponge";
                break;

            case CleaningTool.Broom:
                toolImage.sprite = broomIcon;
                toolText.text = "Broom";
                break;
        }

        previousTool = currentTool;
    }
}
