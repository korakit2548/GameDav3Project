
using UnityEngine;

public class PlayerCarrySystem : MonoBehaviour
{
    private string carriedItem;

    [Header("Carry Visual")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private GameObject carriedBoxPrefab;

    private GameObject currentCarriedObject;

    public void Configure(Animator animator, GameplayAnimationSet animations)
    {
        if (carriedBoxPrefab == null && animations != null) carriedBoxPrefab = animations.carriedBoxPrefab;
        if (carryPoint == null && animator != null && animator.isHuman)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (hand == null) return;
            carryPoint = new GameObject("Carry Point").transform;
            carryPoint.SetParent(hand, false);
            carryPoint.localPosition = new Vector3(-0.5f, 2f, 1f);
        }
    }

    public bool HasItem()
    {
        return !string.IsNullOrEmpty(carriedItem);
    }

    public bool TryPickup(string itemName)
    {
        if (HasItem())
        {
            Debug.Log("�س����觢ͧ��������");
            return false;
        }

        carriedItem = itemName;
        PlayerActionAudio.GetOrCreate(gameObject).PlayPickup();
        if (carryPoint != null && carriedBoxPrefab != null)
        {
            currentCarriedObject = Instantiate(
                carriedBoxPrefab,
                carryPoint
            );

            currentCarriedObject.transform.localPosition = Vector3.zero;
            currentCarriedObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("CarryPoint or CarriedBoxPrefab is missing!");
        }

        Debug.Log("����觢ͧ: " + carriedItem);

        return true;
    }

    public bool TryDeliver()
    {
        if (!HasItem())
        {
            Debug.Log("�س�������觢ͧ������");
            return false;
        }

        Debug.Log("����觢ͧ�����: " + carriedItem);

        if (currentCarriedObject != null)
        {
            Destroy(currentCarriedObject);
            currentCarriedObject = null;
        }

        carriedItem = null;
        PlayerActionAudio.GetOrCreate(gameObject).PlayPlace();

        return true;
    }

    public string GetCarriedItem()
    {
        return carriedItem;
    }

    public void SetCarryVisual(bool visible)
    {
        if (currentCarriedObject != null)
        {
            currentCarriedObject.SetActive(visible);
        }
    }
}
