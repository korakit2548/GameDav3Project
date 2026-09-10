using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [SerializeField]
    private List<Room> rooms = new List<Room>();


    public List<Room> GetNormalRooms()
    {
        List<Room> normalRooms = new List<Room>();

        foreach (Room room in rooms)
        {
            if (room.IsNormal())
            {
                normalRooms.Add(room);
            }
        }

        return normalRooms;
    }


    public Room GetRandomNormalRoom()
    {
        List<Room> normalRooms = GetNormalRooms();

        if (normalRooms.Count == 0)
        {
            Debug.Log("ไม่มีห้องที่สามารถ Spawn Enemy ได้");
            return null;
        }

        int randomIndex = Random.Range(0, normalRooms.Count);

        return normalRooms[randomIndex];
    }
}