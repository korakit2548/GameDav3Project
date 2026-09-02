using UnityEngine;

public class HoldDestroyableObject : MonoBehaviour, IHoldInteractable
{
    [SerializeField]
    private float destroyTime = 2f;

    private float currentTime;
    private bool isHolding;

    public void BeginHold()
    {
        currentTime = 0f;
        isHolding = true;
        Debug.Log("เริ่มทำลาย");
    }

    public void UpdateHold(float deltaTime)
    {
        if (!isHolding)
            return;

        currentTime += deltaTime;

        Debug.Log("กำลังทำลาย: " + currentTime);
        if (currentTime >= destroyTime)
        {
            DestroyObject();
        }
    }

    public void EndHold()
    {
        isHolding = false;
        currentTime = 0f;

        Debug.Log("หยุดทำลาย");
    }

    private void DestroyObject()
    {
        Debug.Log("Object Destroyed!");
        isHolding = false;

        ObjectiveManager objectiveManager = FindFirstObjectByType<ObjectiveManager>();

        if (objectiveManager != null)
        {
            objectiveManager.ObjectDestroyed(gameObject);
        }
        Destroy(gameObject);
    }
}
