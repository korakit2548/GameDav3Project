using System.Collections.Generic;
using UnityEngine;

public class InteractHitbox : MonoBehaviour
{
    private PlayerController player;

    private HashSet<Collider> currentColliders = new HashSet<Collider>();

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        currentColliders.Add(other);

        RefreshCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        currentColliders.Remove(other);

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
    public void RefreshTool()
    {
        foreach (Collider other in currentColliders)
        {
            if (other != null)
            {
                RefreshCollider(other);
            }
        }
    }
    private void RefreshCollider(Collider other)
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

        bool canInteract = false;

        if (holdInteractable != null)
        {
            if (other.CompareTag("Dirty") && player.GetCurrentTool() == CleaningTool.sponge)
            {
                canInteract = true;
            }

            if ((other.CompareTag("Trash") || other.CompareTag("CursedTrash")) && player.GetCurrentTool() == CleaningTool.Broom)
            {
                canInteract = true;
            }

            if (canInteract)
            {
                player.SetHoldInteractable(holdInteractable);
            }
            else
            {
                player.RemoveHoldInteractable(holdInteractable);
            }
        }

        InteractHighlight highlight = other.GetComponentInParent<InteractHighlight>();

        if (highlight == null)
        {
            highlight = other.GetComponentInChildren<InteractHighlight>();
        }
        if (highlight != null)
        {
            highlight.SetHighlight(interactable != null || canInteract);
        }
    }
}