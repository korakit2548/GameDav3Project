
using UnityEngine;

public class TutorialRoomTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private int triggerID;

    [Header("Room Settings")]
    [SerializeField] private Room warningRoom1;
    [SerializeField] private Room warningRoom2;
    [SerializeField] private Room immediateCursedRoom;

    [Header("Tutorial Text")]
    [SerializeField] private GameObject[] tutorialTexts;

    private bool playerInside;

    private void Start()
    {
        // ซ่อนข้อความทั้งหมดตอนเริ่มเกม
        foreach (GameObject text in tutorialTexts)
        {
            if (text != null)
                text.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || playerInside)
            return;

        playerInside = true;

        switch (triggerID)
        {
            case 1:
                TriggerOne();
                break;

            case 2:
                TriggerTwo();
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || !playerInside)
            return;

        playerInside = false;
    }

    private void TriggerOne()
    {
        if (warningRoom1 && warningRoom2 != null)
            warningRoom1.SetWarning();
            warningRoom2.SetWarning();

        ShowTutorialTexts();

        Debug.Log("Tutorial Trigger 1");
    }

    private void TriggerTwo()
    {
        if (immediateCursedRoom != null)
            immediateCursedRoom.StartCursedImmediately();

        ShowTutorialTexts();

        Debug.Log("Tutorial Trigger 2");
    }

    private void ShowTutorialTexts()
    {
        foreach (GameObject text in tutorialTexts)
        {
            if (text != null)
                text.SetActive(true);
        }
    }
}
