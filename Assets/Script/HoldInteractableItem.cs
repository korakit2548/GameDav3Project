using UnityEngine;

public interface IHoldInteractable
{
    void BeginHold();
    void UpdateHold(float deltaTime);
    void EndHold();
}
