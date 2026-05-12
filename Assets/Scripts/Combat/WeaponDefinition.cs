using UnityEngine;

public enum WeaponFireMode
{
    SemiAuto,
    Burst,
    Automatic,
    Charge
}

public enum WeaponShotModel
{
    Hitscan,
    Projectile
}

[CreateAssetMenu(menuName = "TPS/Weapon Definition", fileName = "WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponId = "prototype_rifle";
    public string displayName = "Prototype Rifle";
    public WeaponFireMode fireMode = WeaponFireMode.Automatic;
    public WeaponShotModel shotModel = WeaponShotModel.Hitscan;

    [Header("Damage")]
    public float bodyDamage = 24f;
    public float headshotMultiplier = 1.8f;
    public float maxRange = 180f;
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.55f);
    public LayerMask hitMask = ~0;

    [Header("Cadence")]
    public float fireRate = 540f;
    public int magazineSize = 30;
    public float reloadTime = 1.9f;
    public float emptyReloadTime = 2.25f;
    public float equipTime = 0.25f;
    public float adsTime = 0.14f;

    [Header("Spread")]
    public float hipSpreadDegrees = 2.4f;
    public float adsSpreadDegrees = 0.18f;
    public float movingSpreadAddDegrees = 0.65f;
    public float airborneSpreadAddDegrees = 1.1f;
    public float slideSpreadAddDegrees = 1.4f;
    public float spreadPerShot = 0.18f;
    public float maxSpreadAddDegrees = 2.2f;
    public float spreadRecoveryPerSecond = 6f;

    [Header("Recoil")]
    public float cameraRecoilPitch = 0.42f;
    public float cameraRecoilYaw = 0.18f;
    public float recoilSpreadAddDegrees = 0.12f;
    public float recoilResetDelay = 0.25f;
    public float recoilPitchRampPerShot = 0.035f;
    public float maxRecoilPitchPerShot = 0.68f;
    public float recoilYawPatternStep = 0.045f;
    public float maxRecoilYawPattern = 0.28f;
    public float recoilYawRandomness = 0.04f;

    [Header("Movement Multipliers")]
    public float aimMovementMultiplier = 0.75f;
    public float shootingMovementMultiplier = 1f;

    [Header("Projectile")]
    public float projectileSpeed = 180f;
    public float projectileGravity = 0f;

    public bool IsAutomatic => fireMode == WeaponFireMode.Automatic;

    public static WeaponDefinition CreateRuntimePrototypeRifle()
    {
        WeaponDefinition definition = CreateInstance<WeaponDefinition>();
        definition.name = "Runtime Prototype Rifle";
        return definition;
    }

    public float SecondsPerShot()
    {
        return fireRate <= 0f ? 0.1f : 60f / fireRate;
    }

    public float EvaluateDamage(float distance, bool critical)
    {
        float rangeT = Mathf.Clamp01(distance / Mathf.Max(maxRange, 0.01f));
        float damage = bodyDamage * damageFalloff.Evaluate(rangeT);
        return critical ? damage * headshotMultiplier : damage;
    }
}
