using UnityEngine;

public class InteractHitbox : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.gameObject.name);

        IInteractable interactable = other.GetComponent<IInteractable>();

        IHoldInteractable holdInteractable = other.GetComponent<IHoldInteractable>();

        if (interactable != null)
        {
            player.SetInteractable(interactable);
        }

        if (holdInteractable != null)
        {
            player.SetHoldInteractable(holdInteractable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable =
            other.GetComponent<IInteractable>();

        IHoldInteractable holdInteractable =
            other.GetComponent<IHoldInteractable>();

        if (interactable != null)
        {
            player.RemoveInteractable(interactable);
        }

        if (holdInteractable != null)
        {
            player.RemoveHoldInteractable(holdInteractable);
        }
    }
}
