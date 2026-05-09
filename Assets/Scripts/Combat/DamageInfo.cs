using UnityEngine;

public readonly struct DamageInfo
{
    public DamageInfo(float damage, Vector3 point, Vector3 normal, GameObject instigator, WeaponDefinition weapon, bool isCritical)
    {
        Damage = damage;
        Point = point;
        Normal = normal;
        Instigator = instigator;
        Weapon = weapon;
        IsCritical = isCritical;
    }

    public float Damage { get; }
    public Vector3 Point { get; }
    public Vector3 Normal { get; }
    public GameObject Instigator { get; }
    public WeaponDefinition Weapon { get; }
    public bool IsCritical { get; }
}

public interface IDamageable
{
    void ApplyDamage(DamageInfo damageInfo);
}
