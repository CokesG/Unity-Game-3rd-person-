using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(ThirdPersonMotor))]
public class PlayerWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDefinition weapon;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ThirdPersonCameraController cameraController;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Debug")]
    [SerializeField] private bool drawDebugShots = true;
    [SerializeField] private float debugShotTime = 0.05f;
    [SerializeField] private string weaponState;
    [SerializeField] private int currentAmmo;
    [SerializeField] private bool isReloading;
    [SerializeField] private bool muzzleBlocked;
    [SerializeField] private float currentSpreadDegrees;
    [SerializeField] private Vector3 debugLastAimPoint;
    [SerializeField] private Vector3 debugLastHitPoint;
    [SerializeField] private string debugLastHitName;
    [SerializeField] private int totalShotsFired;
    [SerializeField] private int totalWorldHits;
    [SerializeField] private int totalRegisteredHits;
    [SerializeField] private int totalCriticalHits;
    [SerializeField] private int totalMisses;
    [SerializeField] private int totalBlockedShots;
    [SerializeField] private float totalDamageDealt;
    [SerializeField] private float lastDamageDealt;
    [SerializeField] private bool lastShotRegistered;
    [SerializeField] private bool lastShotCritical;
    [SerializeField] private bool lastShotBlocked;
    [SerializeField] private float lastHitDistance;
    [SerializeField] private string lastRegisteredTargetName;
    [SerializeField] private float metricsWindowSeconds = 5f;

    private PlayerInputHandler input;
    private ThirdPersonMotor motor;
    private WeaponDefinition runtimeFallbackWeapon;
    private float nextFireTime;
    private float reloadEndTime;
    private float reloadStartTime;
    private float reloadDuration;
    private float spreadAddDegrees;
    private float lastFireTime = -999f;
    private float rollingDamageTotal;
    private readonly Queue<float> recentShotTimes = new Queue<float>();
    private readonly Queue<float> recentRegisteredHitTimes = new Queue<float>();
    private readonly Queue<float> recentDamageTimes = new Queue<float>();
    private readonly Queue<float> recentDamageAmounts = new Queue<float>();

    public WeaponDefinition ActiveWeapon => weapon != null ? weapon : GetRuntimeFallbackWeapon();
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => ActiveWeapon.magazineSize;
    public bool IsReloading => isReloading;
    public bool MuzzleBlocked => muzzleBlocked;
    public float CurrentSpreadDegrees => currentSpreadDegrees;
    public string WeaponState => weaponState;
    public float ReloadTimeRemaining => isReloading ? Mathf.Max(0f, reloadEndTime - Time.time) : 0f;
    public float ReloadDuration => reloadDuration;
    public float ReloadProgress01 => isReloading && reloadDuration > 0f ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration) : 1f;
    public float FireCooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public float SpreadAddDegrees => spreadAddDegrees;
    public int TotalShotsFired => totalShotsFired;
    public int TotalWorldHits => totalWorldHits;
    public int TotalRegisteredHits => totalRegisteredHits;
    public int TotalCriticalHits => totalCriticalHits;
    public int TotalMisses => totalMisses;
    public int TotalBlockedShots => totalBlockedShots;
    public float TotalDamageDealt => totalDamageDealt;
    public float LastDamageDealt => lastDamageDealt;
    public bool LastShotRegistered => lastShotRegistered;
    public bool LastShotCritical => lastShotCritical;
    public bool LastShotBlocked => lastShotBlocked;
    public float LastHitDistance => lastHitDistance;
    public string LastHitName => debugLastHitName;
    public string LastRegisteredTargetName => lastRegisteredTargetName;
    public float Accuracy01 => totalShotsFired > 0 ? totalRegisteredHits / (float)totalShotsFired : 0f;
    public float WorldHitRate01 => totalShotsFired > 0 ? totalWorldHits / (float)totalShotsFired : 0f;
    public float CriticalRate01 => totalRegisteredHits > 0 ? totalCriticalHits / (float)totalRegisteredHits : 0f;
    public float RecentAccuracy01 => recentShotTimes.Count > 0 ? recentRegisteredHitTimes.Count / (float)recentShotTimes.Count : 0f;
    public float RecentDps => metricsWindowSeconds > 0f ? rollingDamageTotal / metricsWindowSeconds : 0f;
    public float ObservedRpm => metricsWindowSeconds > 0f ? recentShotTimes.Count / metricsWindowSeconds * 60f : 0f;
    public float MetricsWindowSeconds => metricsWindowSeconds;
    public float RawBodyDps => ActiveWeapon.bodyDamage * ActiveWeapon.fireRate / 60f;
    public float SustainedBodyDps
    {
        get
        {
            WeaponDefinition activeWeapon = ActiveWeapon;
            float shotInterval = activeWeapon.SecondsPerShot();
            float magazineDamage = activeWeapon.bodyDamage * activeWeapon.magazineSize;
            float cycleTime = (Mathf.Max(activeWeapon.magazineSize, 1) - 1) * shotInterval + activeWeapon.reloadTime;
            return cycleTime > 0f ? magazineDamage / cycleTime : 0f;
        }
    }

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        motor = GetComponent<ThirdPersonMotor>();

        if (animationController == null)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<ThirdPersonCameraController>();
        }

        if (muzzle == null)
        {
            muzzle = transform.Find("WeaponMuzzle");
        }

        if (muzzle == null)
        {
            GameObject muzzleObject = new GameObject("WeaponMuzzle");
            muzzleObject.transform.SetParent(transform);
            muzzleObject.transform.localPosition = new Vector3(0.36f, 1.25f, 0.72f);
            muzzleObject.transform.localRotation = Quaternion.identity;
            muzzle = muzzleObject.transform;
        }

        currentAmmo = ActiveWeapon.magazineSize;
        weaponState = "Ready";
    }

    private void Update()
    {
        RecoverSpread(Time.deltaTime);
        TrimRecentMetrics();
        UpdateReload();
        UpdateMuzzleBlockedPreview();
        HandleReloadInput();
        HandleFireInput();
        UpdateDebugState();
        UpdateSpreadPreview();
    }

    private WeaponDefinition GetRuntimeFallbackWeapon()
    {
        if (runtimeFallbackWeapon == null)
        {
            runtimeFallbackWeapon = WeaponDefinition.CreateRuntimePrototypeRifle();
        }

        return runtimeFallbackWeapon;
    }

    private void RecoverSpread(float deltaTime)
    {
        spreadAddDegrees = Mathf.MoveTowards(spreadAddDegrees, 0f, ActiveWeapon.spreadRecoveryPerSecond * deltaTime);
    }

    private void UpdateReload()
    {
        if (!isReloading || Time.time < reloadEndTime)
        {
            return;
        }

        currentAmmo = ActiveWeapon.magazineSize;
        isReloading = false;
        weaponState = "Ready";
    }

    private void HandleReloadInput()
    {
        if (input.ReloadTriggered)
        {
            TryStartReload();
            input.ConsumeReload();
        }
    }

    private void HandleFireInput()
    {
        if (isReloading)
        {
            return;
        }

        WeaponDefinition activeWeapon = ActiveWeapon;
        bool wantsFire = activeWeapon.IsAutomatic ? input.AttackPressed : input.AttackTriggered;
        if (!wantsFire)
        {
            return;
        }

        if (input.AttackTriggered)
        {
            input.ConsumeAttack();
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            TryStartReload();
            return;
        }

        Fire(activeWeapon);
    }

    private void TryStartReload()
    {
        if (isReloading || currentAmmo >= ActiveWeapon.magazineSize)
        {
            return;
        }

        bool empty = currentAmmo <= 0;
        isReloading = true;
        reloadDuration = empty ? ActiveWeapon.emptyReloadTime : ActiveWeapon.reloadTime;
        reloadStartTime = Time.time;
        reloadEndTime = Time.time + reloadDuration;
        weaponState = empty ? "Empty Reload" : "Reload";
    }

    private void Fire(WeaponDefinition activeWeapon)
    {
        currentAmmo--;
        totalShotsFired++;
        TrackRecentShot();
        nextFireTime = Time.time + activeWeapon.SecondsPerShot();
        lastFireTime = Time.time;
        spreadAddDegrees = Mathf.Min(activeWeapon.maxSpreadAddDegrees, spreadAddDegrees + activeWeapon.spreadPerShot + activeWeapon.recoilSpreadAddDegrees);
        animationController?.TriggerPrimaryAttack();

        Vector3 aimPoint = ResolveAimPoint(activeWeapon);
        Vector3 shotOrigin = muzzle.position;
        Vector3 toAimPoint = aimPoint - shotOrigin;
        if (toAimPoint.sqrMagnitude < 0.001f)
        {
            toAimPoint = transform.forward * activeWeapon.maxRange;
            aimPoint = shotOrigin + toAimPoint;
        }

        Vector3 baseDirection = toAimPoint.normalized;
        float targetDistance = toAimPoint.magnitude;
        RaycastHit muzzleHit;
        muzzleBlocked = IsMuzzleBlocked(shotOrigin, baseDirection, targetDistance, activeWeapon, out muzzleHit);
        cameraController?.SetMuzzleBlocked(muzzleBlocked);

        Ray shotRay;
        RaycastHit shotHit;
        bool didHit;

        if (muzzleBlocked)
        {
            shotRay = new Ray(shotOrigin, baseDirection);
            shotHit = muzzleHit;
            didHit = true;
        }
        else
        {
            Vector3 spreadDirection = ApplySpread(baseDirection, CalculateSpread(activeWeapon));
            shotRay = new Ray(shotOrigin, spreadDirection);
            didHit = Physics.Raycast(shotRay, out shotHit, activeWeapon.maxRange, activeWeapon.hitMask, QueryTriggerInteraction.Ignore);
        }

        ApplyRecoil(activeWeapon);
        ResolveHit(activeWeapon, shotRay, didHit, shotHit);
    }

    private Vector3 ResolveAimPoint(WeaponDefinition activeWeapon)
    {
        if (cameraController != null)
        {
            Vector3 cameraAimPoint = cameraController.AimPoint;
            if (cameraAimPoint != Vector3.zero)
            {
                debugLastAimPoint = cameraAimPoint;
                return cameraAimPoint;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return muzzle.position + transform.forward * activeWeapon.maxRange;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.maxRange, activeWeapon.hitMask, QueryTriggerInteraction.Ignore))
        {
            debugLastAimPoint = hit.point;
            return hit.point;
        }

        debugLastAimPoint = ray.GetPoint(activeWeapon.maxRange);
        return debugLastAimPoint;
    }

    private void UpdateMuzzleBlockedPreview()
    {
        WeaponDefinition activeWeapon = ActiveWeapon;
        Vector3 aimPoint = ResolveAimPoint(activeWeapon);
        Vector3 toAimPoint = aimPoint - muzzle.position;
        if (toAimPoint.sqrMagnitude < 0.001f)
        {
            muzzleBlocked = false;
            cameraController?.SetMuzzleBlocked(false);
            return;
        }

        Vector3 direction = toAimPoint.normalized;
        float distance = toAimPoint.magnitude;
        muzzleBlocked = IsMuzzleBlocked(muzzle.position, direction, distance, activeWeapon, out _);
        cameraController?.SetMuzzleBlocked(muzzleBlocked);
    }

    private bool IsMuzzleBlocked(Vector3 origin, Vector3 direction, float distance, WeaponDefinition activeWeapon, out RaycastHit hit)
    {
        if (distance <= 0.05f)
        {
            hit = default(RaycastHit);
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, activeWeapon.hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider == null || candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            hit = candidate;
            return true;
        }

        hit = default(RaycastHit);
        return false;
    }

    private float CalculateSpread(WeaponDefinition activeWeapon)
    {
        currentSpreadDegrees = input.AimPressed ? activeWeapon.adsSpreadDegrees : activeWeapon.hipSpreadDegrees;

        if (motor.GetHorizontalVelocity().magnitude > 0.2f)
        {
            currentSpreadDegrees += activeWeapon.movingSpreadAddDegrees;
        }

        if (!motor.IsGrounded())
        {
            currentSpreadDegrees += activeWeapon.airborneSpreadAddDegrees;
        }

        if (motor.IsSliding())
        {
            currentSpreadDegrees += activeWeapon.slideSpreadAddDegrees;
        }

        currentSpreadDegrees += spreadAddDegrees;
        return currentSpreadDegrees;
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
    {
        if (spreadDegrees <= 0.001f)
        {
            return direction;
        }

        Transform cameraTransform = cameraController != null ? cameraController.transform : Camera.main != null ? Camera.main.transform : transform;
        Vector2 random = Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
        Vector3 spreadDirection = direction + cameraTransform.right * random.x + cameraTransform.up * random.y;
        return spreadDirection.normalized;
    }

    private void ApplyRecoil(WeaponDefinition activeWeapon)
    {
        if (cameraController == null)
        {
            return;
        }

        float yawKick = Random.Range(-activeWeapon.cameraRecoilYaw, activeWeapon.cameraRecoilYaw);
        cameraController.AddRecoil(activeWeapon.cameraRecoilPitch, yawKick);
    }

    private void ResolveHit(WeaponDefinition activeWeapon, Ray shotRay, bool didHit, RaycastHit shotHit)
    {
        Vector3 endPoint = shotRay.GetPoint(activeWeapon.maxRange);
        debugLastHitName = string.Empty;
        lastRegisteredTargetName = string.Empty;
        lastDamageDealt = 0f;
        lastShotRegistered = false;
        lastShotCritical = false;
        lastShotBlocked = muzzleBlocked;
        if (muzzleBlocked)
        {
            totalBlockedShots++;
        }

        if (didHit)
        {
            totalWorldHits++;
            endPoint = shotHit.point;
            debugLastHitPoint = shotHit.point;
            debugLastHitName = shotHit.collider != null ? shotHit.collider.name : string.Empty;
            lastHitDistance = shotHit.distance;

            bool critical = shotHit.collider != null && shotHit.collider.name.ToLowerInvariant().Contains("head");
            float damage = activeWeapon.EvaluateDamage(shotHit.distance, critical);
            IDamageable damageable = FindDamageable(shotHit.collider);
            bool registered = damageable != null;

            if (registered)
            {
                damageable.ApplyDamage(new DamageInfo(damage, shotHit.point, shotHit.normal, gameObject, activeWeapon, critical, totalShotsFired));
                totalRegisteredHits++;
                if (critical)
                {
                    totalCriticalHits++;
                }

                totalDamageDealt += damage;
                lastDamageDealt = damage;
                lastShotRegistered = true;
                lastShotCritical = critical;
                lastRegisteredTargetName = shotHit.collider.GetComponentInParent<TargetDummy>() != null
                    ? shotHit.collider.GetComponentInParent<TargetDummy>().name
                    : debugLastHitName;
                TrackRecentRegisteredHit();
                TrackRecentDamage(damage);
            }

            TPSReticleHUD.NotifyHit(registered, damage, critical);
            SpawnImpactMarker(shotHit.point, shotHit.normal, registered, critical);
        }
        else
        {
            totalMisses++;
            debugLastHitPoint = endPoint;
            lastHitDistance = activeWeapon.maxRange;
            TPSReticleHUD.NotifyHit(false, 0f, false);
        }

        if (drawDebugShots)
        {
            Debug.DrawLine(shotRay.origin, endPoint, didHit ? Color.yellow : Color.white, debugShotTime);
        }
    }

    private void TrackRecentShot()
    {
        recentShotTimes.Enqueue(Time.time);
        TrimRecentMetrics();
    }

    private void TrackRecentRegisteredHit()
    {
        recentRegisteredHitTimes.Enqueue(Time.time);
        TrimRecentMetrics();
    }

    private void TrackRecentDamage(float damage)
    {
        recentDamageTimes.Enqueue(Time.time);
        recentDamageAmounts.Enqueue(damage);
        rollingDamageTotal += damage;
        TrimRecentMetrics();
    }

    private void TrimRecentMetrics()
    {
        float cutoff = Time.time - Mathf.Max(metricsWindowSeconds, 0.01f);
        TrimTimeQueue(recentShotTimes, cutoff);
        TrimTimeQueue(recentRegisteredHitTimes, cutoff);

        while (recentDamageTimes.Count > 0 && recentDamageTimes.Peek() < cutoff)
        {
            recentDamageTimes.Dequeue();
            rollingDamageTotal -= recentDamageAmounts.Dequeue();
        }

        if (rollingDamageTotal < 0f)
        {
            rollingDamageTotal = 0f;
        }
    }

    private static void TrimTimeQueue(Queue<float> queue, float cutoff)
    {
        while (queue.Count > 0 && queue.Peek() < cutoff)
        {
            queue.Dequeue();
        }
    }

    private void SpawnImpactMarker(Vector3 point, Vector3 normal, bool hitDamageable, bool critical)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = hitDamageable ? "Hit Marker" : "Impact Marker";
        marker.transform.position = point + normal * 0.015f;
        marker.transform.localScale = Vector3.one * (critical ? 0.12f : 0.08f);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.material.color = critical ? Color.yellow : hitDamageable ? Color.red : Color.white;
        }

        Destroy(marker, 0.25f);
    }

    private static IDamageable FindDamageable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private void UpdateDebugState()
    {
        if (isReloading) weaponState = weaponState.StartsWith("Empty") ? "Empty Reload" : "Reload";
        else if (currentAmmo <= 0) weaponState = "Empty";
        else if (Time.time - lastFireTime < 0.08f) weaponState = "Firing";
        else weaponState = "Ready";
    }

    private void UpdateSpreadPreview()
    {
        CalculateSpread(ActiveWeapon);
    }
}
