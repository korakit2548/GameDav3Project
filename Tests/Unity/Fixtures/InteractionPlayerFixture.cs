using UnityEngine;

// Only copied to the disposable test project. Production PlayerController
// depends on input/camera/gameplay systems unrelated to trigger eligibility.
public sealed class PlayerController : MonoBehaviour
{
    public CleaningTool tool;
    public IInteractable currentInteract;
    public IHoldInteractable currentHold;
    public CleaningTool GetCurrentTool() { return tool; }
    public void SetInteractable(IInteractable target) { currentInteract = target; }
    public void RemoveInteractable(IInteractable target) { if (currentInteract == target) currentInteract = null; }
    public void SetHoldInteractable(IHoldInteractable target) { currentHold = target; }
    public void RemoveHoldInteractable(IHoldInteractable target) { if (currentHold == target) currentHold = null; }
}

public sealed class OutlineHoldFixture : MonoBehaviour, IHoldInteractable
{
    public void BeginHold() { }
    public void UpdateHold(float deltaTime) { }
    public void EndHold() { }
}

public sealed class OutlinePressFixture : MonoBehaviour, IInteractable
{
    public int presses;
    public void Interact() { presses++; }
}
