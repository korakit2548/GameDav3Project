using UnityEngine;

public sealed class DeliveryPlacementHintUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] proximityPrompts;

    private void LateUpdate()
    {
        if (panel == null) return;
        bool nearby = false;
        if (Time.timeScale > 0f && proximityPrompts != null)
            foreach (var prompt in proximityPrompts)
                if (prompt != null && prompt.activeInHierarchy) { nearby = true; break; }
        if (panel.activeSelf != nearby) panel.SetActive(nearby);
    }

    private void OnDisable()
    {
        if (panel != null) panel.SetActive(false);
    }
}
