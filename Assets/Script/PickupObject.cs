using UnityEngine;

public class PickupObject : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string itemName;

    public void Interact()
    {
        Debug.Log("เก็บ " + itemName);
        Destroy(gameObject);
    }
}
