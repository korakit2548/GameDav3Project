using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    private int totalDirty;
    private int remainingDirty;

    private int totalTrash;
    private int remainingTrash;

    [SerializeField]
    private EnemySpawner enemySpawner;
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
            enemySpawner.ForceNextSpawn();
            Debug.Log("Dirty: " +remainingDirty +" / " +totalDirty);
        }

        if (target.CompareTag("Trash"))
        {
            remainingTrash--;
            enemySpawner.ForceNextSpawn();

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