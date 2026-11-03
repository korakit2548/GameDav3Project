using UnityEngine;

public class HoldDestroyableObject : MonoBehaviour, IHoldInteractable
{
    [SerializeField]
    private float destroyTime = 2f;

    [SerializeField]
    private CleaningBar cleaningBar;

    [SerializeField] private CleaningAction cleaningAction;

    private CleaningController cleaningController;

    private float currentTime;
    private bool isHolding;
    private bool isDestroyed;

    private void Awake()
    {
        cleaningController = FindAnyObjectByType<CleaningController>();
    }

    public void BeginHold()
    {
        if (isDestroyed)
            return;

        if (cleaningController == null)
            cleaningController = FindAnyObjectByType<CleaningController>();

        currentTime = 0f;
        isHolding = true;

        cleaningBar.SetProgress(0f);

        if (cleaningController != null)
        {
            cleaningController.StartCleaning(cleaningAction, transform);
        }

        //Debug.Log("����������");
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

        if (cleaningController != null)
        {
            cleaningController.StopCleaning();
        }

        //Debug.Log("��ش�����");
    }

    private void DestroyObject()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        isHolding = false;

        if (cleaningController != null)
        {
            cleaningController.StopCleaning();
        }

        CursedTrash cursedTrash = GetComponent<CursedTrash>();

        if (cursedTrash != null)
        {
            cursedTrash.Clean();
            return;
        }

        ObjectiveManager objectiveManager = FindAnyObjectByType<ObjectiveManager>();

        if (objectiveManager != null)
        {
            objectiveManager.ObjectDestroyed(gameObject);
        }

        Destroy(gameObject);
    }
}