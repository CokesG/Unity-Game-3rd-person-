using System;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Camera controlledCamera;

    [Header("Follow Settings")]
    [SerializeField] private float distance = 3.75f;
    [SerializeField] private float aimDistance = 2.45f;
    [SerializeField] private float heightOffset = 0.05f;
    [SerializeField] private float shoulderOffset = 0.45f;
    [SerializeField] private float aimShoulderOffset = 0.65f;
    [SerializeField] private bool allowShoulderSwap = true;

    [Header("Look Settings")]
    [SerializeField] private float sensitivityX = 28f;
    [SerializeField] private float sensitivityY = 18f;
    [SerializeField] private float minPitch = -28f;
    [SerializeField] private float maxPitch = 55f;

    [Header("Field Of View")]
    [SerializeField] private float explorationFieldOfView = 85f;
    [SerializeField] private float aimFieldOfView = 76f;
    [SerializeField] private float sprintFieldOfView = 90f;
    [SerializeField] private float fovSmoothTime = 0.1f;

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothTime = 0.07f;

    [Header("Collision")]
    [SerializeField] private bool handleCollision = true;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionBuffer = 0.08f;

    [Header("Aim Ray")]
    [SerializeField] private float aimRayDistance = 250f;
    [SerializeField] private LayerMask aimRayLayers = ~0;
    [SerializeField] private Color aimRayGizmoColor = Color.cyan;

    [Header("Recoil")]
    [SerializeField] private float recoilReturnSpeed = 18f;
    [SerializeField] private float maxRecoilPitch = 8f;
    [SerializeField] private float maxRecoilYaw = 4f;

    [Header("Debug Readout")]
    [SerializeField] private Vector3 debugAimPoint;
    [SerializeField] private bool debugHasAimHit;
    [SerializeField] private bool debugMuzzleBlocked;
    [SerializeField] private string debugAimHitName;
    [SerializeField] private float debugShoulderSide = 1f;

    private PlayerInputHandler input;
    private ThirdPersonMotor motor;
    private float currentPitch;
    private float currentYaw;

    private Vector3 currentFollowVelocity;
    private Vector3 smoothTargetPos;

    private float currentDistance;
    private float currentShoulderOffset;
    private float currentFieldOfView;
    private float targetShoulderSide = 1f;
    private float distVelocity;
    private float shoulderVelocity;
    private float fovVelocity;
    private float recoilPitch;
    private float recoilYaw;
    private Ray currentAimRay;
    private RaycastHit currentAimHit;
    private bool hasAimHit;
    private bool muzzleBlocked;
    private Vector3 aimPoint;

    public Vector3 AimPoint => aimPoint;
    public Ray AimRay => currentAimRay;
    public bool HasAimHit => hasAimHit;
    public bool IsMuzzleBlocked => muzzleBlocked;
    public Camera ControlledCamera => controlledCamera;
    public float CurrentRecoilPitch => recoilPitch;
    public float CurrentRecoilYaw => recoilYaw;

    private void Start()
    {
        if (controlledCamera == null)
        {
            controlledCamera = GetComponent<Camera>();
        }

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Transform camTarget = player.transform.Find("CameraTarget");
                target = camTarget != null ? camTarget : player.transform;
            }
        }

        if (target != null)
        {
            input = target.GetComponentInParent<PlayerInputHandler>();
            motor = target.GetComponentInParent<ThirdPersonMotor>();
        }

        currentDistance = distance;
        currentShoulderOffset = shoulderOffset;
        currentFieldOfView = controlledCamera != null ? controlledCamera.fieldOfView : explorationFieldOfView;
        smoothTargetPos = target != null ? target.position : transform.position;

        Vector3 rotation = transform.eulerAngles;
        currentYaw = rotation.y;
        currentPitch = rotation.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null || input == null) return;

        HandleInput(Time.deltaTime);
        CalculatePositionAndRotation(Time.deltaTime);
        UpdateAimRay();
        UpdateDebugReadout();
    }

    private void HandleInput(float deltaTime)
    {
        Vector2 lookInput = input.LookInput;

        currentYaw += lookInput.x * sensitivityX * deltaTime;
        currentPitch -= lookInput.y * sensitivityY * deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        if (allowShoulderSwap && input.ShoulderSwapTriggered)
        {
            targetShoulderSide *= -1f;
            input.ConsumeShoulderSwap();
        }

        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recoilReturnSpeed * deltaTime);
        recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, recoilReturnSpeed * deltaTime);
    }

    private void CalculatePositionAndRotation(float deltaTime)
    {
        bool isAiming = input.AimPressed;
        bool isSprinting = motor != null && motor.IsSprinting();

        float targetDist = isAiming ? aimDistance : distance;
        float targetShoulder = (isAiming ? aimShoulderOffset : shoulderOffset) * targetShoulderSide;
        float targetFov = isAiming ? aimFieldOfView : isSprinting ? sprintFieldOfView : explorationFieldOfView;

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distVelocity, 0.1f);
        currentShoulderOffset = Mathf.SmoothDamp(currentShoulderOffset, targetShoulder, ref shoulderVelocity, 0.1f);
        currentFieldOfView = Mathf.SmoothDamp(currentFieldOfView, targetFov, ref fovVelocity, fovSmoothTime);

        if (controlledCamera != null)
        {
            controlledCamera.fieldOfView = currentFieldOfView;
        }

        Quaternion targetRotation = Quaternion.Euler(
            Mathf.Clamp(currentPitch + recoilPitch, minPitch, maxPitch),
            currentYaw + recoilYaw,
            0f);

        float rotationT = rotationSmoothTime <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / rotationSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);

        smoothTargetPos = Vector3.SmoothDamp(smoothTargetPos, target.position, ref currentFollowVelocity, followSmoothTime);

        Vector3 pivotPos = smoothTargetPos + transform.up * heightOffset;
        Vector3 backDir = -transform.forward;
        Vector3 rightDir = transform.right;
        Vector3 desiredPos = pivotPos + (backDir * currentDistance) + (rightDir * currentShoulderOffset);

        if (handleCollision)
        {
            Vector3 castVector = desiredPos - pivotPos;
            Vector3 castDir = castVector.normalized;
            float castDist = castVector.magnitude;

            if (castDist > 0.001f && Physics.SphereCast(pivotPos, collisionRadius, castDir, out RaycastHit hit, castDist, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivotPos + castDir * Mathf.Max(0f, hit.distance - collisionBuffer);
            }
        }

        transform.position = desiredPos;
    }

    private void UpdateAimRay()
    {
        Camera rayCamera = controlledCamera != null ? controlledCamera : Camera.main;
        if (rayCamera == null)
        {
            return;
        }

        currentAimRay = rayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        hasAimHit = TryGetFirstValidAimHit(currentAimRay, out currentAimHit);
        aimPoint = hasAimHit ? currentAimHit.point : currentAimRay.GetPoint(aimRayDistance);
    }

    private bool TryGetFirstValidAimHit(Ray ray, out RaycastHit selectedHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, aimRayDistance, aimRayLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (input != null && hit.transform.IsChildOf(input.transform))
            {
                continue;
            }

            selectedHit = hit;
            return true;
        }

        selectedHit = default(RaycastHit);
        return false;
    }

    private void UpdateDebugReadout()
    {
        debugAimPoint = aimPoint;
        debugHasAimHit = hasAimHit;
        debugMuzzleBlocked = muzzleBlocked;
        debugAimHitName = hasAimHit && currentAimHit.collider != null ? currentAimHit.collider.name : string.Empty;
        debugShoulderSide = targetShoulderSide;
    }

    public void AddRecoil(float pitchKick, float yawKick)
    {
        recoilPitch = Mathf.Clamp(recoilPitch - pitchKick, -maxRecoilPitch, maxRecoilPitch);
        recoilYaw = Mathf.Clamp(recoilYaw + yawKick, -maxRecoilYaw, maxRecoilYaw);
    }

    public void SetMuzzleBlocked(bool blocked)
    {
        muzzleBlocked = blocked;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = aimRayGizmoColor;
        Gizmos.DrawLine(currentAimRay.origin, aimPoint == Vector3.zero ? currentAimRay.GetPoint(aimRayDistance) : aimPoint);
        Gizmos.DrawSphere(aimPoint, 0.05f);
    }
}
