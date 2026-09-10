using UnityEngine;

public class CursedTrash : MonoBehaviour
{
    private Room room;

    public void SetRoom(Room targetRoom)
    {
        room = targetRoom;
    }

    public void Clean()
    {
        if (room == null)
        {
            Debug.LogWarning("CursedTrash ไม่มี Room");
            return;
        }

        room.CleanCursedRoom();

        Debug.Log("Cursed Trash ถูกทำความสะอาด");

        Destroy(gameObject);
    }
}