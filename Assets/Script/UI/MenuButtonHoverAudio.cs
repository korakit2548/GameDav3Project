using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class MenuButtonHoverAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private bool pointerInside;

    public static void Bind(GameObject root)
    {
        if (root == null) return;
        foreach (var target in root.GetComponentsInChildren<Button>(true))
            if (target.GetComponent<MenuButtonHoverAudio>() == null)
                target.gameObject.AddComponent<MenuButtonHoverAudio>();
    }

    private void Awake() { button = GetComponent<Button>(); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pointerInside || button == null || !button.IsActive() || !button.IsInteractable()) return;
        pointerInside = true;
        MenuFeedbackAudio.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData) { pointerInside = false; }
    private void OnDisable() { pointerInside = false; }
}
