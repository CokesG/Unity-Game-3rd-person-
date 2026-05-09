using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class TPSReticleHUD : MonoBehaviour
{
    private static TPSReticleHUD instance;

    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private ThirdPersonCameraController cameraController;
    [SerializeField] private ThirdPersonMotor motor;
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private float spreadPixelMultiplier = 9f;
    [SerializeField] private float minCrosshairGap = 5f;
    [SerializeField] private float maxCrosshairGap = 34f;
    [SerializeField] private bool showMetricsOverlay = true;
    [SerializeField] private bool showTargetTable = true;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private int maxTargetsShown = 4;

    private float hitmarkerTimer;
    private float hitDamage;
    private bool hitConfirmed;
    private bool criticalHit;
    private float nextTargetRefreshTime;
    private TargetDummy[] targetDummies = new TargetDummy[0];
    private readonly StringBuilder debugBuilder = new StringBuilder(2048);

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (weaponController == null)
        {
            weaponController = FindAnyObjectByType<PlayerWeaponController>();
        }

        if (cameraController == null)
        {
            cameraController = FindAnyObjectByType<ThirdPersonCameraController>();
        }

        if (motor == null)
        {
            motor = FindAnyObjectByType<ThirdPersonMotor>();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        hitmarkerTimer = Mathf.Max(0f, hitmarkerTimer - Time.deltaTime);

        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            showMetricsOverlay = !showMetricsOverlay;
        }

        if (Time.time >= nextTargetRefreshTime)
        {
            targetDummies = FindObjectsByType<TargetDummy>(FindObjectsInactive.Exclude);
            nextTargetRefreshTime = Time.time + Mathf.Max(targetRefreshInterval, 0.05f);
        }
    }

    private void OnGUI()
    {
        DrawCrosshair();
        DrawHitmarker();
        DrawWeaponReadout();
        DrawMetricsOverlay();
    }

    private void DrawCrosshair()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float spread = weaponController != null ? weaponController.CurrentSpreadDegrees : 0.5f;
        float gap = Mathf.Clamp(minCrosshairGap + spread * spreadPixelMultiplier, minCrosshairGap, maxCrosshairGap);
        float length = 6f;
        float thickness = 1.5f;
        bool blocked = cameraController != null && cameraController.IsMuzzleBlocked;
        Color color = blocked ? new Color(1f, 0.25f, 0.2f, 0.95f) : new Color(1f, 1f, 1f, 0.9f);

        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - gap - length, thickness, length), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, length), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - gap - length, cy - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawHitmarker()
    {
        if (hitmarkerTimer <= 0f)
        {
            return;
        }

        float alpha = Mathf.Clamp01(hitmarkerTimer / 0.16f);
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float size = criticalHit ? 18f : 14f;
        float thickness = 2f;
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.color = hitConfirmed
            ? new Color(criticalHit ? 1f : 0.95f, criticalHit ? 0.85f : 1f, criticalHit ? 0.2f : 1f, alpha)
            : new Color(1f, 1f, 1f, alpha * 0.35f);

        GUIUtility.RotateAroundPivot(45f, new Vector2(cx, cy));
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size * 0.5f, thickness, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - thickness * 0.5f, size, thickness), Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;

        if (hitConfirmed && hitDamage > 0f)
        {
            GUI.Label(new Rect(cx + 18f, cy - 32f, 80f, 24f), Mathf.RoundToInt(hitDamage).ToString());
        }

        GUI.color = oldColor;
    }

    private void DrawWeaponReadout()
    {
        if (!showDebugReadout || weaponController == null)
        {
            return;
        }

        string movement = motor != null ? motor.GetMovementMode() : "No Motor";
        string weapon = weaponController.ActiveWeapon != null ? weaponController.ActiveWeapon.displayName : "No Weapon";
        string ammo = $"{weaponController.CurrentAmmo}/{weaponController.MagazineSize}";
        string state = weaponController.WeaponState;
        string blocked = weaponController.MuzzleBlocked ? "BLOCKED" : "Clear";

        GUI.Label(new Rect(24f, 24f, 420f, 24f), $"{movement} | {weapon} | {ammo} | {state} | {blocked}");
    }

    private void DrawMetricsOverlay()
    {
        if (!showMetricsOverlay || weaponController == null)
        {
            if (showDebugReadout)
            {
                GUI.Label(new Rect(24f, 48f, 180f, 24f), "F1: gun debug");
            }

            return;
        }

        BuildMetricsText();

        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;
        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.Box(new Rect(20f, 52f, 560f, 360f), GUIContent.none);
        GUI.color = oldColor;
        GUI.contentColor = Color.white;
        GUI.Label(new Rect(32f, 62f, 540f, 340f), debugBuilder.ToString());
        GUI.contentColor = oldContentColor;
    }

    private void BuildMetricsText()
    {
        debugBuilder.Length = 0;

        WeaponDefinition weapon = weaponController.ActiveWeapon;
        TargetDummy focusTarget = ResolveFocusTarget();
        float focusHealth = focusTarget != null ? Mathf.Max(focusTarget.CurrentHealth, 0.01f) : 250f;
        float ttkCurrentBody = EstimateTimeToKill(focusHealth, weapon.bodyDamage, weapon.magazineSize, weapon.SecondsPerShot(), weapon.reloadTime);
        float ttkFullBody = EstimateTimeToKill(focusTarget != null ? focusTarget.MaxHealth : 250f, weapon.bodyDamage, weapon.magazineSize, weapon.SecondsPerShot(), weapon.reloadTime);
        float ttkFullCrit = EstimateTimeToKill(focusTarget != null ? focusTarget.MaxHealth : 250f, weapon.bodyDamage * weapon.headshotMultiplier, weapon.magazineSize, weapon.SecondsPerShot(), weapon.reloadTime);

        debugBuilder.AppendLine("GUN TUNING DEBUG  [F1 hides]");
        debugBuilder.Append("Weapon: ").Append(weapon.displayName)
            .Append(" | State: ").Append(weaponController.WeaponState)
            .Append(" | Ammo: ").Append(weaponController.CurrentAmmo).Append('/').Append(weaponController.MagazineSize)
            .AppendLine();

        debugBuilder.Append("Reload: ").Append(weaponController.IsReloading ? $"{weaponController.ReloadTimeRemaining:0.00}s" : "ready")
            .Append(" | Progress: ").Append(FormatPercent(weaponController.ReloadProgress01))
            .Append(" | Fire CD: ").Append(weaponController.FireCooldownRemaining.ToString("0.00")).Append('s')
            .Append(" | Muzzle: ").Append(weaponController.MuzzleBlocked ? "BLOCKED" : "clear")
            .AppendLine();

        debugBuilder.Append("Shots: ").Append(weaponController.TotalShotsFired)
            .Append(" | Registered: ").Append(weaponController.TotalRegisteredHits)
            .Append(" | Crits: ").Append(weaponController.TotalCriticalHits)
            .Append(" | World: ").Append(weaponController.TotalWorldHits)
            .Append(" | Miss: ").Append(weaponController.TotalMisses)
            .Append(" | Blocked: ").Append(weaponController.TotalBlockedShots)
            .AppendLine();

        debugBuilder.Append("Accuracy: ").Append(FormatPercent(weaponController.Accuracy01))
            .Append(" | Recent: ").Append(FormatPercent(weaponController.RecentAccuracy01))
            .Append(" | Crit rate: ").Append(FormatPercent(weaponController.CriticalRate01))
            .Append(" | Damage: ").Append(weaponController.TotalDamageDealt.ToString("0"))
            .AppendLine();

        debugBuilder.Append("Observed RPM: ").Append(weaponController.ObservedRpm.ToString("0"))
            .Append(" / tuned ").Append(weapon.fireRate.ToString("0"))
            .Append(" | Recent DPS: ").Append(weaponController.RecentDps.ToString("0"))
            .Append(" | Raw DPS: ").Append(weaponController.RawBodyDps.ToString("0"))
            .Append(" | Sustained DPS: ").Append(weaponController.SustainedBodyDps.ToString("0"))
            .AppendLine();

        debugBuilder.Append("Spread: ").Append(weaponController.CurrentSpreadDegrees.ToString("0.00"))
            .Append(" deg | Add: ").Append(weaponController.SpreadAddDegrees.ToString("0.00"))
            .Append(" | Hip/ADS: ").Append(weapon.hipSpreadDegrees.ToString("0.00"))
            .Append('/').Append(weapon.adsSpreadDegrees.ToString("0.00"))
            .Append(" | Recover: ").Append(weapon.spreadRecoveryPerSecond.ToString("0.0")).Append("/s")
            .AppendLine();

        debugBuilder.Append("Last: ").Append(weaponController.LastShotRegistered ? "REG" : "no-reg")
            .Append(weaponController.LastShotCritical ? " CRIT" : string.Empty)
            .Append(weaponController.LastShotBlocked ? " BLOCKED" : string.Empty)
            .Append(" | Dmg: ").Append(weaponController.LastDamageDealt.ToString("0.0"))
            .Append(" | Dist: ").Append(weaponController.LastHitDistance.ToString("0.0")).Append("m")
            .Append(" | Hit: ").Append(string.IsNullOrEmpty(weaponController.LastHitName) ? "-" : weaponController.LastHitName)
            .Append(" | Target: ").Append(string.IsNullOrEmpty(weaponController.LastRegisteredTargetName) ? "-" : weaponController.LastRegisteredTargetName)
            .AppendLine();

        debugBuilder.Append("TTK body current/full: ").Append(FormatSeconds(ttkCurrentBody))
            .Append(" / ").Append(FormatSeconds(ttkFullBody))
            .Append(" | full crit: ").Append(FormatSeconds(ttkFullCrit))
            .AppendLine();

        if (focusTarget != null)
        {
            debugBuilder.Append("Focus: ").Append(focusTarget.name)
                .Append(" | HP ").Append(focusTarget.CurrentHealth.ToString("0")).Append('/').Append(focusTarget.MaxHealth.ToString("0"))
                .Append(" | life dmg ").Append(focusTarget.CurrentLifeDamageTaken.ToString("0"))
                .Append(" | session dmg ").Append(focusTarget.SessionDamageTaken.ToString("0"))
                .Append(" | hits ").Append(focusTarget.CurrentLifeHits).Append('/').Append(focusTarget.SessionHits)
                .Append(" | crit ").Append(focusTarget.CurrentLifeCriticalHits).Append('/').Append(focusTarget.SessionCriticalHits)
                .Append(" | last ").Append(focusTarget.LastDamage.ToString("0.0")).Append(focusTarget.LastHitWasCritical ? " CRIT" : string.Empty)
                .AppendLine();
        }

        if (showTargetTable)
        {
            AppendTargetTable();
        }
    }

    private void AppendTargetTable()
    {
        if (targetDummies == null || targetDummies.Length == 0)
        {
            debugBuilder.AppendLine("Targets: none");
            return;
        }

        Array.Sort(targetDummies, CompareTargetsForDebug);
        int count = Mathf.Min(maxTargetsShown, targetDummies.Length);
        debugBuilder.AppendLine("Targets:");
        for (int i = 0; i < count; i++)
        {
            TargetDummy target = targetDummies[i];
            if (target == null)
            {
                continue;
            }

            debugBuilder.Append("  ").Append(target.name)
                .Append(" HP ").Append(target.CurrentHealth.ToString("0")).Append('/').Append(target.MaxHealth.ToString("0"))
                .Append(" | life ").Append(target.CurrentLifeDamageTaken.ToString("0"))
                .Append(" | total ").Append(target.SessionDamageTaken.ToString("0"))
                .Append(" | reg ").Append(target.SessionHits)
                .Append(" | crit ").Append(target.SessionCriticalHits)
                .Append(" | defeats ").Append(target.SessionDefeats)
                .Append(" | last ").Append(target.LastDamage.ToString("0.0")).Append(target.LastHitWasCritical ? " CRIT" : string.Empty)
                .AppendLine();
        }
    }

    private int CompareTargetsForDebug(TargetDummy a, TargetDummy b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int recentCompare = b.LastHitTime.CompareTo(a.LastHitTime);
        if (recentCompare != 0)
        {
            return recentCompare;
        }

        Transform reference = motor != null ? motor.transform : transform;
        float aDist = Vector3.SqrMagnitude(a.transform.position - reference.position);
        float bDist = Vector3.SqrMagnitude(b.transform.position - reference.position);
        return aDist.CompareTo(bDist);
    }

    private TargetDummy ResolveFocusTarget()
    {
        if (targetDummies == null || targetDummies.Length == 0)
        {
            return null;
        }

        TargetDummy bestRecent = null;
        float bestHitTime = -1f;
        foreach (TargetDummy target in targetDummies)
        {
            if (target == null)
            {
                continue;
            }

            if (target.LastHitTime > bestHitTime)
            {
                bestRecent = target;
                bestHitTime = target.LastHitTime;
            }
        }

        if (bestRecent != null && bestHitTime > 0f)
        {
            return bestRecent;
        }

        TargetDummy closest = null;
        float closestDistance = float.MaxValue;
        Transform reference = motor != null ? motor.transform : transform;
        foreach (TargetDummy target in targetDummies)
        {
            if (target == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(target.transform.position - reference.position);
            if (distance < closestDistance)
            {
                closest = target;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private static float EstimateTimeToKill(float health, float damagePerShot, int magazineSize, float shotInterval, float reloadTime)
    {
        if (health <= 0f || damagePerShot <= 0f)
        {
            return 0f;
        }

        int shotsToKill = Mathf.CeilToInt(health / damagePerShot);
        if (shotsToKill <= 1)
        {
            return 0f;
        }

        int reloadsBeforeKill = Mathf.Max(0, (shotsToKill - 1) / Mathf.Max(magazineSize, 1));
        return (shotsToKill - 1) * shotInterval + reloadsBeforeKill * reloadTime;
    }

    private static string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.0") + "%";
    }

    private static string FormatSeconds(float seconds)
    {
        return seconds.ToString("0.00") + "s";
    }

    public static void NotifyHit(bool confirmed, float damage, bool critical)
    {
        if (instance == null)
        {
            return;
        }

        instance.hitConfirmed = confirmed;
        instance.hitDamage = damage;
        instance.criticalHit = critical;
        instance.hitmarkerTimer = confirmed ? 0.16f : 0.08f;
    }
}
