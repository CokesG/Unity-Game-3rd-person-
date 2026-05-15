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
    [SerializeField] private float crouchSpeed = 2.4f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Feel")]
    [SerializeField] private float normalAcceleration = 22f;
    [SerializeField] private float sprintAcceleration = 34f;
    [SerializeField] private float aimAcceleration = 28f;
    [SerializeField] private float groundDeceleration = 46f;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private float airDeceleration = 4f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.55f;
    [SerializeField] private float normalRotationSpeed = 15f;
    [SerializeField] private float aimRotationSpeed = 24f;
    [SerializeField] private float sprintRotationSpeed = 12f;
    [SerializeField] private float slideRotationSpeed = 10f;

    [Header("Physics")]
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float jumpHeight = 0.5625f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = -30f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float minJumpInterval = 0.25f;
    [SerializeField] private float postJumpGroundLockTime = 0.18f;
    [SerializeField] private float groundedJumpResetTime = 0.08f;
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
    [SerializeField] private float slideInputBufferTime = 0.1f;
    [SerializeField] private float slideGroundStickForce = 8f;
    [SerializeField] private float slideExitSpeedCarry = 1.1f;
    [SerializeField] private float slideStartGroundedStableTime = 0.06f;
    [SerializeField] private float slideCrouchExitGroundStickTime = 0.18f;
    [SerializeField] private float slideMaxUpwardExitVelocity = 0f;

    [Header("Debug Readout")]
    [SerializeField] private string currentMovementMode;
    [SerializeField] private bool debugIsCrouching;
    [SerializeField] private bool debugIsSliding;
    [SerializeField] private bool debugIsSlowWalking;
    [SerializeField] private float debugCurrentSpeed;
    [SerializeField] private float debugDesiredSpeed;
    [SerializeField] private float debugCurrentAcceleration;
    [SerializeField] private float debugCoyoteTimer;
    [SerializeField] private float debugJumpBufferTimer;
    [SerializeField] private float debugSlideBufferTimer;
    [SerializeField] private float debugGroundedStableTimer;
    [SerializeField] private bool debugControllerGrounded;
    [SerializeField] private bool debugGroundSphereHit;
    [SerializeField] private bool debugRawGrounded;
    [SerializeField] private bool debugJumpLockedUntilGrounded;
    [SerializeField] private bool debugHasLeftGroundSinceJump;
    [SerializeField] private Vector3 debugHorizontalVelocity;
    [SerializeField] private float debugCapsuleHeight;
    [SerializeField] private Vector3 debugCameraTargetLocalPosition;
    [SerializeField] private string debugStandBlocker;
    [SerializeField] private string debugSlideExitReason;
    [SerializeField] private float debugSlideExitStickTimer;

    private CharacterController controller;
    private PlayerInputHandler input;
    private Transform mainCamera;

    private Vector3 verticalVelocity;
    private Vector3 horizontalVelocity;
    private Vector3 moveDirection;
    private Vector3 desiredMoveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isSprinting;
    private bool isSlowWalking;
    private bool jumpedThisFrame;
    private bool landedThisFrame;
    private bool hasJumpedSinceGrounded;
    private bool jumpLockedUntilGrounded;
    private bool hasLeftGroundSinceJump;
    private bool wantsToCrouch;
    private bool isSliding;
    private bool pendingCrouchToggle;
    private float lastJumpTime = -999f;
    private float standingHeight;
    private float standingStepOffset;
    private Vector3 standingCenter;
    private Vector3 cameraTargetStandingLocalPosition;
    private Vector3 slideDirection;
    private float slideSpeed;
    private float slideTimer;
    private float currentSpeed;
    private float desiredSpeed;
    private float currentAcceleration;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float slideBufferTimer;
    private float groundedStableTimer;
    private float slideExitStickTimer;
    private readonly Collider[] groundOverlapHits = new Collider[8];

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

        CacheMainCamera();
    }

    private void Update()
    {
        jumpedThisFrame = false;
        landedThisFrame = false;

        CacheMainCamera();
        CheckGrounded();
        TrackBufferedInputs(Time.deltaTime);
        HandleCrouchAndSlideInput();
        UpdateCrouchShape(Time.deltaTime);
        HandleMovement(Time.deltaTime);
        HandleRotation(Time.deltaTime);
        HandleGravityAndJump(Time.deltaTime);
        UpdateDebugReadout();
    }

    private void CacheMainCamera()
    {
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    private void CheckGrounded()
    {
        Vector3 capsuleFoot = transform.position + controller.center + Vector3.down * (controller.height * 0.5f);
        Vector3 spherePos = capsuleFoot + Vector3.up * Mathf.Max(groundCheckRadius, 0.02f) + groundCheckOffset;
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;
        bool sphereHit = CheckGroundSphere(spherePos, groundCheckRadius, mask);
        bool rawGrounded = controller.isGrounded || sphereHit;
        debugControllerGrounded = controller.isGrounded;
        debugGroundSphereHit = sphereHit;
        debugRawGrounded = rawGrounded;

        if (jumpLockedUntilGrounded && !rawGrounded)
        {
            hasLeftGroundSinceJump = true;
        }

        bool ignoreGroundAfterJump = jumpLockedUntilGrounded
            && (verticalVelocity.y > 0f || Time.time - lastJumpTime < postJumpGroundLockTime);

        wasGrounded = isGrounded;
        isGrounded = !ignoreGroundAfterJump && rawGrounded;
        landedThisFrame = !wasGrounded && isGrounded && verticalVelocity.y <= 0f;

        if (isGrounded && verticalVelocity.y <= 0.05f)
        {
            groundedStableTimer += Time.deltaTime;
        }
        else
        {
            groundedStableTimer = 0f;
        }

        if (jumpLockedUntilGrounded && isGrounded && verticalVelocity.y <= 0.05f && groundedStableTimer >= groundedJumpResetTime)
        {
            jumpLockedUntilGrounded = false;
            hasJumpedSinceGrounded = false;
            hasLeftGroundSinceJump = false;
        }
        else if (!jumpLockedUntilGrounded && isGrounded)
        {
            hasJumpedSinceGrounded = false;
            hasLeftGroundSinceJump = false;
        }

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
    }

    private bool CheckGroundSphere(Vector3 spherePosition, float radius, int mask)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            spherePosition,
            radius,
            groundOverlapHits,
            mask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = groundOverlapHits[i];
            if (hit == null)
            {
                continue;
            }

            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void TrackBufferedInputs(float deltaTime)
    {
        if (isGrounded && !jumpLockedUntilGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer = Mathf.Max(0f, coyoteTimer - deltaTime);
        }

        if (input.JumpTriggered)
        {
            jumpBufferTimer = jumpLockedUntilGrounded || hasJumpedSinceGrounded ? 0f : jumpBufferTime;
            input.ConsumeJump();
        }
        else
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);
        }

        if (input.CrouchTriggered)
        {
            slideBufferTimer = slideInputBufferTime;
            pendingCrouchToggle = true;
            input.ConsumeCrouch();
        }
        else
        {
            slideBufferTimer = Mathf.Max(0f, slideBufferTimer - deltaTime);
        }
    }

    private void HandleCrouchAndSlideInput()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (!isGrounded)
            {
                EndSlide(true, "left ground");
            }
            else if (input.AimPressed)
            {
                EndSlide(true, "aim cancel");
            }
            else if (slideTimer <= 0f)
            {
                EndSlide(true, "timer");
            }
            else if (slideSpeed <= slideEndSpeed + 0.05f)
            {
                EndSlide(true, "speed");
            }
        }

        if (slideBufferTimer > 0f && CanStartSlide())
        {
            StartSlide();
            pendingCrouchToggle = false;
            slideBufferTimer = 0f;
            return;
        }

        if (!pendingCrouchToggle || isSliding)
        {
            return;
        }

        bool shouldWaitForSlide = input.SprintPressed && !wantsToCrouch && slideBufferTimer > 0f;
        if (shouldWaitForSlide)
        {
            return;
        }

        ToggleCrouch();
        pendingCrouchToggle = false;
        slideBufferTimer = 0f;
    }

    private void HandleMovement(float deltaTime)
    {
        if (isSliding)
        {
            HandleSlideMovement(deltaTime);
            return;
        }

        Vector3 targetVelocity = BuildTargetHorizontalVelocity();
        float acceleration = ResolveHorizontalAcceleration(targetVelocity);
        currentAcceleration = acceleration;

        if (!isGrounded && targetVelocity.sqrMagnitude > 0.001f)
        {
            targetVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, airControl);
        }

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);
        currentSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;

        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            controller.Move(horizontalVelocity * deltaTime);
        }
    }

    private Vector3 BuildTargetHorizontalVelocity()
    {
        float inputMagnitude = Mathf.Clamp01(input.MoveInput.magnitude);
        isSlowWalking = CanSlowWalk(inputMagnitude);
        isSprinting = CanSprint(inputMagnitude);
        desiredSpeed = ResolveDesiredSpeed(inputMagnitude);

        if (mainCamera == null || inputMagnitude < 0.01f)
        {
            desiredMoveDirection = Vector3.zero;
            return Vector3.zero;
        }

        Vector3 moveInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);

        Vector3 camForward = mainCamera.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = mainCamera.right;
        camRight.y = 0f;
        camRight.Normalize();

        desiredMoveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
        if (desiredMoveDirection.sqrMagnitude > 0.001f)
        {
            moveDirection = desiredMoveDirection;
        }

        return desiredMoveDirection * desiredSpeed;
    }

    private float ResolveDesiredSpeed(float inputMagnitude)
    {
        if (inputMagnitude < 0.01f) return 0f;
        if (input.AimPressed) return aimSpeed;
        if (wantsToCrouch) return crouchSpeed;
        if (isSprinting) return sprintSpeed;
        if (isSlowWalking) return walkSpeed * inputMagnitude;

        return Mathf.Lerp(walkSpeed, runSpeed, inputMagnitude);
    }

    private float ResolveHorizontalAcceleration(Vector3 targetVelocity)
    {
        bool hasTargetVelocity = targetVelocity.sqrMagnitude > 0.001f;

        if (!isGrounded)
        {
            return hasTargetVelocity ? airAcceleration : airDeceleration;
        }

        if (!hasTargetVelocity || targetVelocity.magnitude < horizontalVelocity.magnitude)
        {
            return groundDeceleration;
        }

        if (input.AimPressed) return aimAcceleration;
        if (isSprinting) return sprintAcceleration;

        return normalAcceleration;
    }

    private void HandleSlideMovement(float deltaTime)
    {
        isSprinting = false;
        wantsToCrouch = true;
        if (verticalVelocity.y > -2f)
        {
            verticalVelocity.y = -2f;
        }

        Vector3 steerInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        steerInput = Vector3.ClampMagnitude(steerInput, 1f);
        if (mainCamera != null && steerInput.sqrMagnitude > 0.05f)
        {
            Vector3 camForward = mainCamera.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = mainCamera.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 desiredDirection = (camForward * steerInput.z + camRight * steerInput.x).normalized;
            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                slideDirection = Vector3.Slerp(slideDirection, desiredDirection, slideSteerStrength * deltaTime).normalized;
            }
        }

        currentSpeed = slideSpeed;
        desiredSpeed = slideSpeed;
        currentAcceleration = (slideStartSpeed - slideEndSpeed) / Mathf.Max(slideDuration, 0.01f);
        horizontalVelocity = slideDirection * slideSpeed;
        moveDirection = slideDirection;
        controller.Move(horizontalVelocity * deltaTime);
        controller.Move(Vector3.down * (slideGroundStickForce * deltaTime));

        slideSpeed = Mathf.MoveTowards(slideSpeed, slideEndSpeed, (slideStartSpeed - slideEndSpeed) / Mathf.Max(slideDuration, 0.01f) * deltaTime);
    }

    private void HandleRotation(float deltaTime)
    {
        if (mainCamera == null) return;

        Vector3 facingDirection = Vector3.zero;
        float turnSpeed = ResolveRotationSpeed();

        if (isSliding && slideDirection.sqrMagnitude > 0.001f)
        {
            facingDirection = slideDirection;
        }
        else if (input.AimPressed)
        {
            facingDirection = mainCamera.forward;
            facingDirection.y = 0f;
        }
        else if (desiredMoveDirection.sqrMagnitude > 0.001f)
        {
            facingDirection = desiredMoveDirection;
        }

        if (facingDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
    }

    private float ResolveRotationSpeed()
    {
        if (isSliding) return slideRotationSpeed > 0f ? slideRotationSpeed : rotationSpeed;
        if (input.AimPressed) return aimRotationSpeed > 0f ? aimRotationSpeed : rotationSpeed;
        if (isSprinting) return sprintRotationSpeed > 0f ? sprintRotationSpeed : rotationSpeed;
        return normalRotationSpeed > 0f ? normalRotationSpeed : rotationSpeed;
    }

    private void HandleGravityAndJump(float deltaTime)
    {
        TryConsumeBufferedJump();

        float gravityMultiplier = verticalVelocity.y < 0f ? fallGravityMultiplier : 1f;
        verticalVelocity.y += gravity * gravityMultiplier * deltaTime;
        verticalVelocity.y = Mathf.Max(verticalVelocity.y, maxFallSpeed);
        controller.Move(verticalVelocity * deltaTime);

        if (slideExitStickTimer > 0f && wantsToCrouch && !isSliding)
        {
            controller.Move(Vector3.down * (slideGroundStickForce * deltaTime));
            slideExitStickTimer = Mathf.Max(0f, slideExitStickTimer - deltaTime);
        }
    }

    private void TryConsumeBufferedJump()
    {
        if (jumpBufferTimer <= 0f || !CanJump())
        {
            return;
        }

        if (isSliding)
        {
            EndSlide(false, "jump");
        }
        else if (wantsToCrouch)
        {
            if (!CanStand())
            {
                return;
            }

            wantsToCrouch = false;
        }

        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpedThisFrame = true;
        hasJumpedSinceGrounded = true;
        jumpLockedUntilGrounded = true;
        hasLeftGroundSinceJump = false;
        isGrounded = false;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        groundedStableTimer = 0f;
        lastJumpTime = Time.time;
    }

    private bool CanJump()
    {
        return (isGrounded || coyoteTimer > 0f)
            && !hasJumpedSinceGrounded
            && !jumpLockedUntilGrounded
            && verticalVelocity.y <= 0.05f
            && Time.time - lastJumpTime >= minJumpInterval;
    }

    private bool CanSprint(float inputMagnitude)
    {
        return !input.AimPressed
            && !wantsToCrouch
            && !input.SlowWalkPressed
            && input.SprintPressed
            && input.MoveInput.y > 0.1f
            && inputMagnitude > 0.01f;
    }

    private bool CanSlowWalk(float inputMagnitude)
    {
        return !input.AimPressed
            && !wantsToCrouch
            && input.SlowWalkPressed
            && inputMagnitude > 0.01f;
    }

    private bool CanStartSlide()
    {
        float inputMagnitude = Mathf.Clamp01(input.MoveInput.magnitude);
        bool sprintIntent = !input.AimPressed
            && !wantsToCrouch
            && !input.SlowWalkPressed
            && input.SprintPressed
            && input.MoveInput.y > 0.4f
            && inputMagnitude > 0.01f;

        return isGrounded
            && !isSliding
            && sprintIntent
            && groundedStableTimer >= slideStartGroundedStableTime
            && horizontalVelocity.magnitude > runSpeed * 0.75f;
    }

    private void StartSlide()
    {
        isSliding = true;
        wantsToCrouch = true;
        slideTimer = slideDuration;
        slideSpeed = Mathf.Max(slideStartSpeed, horizontalVelocity.magnitude);
        slideDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : transform.forward;
        slideExitStickTimer = 0f;
        debugSlideExitReason = string.Empty;
    }

    private void EndSlide(bool enterCrouch, string reason)
    {
        isSliding = false;
        slideTimer = 0f;
        debugSlideExitReason = reason;
        if (enterCrouch)
        {
            float carrySpeed = Mathf.Min(horizontalVelocity.magnitude, Mathf.Max(crouchSpeed * slideExitSpeedCarry, crouchSpeed));
            horizontalVelocity = slideDirection.sqrMagnitude > 0.001f ? slideDirection.normalized * carrySpeed : Vector3.zero;
            verticalVelocity.y = Mathf.Min(verticalVelocity.y, slideMaxUpwardExitVelocity);
            if (verticalVelocity.y > -2f) verticalVelocity.y = -2f;
            slideExitStickTimer = slideCrouchExitGroundStickTime;
        }
        else
        {
            slideExitStickTimer = 0f;
        }

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
        Vector3 currentCenter = transform.position + controller.center;
        float currentFootY = currentCenter.y - controller.height * 0.5f;
        Vector3 capsuleBase = new Vector3(currentCenter.x, currentFootY, currentCenter.z);
        Vector3 bottom = capsuleBase + Vector3.up * (radius + 0.08f);
        Vector3 top = capsuleBase + Vector3.up * Mathf.Max(standingHeight - radius, radius + 0.1f);
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
        debugIsSlowWalking = isSlowWalking;
        debugCurrentSpeed = currentSpeed;
        debugDesiredSpeed = desiredSpeed;
        debugCurrentAcceleration = currentAcceleration;
        debugCoyoteTimer = coyoteTimer;
        debugJumpBufferTimer = jumpBufferTimer;
        debugSlideBufferTimer = slideBufferTimer;
        debugGroundedStableTimer = groundedStableTimer;
        debugJumpLockedUntilGrounded = jumpLockedUntilGrounded;
        debugHasLeftGroundSinceJump = hasLeftGroundSinceJump;
        debugHorizontalVelocity = horizontalVelocity;
        debugCapsuleHeight = controller.height;
        debugCameraTargetLocalPosition = cameraTarget != null ? cameraTarget.localPosition : Vector3.zero;
        debugSlideExitStickTimer = slideExitStickTimer;
        if (!wantsToCrouch)
        {
            debugStandBlocker = string.Empty;
        }

        if (isSliding) currentMovementMode = "Slide";
        else if (wantsToCrouch) currentMovementMode = currentSpeed > 0.1f ? "Crouch Move" : "Crouch Idle";
        else if (input.AimPressed) currentMovementMode = currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        else if (!isGrounded) currentMovementMode = verticalVelocity.y > 0f ? "Jumping" : "Falling";
        else if (isSprinting) currentMovementMode = "Sprint";
        else if (isSlowWalking) currentMovementMode = "Slow Walk";
        else if (currentSpeed > 0.1f) currentMovementMode = "Run";
        else currentMovementMode = "Idle";
    }

    public bool IsGrounded() => isGrounded;
    public bool IsSprinting() => isSprinting;
    public bool IsSlowWalking() => isSlowWalking;
    public bool IsAiming() => input != null && input.AimPressed;
    public bool IsCrouching() => wantsToCrouch || isSliding;
    public bool IsSliding() => isSliding;
    public bool JumpedThisFrame() => jumpedThisFrame;
    public bool LandedThisFrame() => landedThisFrame;
    public bool IsJumping() => !isGrounded && verticalVelocity.y > 0.1f;
    public bool IsFalling() => !isGrounded && verticalVelocity.y < -0.1f;
    public float GetCurrentSpeed() => currentSpeed;
    public float GetDesiredSpeed() => desiredSpeed;
    public float GetCurrentAcceleration() => currentAcceleration;
    public string GetMovementMode() => currentMovementMode;
    public Vector3 GetHorizontalVelocity() => horizontalVelocity;
    public Vector3 GetMoveDirection() => moveDirection;
    public Vector3 GetVerticalVelocity() => verticalVelocity;
    public float GetCoyoteTimeRemaining() => coyoteTimer;
    public float GetJumpBufferTimeRemaining() => jumpBufferTimer;
    public float GetSlideBufferTimeRemaining() => slideBufferTimer;
    public float GetGroundedStableTime() => groundedStableTimer;
    public bool IsJumpLockedUntilGrounded() => jumpLockedUntilGrounded;
    public bool HasLeftGroundSinceJump() => hasLeftGroundSinceJump;
    public float GetSlideSpeed() => slideSpeed;
    public string GetStandBlocker() => debugStandBlocker;
    public string GetSlideExitReason() => debugSlideExitReason;
    public float GetSlideExitStickTimeRemaining() => slideExitStickTimer;

    private void OnDrawGizmosSelected()
    {
        CharacterController currentController = controller != null ? controller : GetComponent<CharacterController>();
        if (currentController == null)
        {
            return;
        }

        Vector3 capsuleFoot = transform.position + currentController.center + Vector3.down * (currentController.height * 0.5f);
        Vector3 spherePos = capsuleFoot + Vector3.up * Mathf.Max(groundCheckRadius, 0.02f) + groundCheckOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spherePos, groundCheckRadius);
    }
}
