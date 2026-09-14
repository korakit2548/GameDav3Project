using UnityEngine;

public class InteractHighlight : MonoBehaviour
{
    private Renderer objectRenderer;
    private Material material;
    private Color originalColor;

    [SerializeField]
    private Color highlightColor = Color.red;

    [SerializeField]
    private GameObject highlightLabel;

    private void Awake()
    {
        objectRenderer = GetComponentInChildren<Renderer>();

        if (objectRenderer == null)
            return;

        material = objectRenderer.material;

        if (material.HasProperty("_BaseColor"))
        {
            originalColor = material.GetColor("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            originalColor = material.GetColor("_Color");
        }

        if (highlightLabel != null)
        {
            highlightLabel.SetActive(false);
        }
    }

    public void SetHighlight(bool active)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                active ? highlightColor : originalColor
            );
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                active ? highlightColor : originalColor
            );
        }

        if (highlightLabel != null)
        {
            highlightLabel.SetActive(active);
        }
    }
}