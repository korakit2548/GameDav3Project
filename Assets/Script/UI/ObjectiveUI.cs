using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text dirtyText;

    [SerializeField]
    private TMP_Text trashText;

    private ObjectiveManager objectiveManager;

    private void Start()
    {
        objectiveManager = FindFirstObjectByType<ObjectiveManager>();

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (objectiveManager == null)
            return;

        int remainingDirty = objectiveManager.GetRemainingDirty();

        int remainingTrash = objectiveManager.GetRemainingTrash();

        if (remainingDirty <= 0 &&
            remainingTrash <= 0)
        {
            dirtyText.text = "Objective Complete";

            trashText.text = "";

            return;
        }

        dirtyText.text = "Dirty  " + remainingDirty + " / " + objectiveManager.GetTotalDirty();

        trashText.text = "Trash  " + remainingTrash + " / " + objectiveManager.GetTotalTrash();
    }
}