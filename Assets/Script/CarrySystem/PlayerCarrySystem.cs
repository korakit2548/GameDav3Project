
using UnityEngine;

public class PlayerCarrySystem : MonoBehaviour
{
    private string carriedItem;

    public bool HasItem()
    {
        return !string.IsNullOrEmpty(carriedItem);
    }

    public bool TryPickup(string itemName)
    {
        if (HasItem())
        {
            Debug.Log("คุณมีสิ่งของอยู่แล้ว");
            return false;
        }

        carriedItem = itemName;

        Debug.Log("เก็บสิ่งของ: " + carriedItem);

        return true;
    }

    public bool TryDeliver()
    {
        if (!HasItem())
        {
            Debug.Log("คุณไม่มีสิ่งของที่จะส่ง");
            return false;
        }

        Debug.Log("ส่งสิ่งของสำเร็จ: " + carriedItem);

        carriedItem = null;

        return true;
    }

    public string GetCarriedItem()
    {
        return carriedItem;
    }
}
