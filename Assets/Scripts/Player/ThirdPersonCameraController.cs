using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 3.75f;
    [SerializeField] private float aimDistance = 2.45f;
    [SerializeField] private float heightOffset = 0.05f;
    [SerializeField] private float shoulderOffset = 0.45f;
    [SerializeField] private float aimShoulderOffset = 0.65f;

    [Header("Look Settings")]
    [SerializeField] private float sensitivityX = 28f;
    [SerializeField] private float sensitivityY = 18f;
    [SerializeField] private float minPitch = -28f;
    [SerializeField] private float maxPitch = 55f;
    
    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothTime = 0.07f;

    [Header("Collision")]
    [SerializeField] private bool handleCollision = true;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionRadius = 0.25f;

    private PlayerInputHandler input;
    private float currentPitch;
    private float currentYaw;
    
    private Vector3 currentFollowVelocity;
    private Vector3 smoothTargetPos;
    
    private float currentDistance;
    private float currentShoulderOffset;
    private float distVelocity;
    private float shoulderVelocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Try to find CameraTarget child
                Transform camTarget = player.transform.Find("CameraTarget");
                target = camTarget != null ? camTarget : player.transform;
            }
        }

        if (target != null)
        {
            // The Player object usually has the input handler
            input = target.GetComponentInParent<PlayerInputHandler>();
        }

        currentDistance = distance;
        currentShoulderOffset = shoulderOffset;
        smoothTargetPos = target != null ? target.position : transform.position;
        
        // Initialize yaw/pitch from current rotation
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

        HandleInput();
        CalculatePositionAndRotation();
    }

    private void HandleInput()
    {
        Vector2 lookInput = input.LookInput;
        
        currentYaw += lookInput.x * sensitivityX * Time.deltaTime;
        currentPitch -= lookInput.y * sensitivityY * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
    }

    private void CalculatePositionAndRotation()
    {
        // 1. Smoothly transition parameters for Aim mode
        float targetDist = input.AimPressed ? aimDistance : distance;
        float targetShoulder = input.AimPressed ? aimShoulderOffset : shoulderOffset;
        
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distVelocity, 0.1f);
        currentShoulderOffset = Mathf.SmoothDamp(currentShoulderOffset, targetShoulder, ref shoulderVelocity, 0.1f);

        // 2. Calculate target rotation
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f / rotationSmoothTime * Time.deltaTime);

        // 3. Calculate camera position relative to target
        // We follow the target position smoothly
        smoothTargetPos = Vector3.SmoothDamp(smoothTargetPos, target.position, ref currentFollowVelocity, followSmoothTime);
        
        // Pivot point is target + height offset
        Vector3 pivotPos = smoothTargetPos + transform.up * heightOffset;
        
        // Backwards direction from current rotation
        Vector3 backDir = -transform.forward;
        Vector3 rightDir = transform.right;

        // Final desired position before collision
        Vector3 desiredPos = pivotPos + (backDir * currentDistance) + (rightDir * currentShoulderOffset);

        // 4. Handle Collision
        if (handleCollision)
        {
            // Check from pivot to desired position
            Vector3 castDir = (desiredPos - pivotPos).normalized;
            float castDist = Vector3.Distance(pivotPos, desiredPos);
            
            RaycastHit hit;
            if (Physics.SphereCast(pivotPos, collisionRadius, castDir, out hit, castDist, collisionLayers))
            {
                // Move camera forward if hit
                desiredPos = pivotPos + castDir * (hit.distance);
            }
        }

        transform.position = desiredPos;
    }
}
