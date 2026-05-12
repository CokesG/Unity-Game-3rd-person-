using UnityEngine;

[DisallowMultipleComponent]
public sealed class CriticalHitbox : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;
}
