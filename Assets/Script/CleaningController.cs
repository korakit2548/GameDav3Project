using UnityEngine;

public class CleaningController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform itemHolder;

    [Header("Cleaning Tools")]
    [SerializeField] private GameObject broomPrefab;
    [SerializeField] private GameObject spongePrefab;

    private GameObject currentTool;

    public void StartCleaning(CleaningAction action)
    {
        StopCleaning();

        switch (action)
        {
            case CleaningAction.Sweeping:
                EquipTool(broomPrefab);
                animator.SetBool("IsSweeping", true);
                break;

            case CleaningAction.WipingWall:
                EquipTool(spongePrefab);
                animator.SetBool("IsWiping", true);
                break;
        }
    }

    public void StopCleaning()
    {
        animator.SetBool("IsSweeping", false);
        animator.SetBool("IsWiping", false);

        if (currentTool != null)
        {
            Destroy(currentTool);
            currentTool = null;
        }
    }

    private void EquipTool(GameObject prefab)
    {
        if (prefab == null || itemHolder == null)
            return;

        currentTool = Instantiate(prefab, itemHolder);

        currentTool.transform.localPosition = Vector3.zero;
        currentTool.transform.localRotation = Quaternion.identity;
    }
}