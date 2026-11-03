
using UnityEngine;
using UnityEngine.UI;

public class CarryItemUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private Sprite boxImage;

    private PlayerCarrySystem playerCarrySystem;
    private bool previousHasItem;

    private void Start()
    {
        playerCarrySystem = FindAnyObjectByType<PlayerCarrySystem>();

        itemImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerCarrySystem == null)
            return;

        bool hasItem = playerCarrySystem.HasItem();

        if (hasItem != previousHasItem)
        {
            UpdateUI(hasItem);
            previousHasItem = hasItem;
        }
    }

    private void UpdateUI(bool hasItem)
    {
        if (hasItem)
        {
            itemImage.sprite = boxImage;
            itemImage.gameObject.SetActive(true);
        }
        else
        {
            itemImage.gameObject.SetActive(false);
        }
    }
}
