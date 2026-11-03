
using UnityEngine;

public class PickupStation : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField]
    private string itemName = "Box";

    [SerializeField]
    private GameObject[] objectsToHide;

    private PlayerCarrySystem playerCarrySystem;

    private int pickedUpCount;

    private void Awake()
    {
        playerCarrySystem = FindAnyObjectByType<PlayerCarrySystem>();
    }

    private void Start()
    {
        pickedUpCount = 0;
    }

    public void Interact()
    {
        // ตรวจสอบจำนวน Item ที่เหลือ
        if (pickedUpCount >= objectsToHide.Length)
        {
            Debug.Log("Pickup Station หมดแล้ว");
            return;
        }

        if (playerCarrySystem == null)
        {
            Debug.LogError("ไม่พบ PlayerCarrySystem");
            return;
        }

        // หยิบ Item
        if (playerCarrySystem.TryPickup(itemName))
        {
            // ซ่อน Object ตามลำดับ
            GameObject targetObject = objectsToHide[pickedUpCount];

            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }

            pickedUpCount++;

            Debug.Log(
                "Pickup: " + pickedUpCount +
                " / " + objectsToHide.Length
            );

            // เมื่อหยิบครบ ให้ซ่อน PickupStation
            if (pickedUpCount >= objectsToHide.Length)
            {
                HidePickupStation();
            }
        }
    }

    private void HidePickupStation()
    {
        gameObject.SetActive(false);
    }

    public int GetPickedUpCount()
    {
        return pickedUpCount;
    }

    public int GetCapacity()
    {
        return objectsToHide.Length;
    }
}
