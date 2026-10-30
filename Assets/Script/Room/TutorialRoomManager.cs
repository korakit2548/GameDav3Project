
using UnityEngine;
using System.Collections.Generic;

public class TutorialRoomManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialRoom
    {
        public Room room;

        public Room.RoomState startState;
    }

    [SerializeField]
    private List<TutorialRoom> tutorialRooms = new List<TutorialRoom>();

    private void Start()
    {
        foreach (TutorialRoom tutorialRoom in tutorialRooms)
        {
            if (tutorialRoom.room == null)
                continue;

            switch (tutorialRoom.startState)
            {
                case Room.RoomState.Normal:
                    tutorialRoom.room.SetNormal();
                    break;

                case Room.RoomState.Warning:
                    tutorialRoom.room.SetWarning();
                    break;

                case Room.RoomState.Cursed:
                    tutorialRoom.room.StartCursedImmediately();
                    break;
            }
        }
    }
}
