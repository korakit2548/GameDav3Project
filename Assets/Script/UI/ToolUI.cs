using UnityEngine;
using TMPro;

public class ToolUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text toolText;

    private PlayerController player;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (player == null)
            return;

        toolText.text = "Tool: " + player.GetCurrentTool();
    }
}