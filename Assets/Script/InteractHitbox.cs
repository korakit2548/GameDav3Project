
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
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            interactable = other.GetComponentInChildren<IInteractable>();
        }

        IHoldInteractable holdInteractable = other.GetComponentInParent<IHoldInteractable>();

        if (holdInteractable == null)
        {
            holdInteractable = other.GetComponentInChildren<IHoldInteractable>();
        }

        if (interactable != null)
        {
            player.SetInteractable(interactable);
        }

        if (holdInteractable != null)
        {
            player.SetHoldInteractable(holdInteractable);
        }

        if (interactable != null || holdInteractable != null)
        {
            InteractHighlight highlight = other.GetComponentInParent<InteractHighlight>();

            if (highlight == null)
            {
                highlight = other.GetComponentInChildren<InteractHighlight>();
            }

            if (highlight != null)
            {
                highlight.SetHighlight(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            interactable = other.GetComponentInChildren<IInteractable>();
        }

        IHoldInteractable holdInteractable = other.GetComponentInParent<IHoldInteractable>();

        if (holdInteractable == null)
        {
            holdInteractable = other.GetComponentInChildren<IHoldInteractable>();
        }

        if (interactable != null)
        {
            player.RemoveInteractable(interactable);
        }

        if (holdInteractable != null)
        {
            player.RemoveHoldInteractable(holdInteractable);
        }

        if (interactable != null || holdInteractable != null)
        {
            InteractHighlight highlight = other.GetComponentInParent<InteractHighlight>();

            if (highlight == null)
            {
                highlight = other.GetComponentInChildren<InteractHighlight>();
            }

            if (highlight != null)
            {
                highlight.SetHighlight(false);
            }
        }
    }
}