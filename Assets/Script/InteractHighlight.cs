using UnityEngine;

public class InteractHighlight : MonoBehaviour
{
    private Renderer objectRenderer;
    private Material material;
    private Color originalColor;

    [SerializeField]
    private Color highlightColor = Color.red;

    private void Awake()
    {
        objectRenderer = GetComponentInChildren<Renderer>();

        if (objectRenderer == null)
        {
            return;
        }

        material = objectRenderer.material;

        if (material.HasProperty("_BaseColor"))
        {
            originalColor = material.GetColor("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            originalColor = material.GetColor("_Color");
        }
    }
    public void SetHighlight(bool active)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            if (active)
            {
                material.SetColor("_BaseColor", Color.red);
            }
            else
            {
                material.SetColor("_BaseColor", originalColor);
            }
        }
    }
}

