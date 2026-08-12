using UnityEngine;

public class DestroyableObject : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("ทำลายวัตถุ");
        Destroy(gameObject);
    }
}
