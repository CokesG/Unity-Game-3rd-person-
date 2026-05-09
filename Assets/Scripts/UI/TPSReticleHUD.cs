using UnityEngine;

public class TPSReticleHUD : MonoBehaviour
{
    private static TPSReticleHUD instance;

    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private ThirdPersonCameraController cameraController;
    [SerializeField] private ThirdPersonMotor motor;
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private float spreadPixelMultiplier = 14f;
    [SerializeField] private float minCrosshairGap = 8f;
    [SerializeField] private float maxCrosshairGap = 56f;

    private float hitmarkerTimer;
    private float hitDamage;
    private bool hitConfirmed;
    private bool criticalHit;

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
    }

    private void OnGUI()
    {
        DrawCrosshair();
        DrawHitmarker();
        DrawWeaponReadout();
    }

    private void DrawCrosshair()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float spread = weaponController != null ? weaponController.CurrentSpreadDegrees : 0.5f;
        float gap = Mathf.Clamp(minCrosshairGap + spread * spreadPixelMultiplier, minCrosshairGap, maxCrosshairGap);
        float length = 9f;
        float thickness = 2f;
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
