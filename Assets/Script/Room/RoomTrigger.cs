using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    [SerializeField]
    private Room room;

    private bool playerInside;


    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }
        if (playerInside)
        {
            return;
        }

        playerInside = true;
        room.PlayerEnter();
    }


    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }
        if (!playerInside)
        {
            return;
        }

        playerInside = false;
        room.PlayerExit();
    }
}