using UnityEngine;

[CreateAssetMenu(menuName = "Game/Gameplay Animation Set")]
public sealed class GameplayAnimationSet : ScriptableObject
{
    public RuntimeAnimatorController controller;
    public GameObject broomPrefab;
    public GameObject spongePrefab;
    public GameObject carriedBoxPrefab;

    public static GameplayAnimationSet Load()
    {
        return Resources.Load<GameplayAnimationSet>("GameplayAnimations");
    }
}
