using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text dirtyText;

    [SerializeField]
    private TMP_Text trashText;

    [SerializeField]
    private GameCompleteUI gameCompleteUI;

    private ObjectiveManager objectiveManager;

    private bool isComplete;

    private void Start()
    {
        objectiveManager =
            FindFirstObjectByType<ObjectiveManager>();
        UpdateUI();
    }

    private void Update()
    {
        if (isComplete)
            return;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (objectiveManager == null)
            return;

        int remainingDirty = objectiveManager.GetRemainingDirty();
        int remainingTrash = objectiveManager.GetRemainingTrash();

        int totalDirty = objectiveManager.GetTotalDirty();
        int totalTrash = objectiveManager.GetTotalTrash();

        if (totalDirty + totalTrash > 0 && remainingDirty <= 0 && remainingTrash <= 0)
        {
            isComplete = true;

            dirtyText.text = "Objective Complete";
            trashText.text = "";

            if (gameCompleteUI != null)
            {
                gameCompleteUI.ShowComplete();
            }

            return;
        }

        dirtyText.text = "Dirty  " + remainingDirty + " / " + totalDirty;
        trashText.text = "Trash  " + remainingTrash + " / " + totalTrash;
    }
}