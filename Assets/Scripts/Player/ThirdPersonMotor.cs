using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class ThirdPersonMotor : MonoBehaviour
{
    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float sprintSpeed = 7.25f;
    [SerializeField] private float aimSpeed = 3.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float postJumpGroundLockTime = 0.18f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private Vector3 groundCheckOffset = Vector3.zero;
    [SerializeField] private LayerMask groundMask;

    [Header("Crouch / Slide")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchCameraTargetY = 0.15f;
    [SerializeField] private float crouchTransitionSpeed = 12f;
    [SerializeField] private float crouchStepOffset = 0.15f;
    [SerializeField] private float slideStartSpeed = 8.5f;
    [SerializeField] private float slideEndSpeed = 3.0f;
    [SerializeField] private float slideDuration = 0.9f;
    [SerializeField] private float slideSteerStrength = 5f;

    [Header("Debug Readout")]
    [SerializeField] private string currentMovementMode;
    [SerializeField] private bool debugIsCrouching;
    [SerializeField] private bool debugIsSliding;
    [SerializeField] private float debugCurrentSpeed;
    [SerializeField] private float debugCapsuleHeight;
    [SerializeField] private Vector3 debugCameraTargetLocalPosition;
    [SerializeField] private string debugStandBlocker;

    private CharacterController controller;
    private PlayerInputHandler input;
    private Transform mainCamera;

    private Vector3 verticalVelocity;
    private Vector3 horizontalVelocity;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isSprinting;
    private bool jumpedThisFrame;
    private bool landedThisFrame;
    private bool hasJumpedSinceGrounded;
    private bool wantsToCrouch;
    private bool isSliding;
    private float lastJumpTime = -999f;
    private float standingHeight;
    private float standingStepOffset;
    private Vector3 standingCenter;
    private Vector3 cameraTargetStandingLocalPosition;
    private Vector3 slideDirection;
    private float slideSpeed;
    private float slideTimer;
    private float currentSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        standingStepOffset = controller.stepOffset;

        if (cameraTarget == null)
        {
            cameraTarget = transform.Find("CameraTarget");
        }

        if (cameraTarget != null)
        {
            cameraTargetStandingLocalPosition = cameraTarget.localPosition;
        }

        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        jumpedThisFrame = false;
        landedThisFrame = false;

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        CheckGrounded();
        HandleCrouchAndSlideInput();
        UpdateCrouchShape(Time.deltaTime);
        HandleMovement();
        HandleRotation();
        HandleGravityAndJump();
        UpdateDebugReadout();
    }

    private void CheckGrounded()
    {
        Vector3 capsuleFoot = transform.position + controller.center + Vector3.down * (controller.height * 0.5f);
        Vector3 spherePos = capsuleFoot + Vector3.up * Mathf.Max(groundCheckRadius, 0.02f) + groundCheckOffset;
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;
        bool sphereHit = Physics.CheckSphere(spherePos, groundCheckRadius, mask, QueryTriggerInteraction.Ignore);
        bool ignoreGroundAfterJump = hasJumpedSinceGrounded
            && (verticalVelocity.y > 0f || Time.time - lastJumpTime < postJumpGroundLockTime);
        
        wasGrounded = isGrounded;
        isGrounded = !ignoreGroundAfterJump && (controller.isGrounded || sphereHit);
        landedThisFrame = !wasGrounded && isGrounded;

        if (landedThisFrame)
        {
            hasJumpedSinceGrounded = false;
        }
        
        // Reset vertical velocity if grounded
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
    }

    private void HandleCrouchAndSlideInput()
    {
        if (input.CrouchTriggered)
        {
            if (CanStartSlide())
            {
                StartSlide();
            }
            else if (!isSliding)
            {
                ToggleCrouch();
            }

            input.ConsumeCrouch();
        }

        if (!isSliding) return;

        slideTimer -= Time.deltaTime;
        if (!isGrounded || input.AimPressed || slideTimer <= 0f || slideSpeed <= slideEndSpeed + 0.05f)
        {
            EndSlide(true);
        }
    }

    private void HandleMovement()
    {
        if (isSliding)
        {
            HandleSlideMovement();
            return;
        }

        float inputMag = Mathf.Clamp01(input.MoveInput.magnitude);
        isSprinting = !input.AimPressed && !wantsToCrouch && input.SprintPressed && input.MoveInput.y > 0.1f && inputMag > 0.01f;
        
        if (input.AimPressed)
        {
            currentSpeed = aimSpeed;
        }
        else if (wantsToCrouch)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = Mathf.Lerp(walkSpeed, runSpeed, inputMag);
        }

        if (inputMag < 0.01f) currentSpeed = 0;

        Vector3 moveInput = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
        
        if (mainCamera == null)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        Vector3 camForward = mainCamera.forward;
        camForward.y = 0;
        camForward.Normalize();
        
        Vector3 camRight = mainCamera.right;
        camRight.y = 0;
        camRight.Normalize();

        moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
        horizontalVelocity = moveDirection * currentSpeed;

        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private void HandleSlideMovement()
    {
        isSprinting = false;
        wantsToCrouch = true;

        Vector3 steerInput = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
        steerInput = Vector3.ClampMagnitude(steerInput, 1f);
        if (mainCamera != null && steerInput.sqrMagnitude > 0.05f)
        {
            Vector3 camForward = mainCamera.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = mainCamera.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 desiredDirection = (camForward * steerInput.z + camRight * steerInput.x).normalized;
            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                slideDirection = Vector3.Slerp(slideDirection, desiredDirection, slideSteerStrength * Time.deltaTime).normalized;
            }
        }

        currentSpeed = slideSpeed;
        horizontalVelocity = slideDirection * slideSpeed;
        moveDirection = slideDirection;
        controller.Move(horizontalVelocity * Time.deltaTime);

        slideSpeed = Mathf.MoveTowards(slideSpeed, slideEndSpeed, (slideStartSpeed - slideEndSpeed) / Mathf.Max(slideDuration, 0.01f) * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (mainCamera == null) return;

        if (isSliding && slideDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(slideDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (input.AimPressed)
        {
            Vector3 camForward = mainCamera.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else if (input.MoveInput.sqrMagnitude > 0.001f)
        {
            Vector3 camForward = mainCamera.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = mainCamera.right;
            camRight.y = 0;
            camRight.Normalize();
            
            Vector3 moveDirection = (camForward * input.MoveInput.y + camRight * input.MoveInput.x).normalized;
            
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleGravityAndJump()
    {
        if (input.JumpTriggered)
        {
            if (CanJump())
            {
                if (isSliding)
                {
                    EndSlide(false);
                }
                else if (wantsToCrouch)
                {
                    TryStand();
                }

                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpedThisFrame = true;
                hasJumpedSinceGrounded = true;
                isGrounded = false;
                lastJumpTime = Time.time;
            }

            input.ConsumeJump();
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private bool CanJump()
    {
        return isGrounded && !hasJumpedSinceGrounded;
    }

    private bool CanStartSlide()
    {
        return isGrounded
            && !isSliding
            && !input.AimPressed
            && isSprinting
            && input.MoveInput.y > 0.4f
            && horizontalVelocity.magnitude > runSpeed * 0.8f;
    }

    private void StartSlide()
    {
        isSliding = true;
        wantsToCrouch = true;
        slideTimer = slideDuration;
        slideSpeed = Mathf.Max(slideStartSpeed, horizontalVelocity.magnitude);
        slideDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : transform.forward;
    }

    private void EndSlide(bool enterCrouch)
    {
        isSliding = false;
        slideTimer = 0f;
        slideSpeed = 0f;
        wantsToCrouch = enterCrouch;
    }

    private void ToggleCrouch()
    {
        if (wantsToCrouch)
        {
            TryStand();
        }
        else
        {
            wantsToCrouch = true;
        }
    }

    private void TryStand()
    {
        if (CanStand())
        {
            wantsToCrouch = false;
        }
    }

    private bool CanStand()
    {
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;
        float radius = Mathf.Max(controller.radius * 0.95f, 0.05f);
        Vector3 worldCenter = transform.position + standingCenter;
        Vector3 bottom = worldCenter + Vector3.down * (standingHeight * 0.5f - radius) + Vector3.up * 0.05f;
        Vector3 top = worldCenter + Vector3.up * (standingHeight * 0.5f - radius);
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            debugStandBlocker = hit.name;
            return false;
        }

        debugStandBlocker = string.Empty;
        return true;
    }

    private void UpdateCrouchShape(float deltaTime)
    {
        bool useCrouchShape = wantsToCrouch || isSliding;
        float targetHeight = useCrouchShape ? crouchHeight : standingHeight;
        Vector3 targetCenter = standingCenter + Vector3.up * ((targetHeight - standingHeight) * 0.5f);
        float targetStepOffset = useCrouchShape ? crouchStepOffset : standingStepOffset;
        float t = 1f - Mathf.Exp(-crouchTransitionSpeed * deltaTime);

        controller.height = Mathf.Lerp(controller.height, targetHeight, t);
        controller.center = Vector3.Lerp(controller.center, targetCenter, t);
        controller.stepOffset = Mathf.Lerp(controller.stepOffset, targetStepOffset, t);

        if (cameraTarget == null) return;

        Vector3 targetCameraPosition = cameraTargetStandingLocalPosition;
        if (useCrouchShape)
        {
            targetCameraPosition.y = crouchCameraTargetY;
        }

        cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, targetCameraPosition, t);
    }

    private void UpdateDebugReadout()
    {
        debugIsCrouching = IsCrouching();
        debugIsSliding = isSliding;
        debugCurrentSpeed = currentSpeed;
        debugCapsuleHeight = controller.height;
        debugCameraTargetLocalPosition = cameraTarget != null ? cameraTarget.localPosition : Vector3.zero;
        if (!wantsToCrouch)
        {
            debugStandBlocker = string.Empty;
        }

        if (isSliding) currentMovementMode = "Slide";
        else if (wantsToCrouch) currentMovementMode = currentSpeed > 0.1f ? "Crouch Move" : "Crouch Idle";
        else if (input.AimPressed) currentMovementMode = currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        else if (!isGrounded) currentMovementMode = verticalVelocity.y > 0f ? "Jumping" : "Falling";
        else if (isSprinting) currentMovementMode = "Sprint";
        else if (currentSpeed > walkSpeed + 0.1f) currentMovementMode = "Run";
        else if (currentSpeed > 0.1f) currentMovementMode = "Walk";
        else currentMovementMode = "Idle";
    }

    public bool IsGrounded() => isGrounded;
    public bool IsSprinting() => isSprinting;
    public bool IsAiming() => input != null && input.AimPressed;
    public bool IsCrouching() => wantsToCrouch || isSliding;
    public bool IsSliding() => isSliding;
    public bool JumpedThisFrame() => jumpedThisFrame;
    public bool LandedThisFrame() => landedThisFrame;
    public bool IsJumping() => !isGrounded && verticalVelocity.y > 0.1f;
    public bool IsFalling() => !isGrounded && verticalVelocity.y < -0.1f;
    public float GetCurrentSpeed() => currentSpeed;
    public Vector3 GetHorizontalVelocity() => horizontalVelocity;
    public Vector3 GetMoveDirection() => moveDirection;
    public Vector3 GetVerticalVelocity() => verticalVelocity;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
    }
}
