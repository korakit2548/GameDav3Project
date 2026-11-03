using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DeliveryObjectiveManager : MonoBehaviour
{
    [Header("Room Objectives")]
    [SerializeField, Tooltip("Required rooms. Totals use each DeliveryPoint's Required Items. Leave empty to use the legacy total.")]
    private DeliveryPoint[] roomDeliveryPoints;

    [Header("Legacy Objective Settings")]
    [SerializeField] private int totalItems = 5;
    [SerializeField] private EnemySpawner enemySpawner;

    private int deliveredItems;
    private bool isComplete;
    private bool UsesRoomGoals => roomDeliveryPoints != null && roomDeliveryPoints.Length > 0;

    private IEnumerable<DeliveryPoint> RoomGoals()
    {
        if (roomDeliveryPoints == null) yield break;
        var seen = new HashSet<DeliveryPoint>();
        foreach (var point in roomDeliveryPoints)
            if (point != null && seen.Add(point) && point.GetCapacity() > 0)
                yield return point;
    }

    private void Start()
    {
        deliveredItems = 0;
        isComplete = false;
    }

    public bool CanAcceptDelivery(DeliveryPoint point)
    {
        if (point == null || isComplete || point.GetDeliveredCount() >= point.GetCapacity()) return false;
        if (!UsesRoomGoals) return deliveredItems < GetTotalItems();
        foreach (var goal in RoomGoals()) if (goal == point) return true;
        return false;
    }

    public void ObjectDelivered(DeliveryPoint point)
    {
        if (!UsesRoomGoals) { ObjectDelivered(); return; }
        if (isComplete) return;
        bool tracked = false;
        foreach (var goal in RoomGoals()) if (goal == point) { tracked = true; break; }
        if (!tracked) return;
        int current = GetDeliveredItems();
        if (current <= deliveredItems) return;
        deliveredItems = current;
        RecordDelivery();
    }

    // Retained for existing levels and callers using a single total.
    public void ObjectDelivered()
    {
        if (UsesRoomGoals || isComplete || deliveredItems >= GetTotalItems()) return;
        deliveredItems++;
        RecordDelivery();
    }

    private void RecordDelivery()
    {
        if (enemySpawner != null) enemySpawner.ReduceTimeNextSpawn();
        int required = GetTotalItems();
        isComplete = required > 0 && GetDeliveredItems() >= required;
        Debug.Log("Delivery: " + GetDeliveredItems() + " / " + required);
    }

    public int GetDeliveredItems()
    {
        if (!UsesRoomGoals) return deliveredItems;
        int total = 0;
        foreach (var point in RoomGoals()) total += Mathf.Clamp(point.GetDeliveredCount(), 0, point.GetCapacity());
        return total;
    }

    public int GetTotalItems()
    {
        if (!UsesRoomGoals) return Mathf.Max(0, totalItems);
        int total = 0;
        foreach (var point in RoomGoals()) total += point.GetCapacity();
        return total;
    }

    public string GetRoomProgressText()
    {
        if (!UsesRoomGoals) return "Delivery  " + GetDeliveredItems() + " / " + GetTotalItems();
        var text = new StringBuilder();
        foreach (var point in RoomGoals())
        {
            if (text.Length > 0) text.Append('\n');
            text.Append(point.GetProgressText());
        }
        return text.ToString();
    }

    public bool IsComplete() => isComplete;
}
