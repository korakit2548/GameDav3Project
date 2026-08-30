using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    private int totalDirty;
    private int remainingDirty;

    private int totalTrash;
    private int remainingTrash;

    private void Start()
    {
        totalDirty = GameObject.FindGameObjectsWithTag("Dirty").Length;

        totalTrash = GameObject.FindGameObjectsWithTag("Trash").Length;

        remainingDirty = totalDirty;
        remainingTrash = totalTrash;

        Debug.Log("Dirty: " + remainingDirty + " / " + totalDirty);
        Debug.Log("Trash: " + remainingTrash + " / " + totalTrash);
    }

    public void ObjectDestroyed(GameObject target)
    {
        if (target.CompareTag("Dirty"))
        {
            remainingDirty--;

            Debug.Log("Dirty: " +remainingDirty +" / " +totalDirty);
        }

        if (target.CompareTag("Trash"))
        {
            remainingTrash--;

            Debug.Log("Trash: " +remainingTrash +" / " +totalTrash);
        }
    }

    public int GetRemainingDirty()
    {
        return remainingDirty;
    }

    public int GetRemainingTrash()
    {
        return remainingTrash;
    }

    public int GetTotalDirty()
    {
        return totalDirty;
    }

    public int GetTotalTrash()
    {
        return totalTrash;
    }
}