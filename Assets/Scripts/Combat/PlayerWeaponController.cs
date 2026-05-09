using UnityEngine;

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

    private PlayerInputHandler input;
    private ThirdPersonMotor motor;
    private WeaponDefinition runtimeFallbackWeapon;
    private float nextFireTime;
    private float reloadEndTime;
    private float spreadAddDegrees;
    private float lastFireTime = -999f;

    public WeaponDefinition ActiveWeapon => weapon != null ? weapon : GetRuntimeFallbackWeapon();
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => ActiveWeapon.magazineSize;
    public bool IsReloading => isReloading;
    public bool MuzzleBlocked => muzzleBlocked;
    public float CurrentSpreadDegrees => currentSpreadDegrees;
    public string WeaponState => weaponState;

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
        reloadEndTime = Time.time + (empty ? ActiveWeapon.emptyReloadTime : ActiveWeapon.reloadTime);
        weaponState = empty ? "Empty Reload" : "Reload";
    }

    private void Fire(WeaponDefinition activeWeapon)
    {
        currentAmmo--;
        nextFireTime = Time.time + activeWeapon.SecondsPerShot();
        lastFireTime = Time.time;
        spreadAddDegrees = Mathf.Min(activeWeapon.maxSpreadAddDegrees, spreadAddDegrees + activeWeapon.spreadPerShot + activeWeapon.recoilSpreadAddDegrees);
        animationController?.TriggerPrimaryAttack();

        Vector3 aimPoint = ResolveAimPoint(activeWeapon);
        Vector3 shotOrigin = muzzle.position;
        Vector3 baseDirection = (aimPoint - shotOrigin).normalized;
        float targetDistance = Vector3.Distance(shotOrigin, aimPoint);
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
            debugLastAimPoint = cameraController.AimPoint;
            return cameraController.AimPoint;
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
        Vector3 direction = (aimPoint - muzzle.position).normalized;
        float distance = Vector3.Distance(muzzle.position, aimPoint);
        muzzleBlocked = IsMuzzleBlocked(muzzle.position, direction, distance, activeWeapon, out _);
        cameraController?.SetMuzzleBlocked(muzzleBlocked);
    }

    private bool IsMuzzleBlocked(Vector3 origin, Vector3 direction, float distance, WeaponDefinition activeWeapon, out RaycastHit hit)
    {
        if (distance <= 0.05f)
        {
            hit = default;
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

        hit = default;
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

        if (didHit)
        {
            endPoint = shotHit.point;
            debugLastHitPoint = shotHit.point;
            debugLastHitName = shotHit.collider != null ? shotHit.collider.name : string.Empty;

            bool critical = shotHit.collider != null && shotHit.collider.name.ToLowerInvariant().Contains("head");
            float damage = activeWeapon.EvaluateDamage(shotHit.distance, critical);
            IDamageable damageable = FindDamageable(shotHit.collider);
            damageable?.ApplyDamage(new DamageInfo(damage, shotHit.point, shotHit.normal, gameObject, activeWeapon, critical));
            TPSReticleHUD.NotifyHit(damageable != null, damage, critical);
            SpawnImpactMarker(shotHit.point, shotHit.normal, damageable != null, critical);
        }
        else
        {
            debugLastHitPoint = endPoint;
            TPSReticleHUD.NotifyHit(false, 0f, false);
        }

        if (drawDebugShots)
        {
            Debug.DrawLine(shotRay.origin, endPoint, didHit ? Color.yellow : Color.white, debugShotTime);
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
