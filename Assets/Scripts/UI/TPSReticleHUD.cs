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
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private float spreadPixelMultiplier = 9f;
    [SerializeField] private float minCrosshairGap = 5f;
    [SerializeField] private float maxCrosshairGap = 34f;
    [SerializeField] private bool showMetricsOverlay = true;
    [SerializeField] private bool showTargetTable = true;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private int maxTargetsShown = 4;

    private const float SettingsPanelWidth = 460f;
    private const float SettingsPanelHeight = 500f;
    private static Texture2D circleTexture;
    private static Texture2D ringTexture;
    private Vector2 settingsScroll;
    private float hitmarkerTimer;
    private float hitDamage;
    private bool hitConfirmed;
    private bool criticalHit;
    private float nextTargetRefreshTime;
    private TargetDummy[] targetDummies = new TargetDummy[0];
    private readonly StringBuilder debugBuilder = new StringBuilder(4096);

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

        if (animationController == null)
        {
            animationController = FindAnyObjectByType<PlayerAnimationController>();
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

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TpsPlayerSettings.SettingsOpen = !TpsPlayerSettings.SettingsOpen;
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
        DrawSettingsPanel();
    }

    private void DrawCrosshair()
    {
        if (TpsPlayerSettings.SettingsOpen)
        {
            return;
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float spread = weaponController != null ? weaponController.CurrentSpreadDegrees : 0.5f;
        float baseGap = TpsPlayerSettings.ReticleGap;
        float gap = Mathf.Clamp(baseGap + spread * spreadPixelMultiplier, baseGap, maxCrosshairGap);
        float length = TpsPlayerSettings.ReticleSize;
        float thickness = TpsPlayerSettings.ReticleThickness;
        bool blocked = cameraController != null && cameraController.IsMuzzleBlocked;
        Color color = blocked ? new Color(1f, 0.25f, 0.2f, 0.95f) : TpsPlayerSettings.ReticleColor;

        Color oldColor = GUI.color;
        DrawReticleShape(cx, cy, gap, length, thickness, color);
        GUI.color = oldColor;
    }

    private void DrawReticleShape(float cx, float cy, float gap, float size, float thickness, Color color)
    {
        if (TpsPlayerSettings.ReticleOutline)
        {
            Color outline = new Color(0f, 0f, 0f, Mathf.Clamp01(color.a * 0.75f));
            DrawReticleShapeRaw(cx, cy, gap, size, thickness + 2f, outline);
        }

        DrawReticleShapeRaw(cx, cy, gap, size, thickness, color);
    }

    private void DrawReticleShapeRaw(float cx, float cy, float gap, float size, float thickness, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;

        switch (TpsPlayerSettings.ReticleMode)
        {
            case ReticleStyle.Dot:
                DrawTextureCentered(GetCircleTexture(), cx, cy, Mathf.Max(size, thickness + 2f));
                break;
            case ReticleStyle.Circle:
                DrawTextureCentered(GetRingTexture(), cx, cy, Mathf.Max((gap + size) * 2f, 8f));
                break;
            case ReticleStyle.CircleDot:
                DrawTextureCentered(GetRingTexture(), cx, cy, Mathf.Max((gap + size) * 2f, 8f));
                DrawTextureCentered(GetCircleTexture(), cx, cy, Mathf.Max(thickness + 2f, 3f));
                break;
            default:
                DrawCrosshairLines(cx, cy, gap, size, thickness);
                break;
        }

        GUI.color = previous;
    }

    private void DrawCrosshairLines(float cx, float cy, float gap, float length, float thickness)
    {
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - gap - length, thickness, length), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, length), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - gap - length, cy - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
    }

    private void DrawTextureCentered(Texture2D texture, float cx, float cy, float size)
    {
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), texture);
    }

    private static Texture2D GetCircleTexture()
    {
        if (circleTexture == null)
        {
            circleTexture = BuildCircleTexture(false);
        }

        return circleTexture;
    }

    private static Texture2D GetRingTexture()
    {
        if (ringTexture == null)
        {
            ringTexture = BuildCircleTexture(true);
        }

        return ringTexture;
    }

    private static Texture2D BuildCircleTexture(bool ring)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;
        float inner = size * 0.27f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = ring
                    ? Mathf.Clamp01(radius - distance) * Mathf.Clamp01(distance - inner)
                    : Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
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
        string reload = weaponController.IsReloading ? $"Reload {weaponController.ReloadTimeRemaining:0.00}s" : "Ready";

        GUI.Label(new Rect(24f, 24f, 560f, 24f), $"{movement} | {weapon} | {ammo} | {state} | {reload} | {blocked}");
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
        GUI.Box(new Rect(20f, 52f, 660f, 500f), GUIContent.none);
        GUI.color = oldColor;
        GUI.contentColor = Color.white;
        GUI.Label(new Rect(32f, 62f, 636f, 480f), debugBuilder.ToString());
        GUI.contentColor = oldContentColor;
    }

    private void DrawSettingsPanel()
    {
        if (!TpsPlayerSettings.SettingsOpen)
        {
            return;
        }

        Rect panel = new Rect(
            (Screen.width - SettingsPanelWidth) * 0.5f,
            Mathf.Max(24f, (Screen.height - Mathf.Min(SettingsPanelHeight, Screen.height - 48f)) * 0.5f),
            SettingsPanelWidth,
            Mathf.Min(SettingsPanelHeight, Screen.height - 48f));

        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;
        GUI.color = new Color(0.03f, 0.035f, 0.04f, 0.92f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = oldColor;
        GUI.contentColor = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 22f, panel.y + 18f, panel.width - 44f, panel.height - 36f));
        settingsScroll = GUILayout.BeginScrollView(settingsScroll, false, true);
        GUILayout.Label("Settings");
        GUILayout.Space(8f);

        GUILayout.Label("Aim Sensitivity");
        DrawSliderSetting("Sensitivity", TpsPlayerSettings.LookSensitivity, 0.1f, 10f, TpsPlayerSettings.SetLookSensitivity);
        DrawSliderSetting("ADS Sens", TpsPlayerSettings.AdsMultiplier, 0.1f, 2f, TpsPlayerSettings.SetAdsMultiplier);
        DrawSliderSetting("Vertical", TpsPlayerSettings.VerticalRatio, 0.1f, 2f, TpsPlayerSettings.SetVerticalRatio);

        GUILayout.Space(12f);
        GUILayout.Label("Reticle");
        GUILayout.BeginHorizontal();
        DrawStyleButton("Cross", ReticleStyle.Crosshair);
        DrawStyleButton("Dot", ReticleStyle.Dot);
        DrawStyleButton("Circle", ReticleStyle.Circle);
        DrawStyleButton("Circle+Dot", ReticleStyle.CircleDot);
        GUILayout.EndHorizontal();

        DrawSliderSetting("Size", TpsPlayerSettings.ReticleSize, 2f, 30f, TpsPlayerSettings.SetReticleSize);
        DrawSliderSetting("Gap", TpsPlayerSettings.ReticleGap, 0f, 30f, TpsPlayerSettings.SetReticleGap);
        DrawSliderSetting("Thickness", TpsPlayerSettings.ReticleThickness, 1f, 6f, TpsPlayerSettings.SetReticleThickness);
        bool outline = GUILayout.Toggle(TpsPlayerSettings.ReticleOutline, "Outline");
        if (outline != TpsPlayerSettings.ReticleOutline)
        {
            TpsPlayerSettings.SetReticleOutline(outline);
        }

        GUILayout.Space(6f);
        GUILayout.Label("Color");
        GUILayout.BeginHorizontal();
        DrawColorButton("Green", new Color(0.15f, 1f, 0.55f, 0.95f));
        DrawColorButton("Cyan", new Color(0.1f, 0.9f, 1f, 0.95f));
        DrawColorButton("Pink", new Color(1f, 0.25f, 0.85f, 0.95f));
        DrawColorButton("Yellow", new Color(1f, 0.9f, 0.15f, 0.95f));
        DrawColorButton("White", new Color(1f, 1f, 1f, 0.95f));
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.Label("Esc closes settings.");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Defaults", GUILayout.Height(30f)))
        {
            TpsPlayerSettings.ResetDefaults();
        }

        if (GUILayout.Button("Close", GUILayout.Height(30f)))
        {
            TpsPlayerSettings.SettingsOpen = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        DrawReticlePreview(panel);
        GUI.contentColor = oldContentColor;
    }

    private void DrawSliderSetting(string label, float value, float min, float max, Action<float> setter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.##}", GUILayout.Width(120f));
        float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(230f));
        GUILayout.EndHorizontal();
        if (!Mathf.Approximately(next, value))
        {
            setter(next);
        }
    }

    private void DrawStyleButton(string label, ReticleStyle style)
    {
        bool selected = TpsPlayerSettings.ReticleMode == style;
        Color oldColor = GUI.color;
        GUI.color = selected ? new Color(0.25f, 0.7f, 1f, 1f) : oldColor;
        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            TpsPlayerSettings.SetReticleStyle(style);
        }
        GUI.color = oldColor;
    }

    private void DrawColorButton(string label, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            TpsPlayerSettings.SetReticleColor(color);
        }
        GUI.color = oldColor;
    }

    private void DrawReticlePreview(Rect panel)
    {
        float cx = panel.x + panel.width - 88f;
        float cy = panel.y + 82f;
        Color oldColor = GUI.color;
        GUI.color = new Color(0.12f, 0.14f, 0.16f, 0.96f);
        GUI.Box(new Rect(cx - 52f, cy - 52f, 104f, 104f), GUIContent.none);
        GUI.color = oldColor;
        DrawReticleShape(cx, cy, TpsPlayerSettings.ReticleGap, TpsPlayerSettings.ReticleSize, TpsPlayerSettings.ReticleThickness, TpsPlayerSettings.ReticleColor);
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

        AppendMovementMetrics();
        AppendAnimationMetrics();
        AppendRecoilMetrics(weapon);

        debugBuilder.Append("Shots: ").Append(weaponController.TotalShotsFired)
            .Append(" | Registered: ").Append(weaponController.TotalRegisteredHits)
            .Append(" | Crits: ").Append(weaponController.TotalCriticalHits)
            .Append(" | World: ").Append(weaponController.TotalWorldHits)
            .Append(" | NoDmg: ").Append(weaponController.TotalNonDamageWorldHits)
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
            .Append(" | Stance x").Append(weaponController.StanceSpreadMultiplier.ToString("0.00"))
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

    private void AppendMovementMetrics()
    {
        if (motor == null)
        {
            return;
        }

        debugBuilder.Append("Move: ").Append(motor.GetMovementMode())
            .Append(" | Speed ").Append(motor.GetCurrentSpeed().ToString("0.00"))
            .Append('/').Append(motor.GetDesiredSpeed().ToString("0.00"))
            .Append(" | Accel ").Append(motor.GetCurrentAcceleration().ToString("0.0"))
            .Append(" | Y ").Append(motor.GetVerticalVelocity().y.ToString("0.00"))
            .Append(" | Ground ").Append(motor.IsGrounded() ? "yes" : "no")
            .AppendLine();

        debugBuilder.Append("Move timers: coyote ").Append(motor.GetCoyoteTimeRemaining().ToString("0.00"))
            .Append(" | jumpBuf ").Append(motor.GetJumpBufferTimeRemaining().ToString("0.00"))
            .Append(" | slideBuf ").Append(motor.GetSlideBufferTimeRemaining().ToString("0.00"))
            .Append(" | stable ").Append(motor.GetGroundedStableTime().ToString("0.00"))
            .Append(" | lock ").Append(motor.IsJumpLockedUntilGrounded() ? "yes" : "no")
            .Append(" | slideSpd ").Append(motor.GetSlideSpeed().ToString("0.00"))
            .Append(" | settle stick/jump/stand ").Append(motor.GetSlideExitStickTimeRemaining().ToString("0.00"))
            .Append('/').Append(motor.GetSlideExitJumpLockTimeRemaining().ToString("0.00"))
            .Append('/').Append(motor.GetSlideExitStandLockTimeRemaining().ToString("0.00"))
            .Append(" | leftGround ").Append(motor.HasLeftGroundSinceJump() ? "yes" : "no");

        string slideExit = motor.GetSlideExitReason();
        if (!string.IsNullOrEmpty(slideExit))
        {
            debugBuilder.Append(" | slideExit ").Append(slideExit);
        }

        string standBlocker = motor.GetStandBlocker();
        if (!string.IsNullOrEmpty(standBlocker))
        {
            debugBuilder.Append(" | stand blocked ").Append(standBlocker);
        }

        debugBuilder.AppendLine();
    }

    private void AppendRecoilMetrics(WeaponDefinition weapon)
    {
        debugBuilder.Append("Recoil: burst ").Append(weaponController.RecoilBurstShotIndex)
            .Append(" | kick P/Y ").Append(weaponController.LastRecoilPitchKick.ToString("0.00"))
            .Append('/').Append(weaponController.LastRecoilYawKick.ToString("0.00"))
            .Append(" | stance x").Append(weaponController.StanceRecoilMultiplier.ToString("0.00"));

        if (cameraController != null)
        {
            debugBuilder.Append(" | cam P/Y ").Append(cameraController.CurrentRecoilPitch.ToString("0.00"))
                .Append('/').Append(cameraController.CurrentRecoilYaw.ToString("0.00"));
        }

        debugBuilder.Append(" | reset ").Append(weapon.recoilResetDelay.ToString("0.00")).Append('s')
            .AppendLine();
    }

    private void AppendAnimationMetrics()
    {
        if (animationController == null)
        {
            return;
        }

        debugBuilder.Append("Anim: ").Append(animationController.CurrentMovementState)
            .Append(" | MX/MY ").Append(animationController.CurrentMovementX.ToString("0.00"))
            .Append('/').Append(animationController.CurrentMovementY.ToString("0.00"))
            .Append(" | CWalk ").Append(animationController.CurrentUsesCrouchWalkVisual ? "yes" : "no")
            .Append(" | State ").Append(string.IsNullOrEmpty(animationController.CurrentAnimatorStatePath) ? "-" : animationController.CurrentAnimatorStatePath)
            .AppendLine();

        debugBuilder.Append("Anim timers: crouchTrans ").Append(animationController.CurrentCrouchTransitionStateTime.ToString("0.00"))
            .Append(" | slideSettle ").Append(animationController.SlideExitCrouchSettleTime.ToString("0.00"))
            .Append(" | visualGround ").Append(animationController.CurrentVisualGroundOffset.ToString("0.000"))
            .Append(" | crouchStable ").Append(weaponController.IsCrouchStabilized ? "yes" : "no")
            .Append(" | aimStrafe ").Append(animationController.AimStrafeClipPromoted ? "live" : "blocked")
            .AppendLine();
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
