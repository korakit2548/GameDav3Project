using UnityEngine;

public class HoldDestroyableObject : MonoBehaviour, IHoldInteractable
{
    [SerializeField]
    private float destroyTime = 2f;

    [SerializeField]
    private CleaningBar cleaningBar;

    private float currentTime;
    private bool isHolding;
    private bool isDestroyed;

    public void BeginHold()
    {
        if (isDestroyed)
            return;

        currentTime = 0f;
        isHolding = true;

        cleaningBar.SetProgress(0f);

        //Debug.Log("เริ่มทำลาย");
    }

    public void UpdateHold(float deltaTime)
    {
        if (!isHolding || isDestroyed)
            return;

        currentTime += deltaTime;

        float progress = currentTime / destroyTime;
        cleaningBar.SetProgress(progress);

        if (currentTime >= destroyTime)
        {
            DestroyObject();
        }
    }

    public void EndHold()
    {
        if (isDestroyed)
            return;

        isHolding = false;
        currentTime = 0f;

        cleaningBar.SetProgress(0f);

        //Debug.Log("หยุดทำลาย");
    }

    private void DestroyObject()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        isHolding = false;

        CursedTrash cursedTrash = GetComponent<CursedTrash>();

        if (cursedTrash != null)
        {
            cursedTrash.Clean();
            return;
        }

        ObjectiveManager objectiveManager = FindFirstObjectByType<ObjectiveManager>();

        if (objectiveManager != null)
        {
            objectiveManager.ObjectDestroyed(gameObject);
        }

        Destroy(gameObject);
    }
}