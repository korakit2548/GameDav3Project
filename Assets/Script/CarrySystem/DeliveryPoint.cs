
using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [Header("Delivery Settings")]
    [SerializeField]
    private GameObject[] objectsToActivate;

    private PlayerCarrySystem playerCarrySystem;
    private DeliveryObjectiveManager objectiveManager;

    private int deliveredCount;

    private void Awake()
    {
        playerCarrySystem = FindFirstObjectByType<PlayerCarrySystem>();

        objectiveManager =
            FindFirstObjectByType<DeliveryObjectiveManager>();
    }

    private void Start()
    {
        deliveredCount = 0;

        // ปิด Object ทั้งหมดเมื่อเริ่มเกม
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    public void Interact()
    {
        // ตรวจสอบ Capacity
        if (deliveredCount >= objectsToActivate.Length)
        {
            Debug.Log("Delivery Point เต็มแล้ว");
            return;
        }

        if (playerCarrySystem == null)
        {
            Debug.LogError("ไม่พบ PlayerCarrySystem");
            return;
        }

        // ตรวจสอบว่าผู้เล่นมีสิ่งของหรือไม่
        if (!playerCarrySystem.HasItem())
        {
            Debug.Log("คุณไม่มีสิ่งของที่จะส่ง");
            return;
        }

        // ส่งสิ่งของ
        if (playerCarrySystem.TryDeliver())
        {
            // เปิด Object ตามลำดับ
            GameObject targetObject =
                objectsToActivate[deliveredCount];

            if (targetObject != null)
            {
                targetObject.SetActive(true);
            }

            deliveredCount++;

            Debug.Log(
                "Delivery: " + deliveredCount +
                " / " + objectsToActivate.Length
            );

            // อัปเดต Objective
            if (objectiveManager != null)
            {
                objectiveManager.ObjectDelivered();
            }
            if (deliveredCount >= objectsToActivate.Length)
            {
                HideDeliveryPoint();
            }
        }
    }

    public int GetDeliveredCount()
    {
        return deliveredCount;
    }

    public int GetCapacity()
    {
        return objectsToActivate.Length;
    }

    private void HideDeliveryPoint()
    {
        gameObject.SetActive(false);
    }
}
