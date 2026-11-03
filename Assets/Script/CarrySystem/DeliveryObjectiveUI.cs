
using UnityEngine;
using TMPro;
using System.Collections;

public class DeliveryObjectiveUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text deliveryText;

    [SerializeField, Tooltip("Show a separate progress line for each configured room.")]
    private bool showRoomProgress;

    [SerializeField]
    private GameCompleteUI gameCompleteUI;

    [SerializeField]
    private float completeDelay = 1f;

    private DeliveryObjectiveManager objectiveManager;

    private bool isComplete;

    private void Start()
    {
        objectiveManager =
            FindFirstObjectByType<DeliveryObjectiveManager>();

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

        int delivered = objectiveManager.GetDeliveredItems();
        int total = objectiveManager.GetTotalItems();

        if (deliveryText == null) return;
        deliveryText.text = showRoomProgress ? objectiveManager.GetRoomProgressText()
            : "Delivery  " + delivered + " / " + total;

        if (objectiveManager.IsComplete())
        {
            isComplete = true;

            deliveryText.gameObject.SetActive(false);

            StartCoroutine(ShowCompleteAfterDelay());
        }
    }

    private IEnumerator ShowCompleteAfterDelay()
    {
        yield return new WaitForSeconds(completeDelay);

        if (gameCompleteUI != null)
        {
            gameCompleteUI.ShowComplete();
        }
    }
}
