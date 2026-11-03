using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    public int damageCalls;
    public float totalDamage;
    public void TakeSanityDamage(float amount) { damageCalls++; totalDamage += amount; }
    public void BigShake() { }
    public void SmallShake() { }
}

public sealed class ObjectiveManager : MonoBehaviour
{
    public void ObjectDestroyed(GameObject target) { }
}
