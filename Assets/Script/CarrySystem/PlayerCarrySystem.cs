
using UnityEngine;

public class PlayerCarrySystem : MonoBehaviour
{
    private string carriedItem;

    [Header("Carry Visual")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private GameObject carriedBoxPrefab;

    private GameObject currentCarriedObject;
    
    public bool HasItem()
    {
        return !string.IsNullOrEmpty(carriedItem);
    }

    public bool TryPickup(string itemName)
    {
        if (HasItem())
        {
            Debug.Log("�س����觢ͧ��������");
            return false;
        }

        carriedItem = itemName;
        if (carryPoint != null && carriedBoxPrefab != null)
        {
            currentCarriedObject = Instantiate(
                carriedBoxPrefab,
                carryPoint
            );

            currentCarriedObject.transform.localPosition = Vector3.zero;
            currentCarriedObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("CarryPoint or CarriedBoxPrefab is missing!");
        }
        
        Debug.Log("����觢ͧ: " + carriedItem);

        return true;
    }

    public bool TryDeliver()
    {
        if (!HasItem())
        {
            Debug.Log("�س�������觢ͧ������");
            return false;
        }

        Debug.Log("����觢ͧ�����: " + carriedItem);

        if (currentCarriedObject != null)
        {
            Destroy(currentCarriedObject);
            currentCarriedObject = null;
        }
        
        carriedItem = null;

        return true;
    }

    public string GetCarriedItem()
    {
        return carriedItem;
    }
}
