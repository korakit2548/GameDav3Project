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

    public void Configure(Animator target, GameplayAnimationSet animations)
    {
        if (animator == null) animator = target;
        if (animations == null) return;
        if (broomPrefab == null) broomPrefab = animations.broomPrefab;
        if (spongePrefab == null) spongePrefab = animations.spongePrefab;
        if (itemHolder == null && animator != null && animator.isHuman)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (hand == null) return;
            itemHolder = new GameObject("Item_Holding").transform;
            itemHolder.SetParent(hand, false);
            // Preserve the attachment pose authored with the supplied animations.
            itemHolder.localPosition = new Vector3(0.5f, 2f, 0.5f);
            itemHolder.localRotation = Quaternion.Euler(0f, 290f, 90f);
        }
    }

    public void StartCleaning(CleaningAction action)
    {
        StopCleaning();
        if (animator == null) return;

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
        if (animator != null)
        {
            animator.SetBool("IsSweeping", false);
            animator.SetBool("IsWiping", false);
        }

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
