using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [Header("Room Objective")]
    [SerializeField, Tooltip("Room name displayed in the delivery objective.")]
    private string roomName = "Delivery";

    [SerializeField, Min(-1), Tooltip("Required deliveries. 0 disables this room task; -1 uses the furniture list length.")]
    private int requiredItems = -1;

    [Header("Delivery Settings")]
    [SerializeField] private GameObject[] objectsToActivate;

    private PlayerCarrySystem playerCarrySystem;
    private DeliveryObjectiveManager objectiveManager;
    private int deliveredCount;

    private void Awake()
    {
        playerCarrySystem = FindAnyObjectByType<PlayerCarrySystem>();
        objectiveManager = FindAnyObjectByType<DeliveryObjectiveManager>();
    }

    private void Start()
    {
        deliveredCount = 0;
        bool noTask = GetCapacity() == 0;
        if (objectsToActivate != null)
            foreach (var item in objectsToActivate)
                if (item != null) item.SetActive(noTask);
        if (noTask) HideDeliveryPoint();
    }

    public void Interact()
    {
        if (deliveredCount >= GetCapacity() || playerCarrySystem == null) return;
        if (objectiveManager != null && !objectiveManager.CanAcceptDelivery(this)) return;
        if (!playerCarrySystem.TryDeliver()) return;

        if (objectsToActivate != null && deliveredCount < objectsToActivate.Length)
        {
            var item = objectsToActivate[deliveredCount];
            if (item != null) item.SetActive(true);
        }
        deliveredCount++;

        // Completing a room reveals its final furniture, even if its quota
        // differs from the number of furniture visuals.
        if (deliveredCount >= GetCapacity())
        {
            if (objectsToActivate != null)
                foreach (var item in objectsToActivate)
                    if (item != null) item.SetActive(true);
            HideDeliveryPoint();
        }
        if (objectiveManager != null) objectiveManager.ObjectDelivered(this);
        Debug.Log(GetProgressText());
    }

    public int GetDeliveredCount() => deliveredCount;
    public int GetCapacity() => requiredItems >= 0
        ? requiredItems : (objectsToActivate != null ? objectsToActivate.Length : 0);

    public string GetProgressText()
    {
        string label = string.IsNullOrWhiteSpace(roomName) ? gameObject.name : roomName;
        return label + "  " + deliveredCount + " / " + GetCapacity();
    }

    private void HideDeliveryPoint() => gameObject.SetActive(false);
}
