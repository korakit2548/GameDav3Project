
using UnityEngine;

public class DeliveryObjectiveManager : MonoBehaviour
{
    [Header("Objective Settings")]
    [SerializeField]
    private int totalItems = 5;

    [SerializeField]
    private EnemySpawner enemySpawner;

    private int deliveredItems;
    private bool isComplete;

    private void Start()
    {
        deliveredItems = 0;

        Debug.Log("Delivery: " + deliveredItems + " / " + totalItems);
    }

    public void ObjectDelivered()
    {
        if (isComplete)
            return;

        if (deliveredItems >= totalItems)
            return;

        deliveredItems++;

        // ลดเวลารอก่อนผีเกิด เหมือนระบบด่าน 1
        if (enemySpawner != null)
        {
            enemySpawner.ReduceTimeNextSpawn();
        }

        Debug.Log("Delivery: " + deliveredItems + " / " + totalItems);

        if (deliveredItems >= totalItems)
        {
            isComplete = true;

            Debug.Log("Delivery Objective Complete!");
        }
    }

    public int GetDeliveredItems()
    {
        return deliveredItems;
    }

    public int GetTotalItems()
    {
        return totalItems;
    }

    public bool IsComplete()
    {
        return isComplete;
    }
}
