using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(ThirdPersonMotor))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAnimationController : MonoBehaviour
{
    private const string NightfallExpectedArmatureName = "NightfallVanguard_FullQuality_Armature";
    private const string NightfallLegacyArmatureName = "Armature";

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform characterVisual;

    [Header("Damping")]
    [SerializeField] private float speedDampTime = 0.14f;
    [SerializeField] private float movementDampTime = 0.1f;
    [SerializeField] private float crouchMovementDampTime = 0.16f;
    [SerializeField] private float locomotionCrossFadeTime = 0.18f;
    [SerializeField] private float crouchWalkCrossFadeTime = 0.3f;
    [SerializeField] private float jumpCrossFadeTime = 0.06f;
    [SerializeField] private float crouchWalkEnterSpeed = 0.12f;
    [SerializeField] private float crouchWalkExitSpeed = 0.04f;
    [SerializeField] private float crouchTransitionCrossFadeTime = 0.16f;
    [SerializeField] private float slideEnterCrossFadeTime = 0.08f;
    [SerializeField] private float slideExitToCrouchCrossFadeTime = 0.22f;
    [SerializeField] private float slideExitToStandCrossFadeTime = 0.3f;
    [SerializeField] private float slideExitCrouchSettleDuration = 0.35f;
    [SerializeField] private bool driveAnimatorStateMachine;
    [SerializeField] private bool walkClipPromoted = true;
    [SerializeField] private bool runClipPromoted;
    [SerializeField] private bool sprintClipPromoted;
    [SerializeField] private bool slideClipPromoted;
    [SerializeField] private bool aimStrafeClipPromoted;
    [SerializeField] private bool jumpClipPromoted;
    [SerializeField] private bool airClipPromoted;
    [SerializeField] private bool landClipPromoted;
    [SerializeField] private bool crouchIdleClipPromoted;
    [SerializeField] private bool crouchWalkClipPromoted;
    [SerializeField] private bool standToCrouchClipPromoted;
    [SerializeField] private bool standUpClipPromoted;
    [SerializeField] private bool allowCrouchAnimationClips;
    [SerializeField] private bool forceCrouchWalkWhenMoving;
    [SerializeField] private float landingStateDuration = 0.22f;
    [SerializeField] private float standToCrouchStateDuration = 0.6f;
    [SerializeField] private float standUpStateDuration = 0.6f;
    [SerializeField, Range(0f, 0.99f)] private float crouchIdleHoldNormalizedTime = 0.95f;

    [Header("Visual Grounding")]
    [FormerlySerializedAs("groundVisualDuringCrouch")]
    [SerializeField] private bool groundVisualWhenGrounded = true;
    [SerializeField] private bool preferFootBoneGrounding = true;
    [SerializeField] private float crouchVisualGroundPadding = 0.02f;
    [SerializeField] private float visualGroundResetSpeed = 16f;

    [Header("Debug Readout")]
    [SerializeField] private string currentMovementState;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float currentMovementX;
    [SerializeField] private float currentMovementY;
    [SerializeField] private float currentVerticalVelocity;
    [SerializeField] private bool currentIsGrounded;
    [SerializeField] private bool currentIsAiming;
    [SerializeField] private bool currentIsSprinting;
    [SerializeField] private bool currentIsSlowWalking;
    [SerializeField] private bool currentIsJumping;
    [SerializeField] private bool currentIsFalling;
    [SerializeField] private bool currentIsCrouching;
    [SerializeField] private bool currentIsSliding;
    [SerializeField] private bool currentUsesRunVisual;
    [SerializeField] private bool currentUsesCrouchWalkVisual;
    [SerializeField] private float currentLandingStateTime;
    [SerializeField] private float currentCrouchTransitionStateTime;
    [SerializeField] private float currentVisualGroundOffset;

    private ThirdPersonMotor motor;
    private PlayerInputHandler input;
    private CharacterController controller;

    // Parameter names
    private int speedHash;
    private int movementXHash;
    private int movementYHash;
    private int isGroundedHash;
    private int isSprintingHash;
    private int isAimingHash;
    private int isJumpingHash;
    private int isFallingHash;
    private int isCrouchingHash;
    private int isSlidingHash;
    private int verticalVelocityHash;
    private int jumpHash;
    private int landHash;
    private int primaryAttackHash;
    private int abilityPrimaryHash;
    private int abilitySecondaryHash;
    private int ultimateHash;
    private int slideHash;

    private const string IdleState = "Base Layer.Idle";
    private const string WalkState = "Base Layer.Walk";
    private const string RunState = "Base Layer.Run";
    private const string SprintState = "Base Layer.Sprint";
    private const string SlideState = "Base Layer.Slide";
    private const string JumpState = "Base Layer.Jump Start";
    private const string AirState = "Base Layer.Falling / In Air";
    private const string LandingState = "Base Layer.Landing";
    private const string CrouchIdleState = "Base Layer.Crouch Idle";
    private const string CrouchWalkState = "Base Layer.Crouch Walk";
    private const string StandToCrouchState = "Base Layer.Stand To Crouch";
    private const string StandUpState = "Base Layer.Stand Up";
    private static readonly HumanBodyBones[] VisualGroundingBones =
    {
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightToes,
    };

    private string currentAnimatorStatePath;
    private readonly HashSet<int> availableParameterHashes = new HashSet<int>();
    private RuntimeAnimatorController cachedController;
    private float landingStateTimer;
    private float standToCrouchStateTimer;
    private float standUpStateTimer;
    private bool wasCrouchingLastFrame;
    private bool wasSlidingLastFrame;
    private bool wantsCrouchWalkVisual;
    private int currentAnimatorStateHash;
    private float slideExitCrouchSettleTimer;
    private Vector3 visualBaseLocalPosition;
    private Renderer[] visualRenderers;
    private bool hasVisualBaseLocalPosition;

    private void Awake()
    {
        Transform visualRoot = characterVisual != null && characterVisual.gameObject.activeInHierarchy
            ? characterVisual
            : transform.Find("CharacterVisual");

        if (animator == null || !animator.gameObject.activeInHierarchy)
        {
            animator = visualRoot != null
                ? visualRoot.GetComponentInChildren<Animator>(false)
                : GetComponentInChildren<Animator>(false);
        }

        if ((characterVisual == null || !characterVisual.gameObject.activeInHierarchy) && animator != null)
        {
            characterVisual = animator.transform;
        }

        motor = GetComponent<ThirdPersonMotor>();
        input = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();

        CacheVisualGroundingReferences();

        speedHash = Animator.StringToHash("Speed");
        movementXHash = Animator.StringToHash("MovementX");
        movementYHash = Animator.StringToHash("MovementY");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        isSprintingHash = Animator.StringToHash("IsSprinting");
        isAimingHash = Animator.StringToHash("IsAiming");
        isJumpingHash = Animator.StringToHash("IsJumping");
        isFallingHash = Animator.StringToHash("IsFalling");
        isCrouchingHash = Animator.StringToHash("IsCrouching");
        isSlidingHash = Animator.StringToHash("IsSliding");
        verticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        jumpHash = Animator.StringToHash("Jump");
        landHash = Animator.StringToHash("Land");
        primaryAttackHash = Animator.StringToHash("PrimaryAttack");
        abilityPrimaryHash = Animator.StringToHash("AbilityPrimary");
        abilitySecondaryHash = Animator.StringToHash("AbilitySecondary");
        ultimateHash = Animator.StringToHash("Ultimate");
        slideHash = Animator.StringToHash("Slide");

        if (animator != null)
        {
            EnsureNightfallArmatureName(animator.transform);
            animator.applyRootMotion = false;
            animator.Rebind();
            CacheAnimatorParameters();
        }
    }

    private void LateUpdate()
    {
        if (animator == null) return;

        Vector3 localHorizontalVelocity = transform.InverseTransformDirection(motor.GetHorizontalVelocity());
        currentSpeed = new Vector2(localHorizontalVelocity.x, localHorizontalVelocity.z).magnitude;
        currentVerticalVelocity = motor.GetVerticalVelocity().y;
        currentIsGrounded = motor.IsGrounded();
        currentIsAiming = input.AimPressed;
        currentIsSprinting = motor.IsSprinting();
        currentIsSlowWalking = motor.IsSlowWalking();
        currentIsJumping = motor.IsJumping();
        currentIsFalling = motor.IsFalling();
        currentIsCrouching = motor.IsCrouching();
        currentIsSliding = motor.IsSliding();
        Vector2 movementBlend = ResolveMovementBlend(localHorizontalVelocity);
        currentMovementX = movementBlend.x;
        currentMovementY = movementBlend.y;
        currentUsesRunVisual = ShouldUseRunState();
        UpdateLandingStateTimer();
        UpdateSlideExitCrouchSettleTimer();
        UpdateCrouchTransitionStateTimers();
        UpdateCrouchWalkVisualState();
        currentUsesCrouchWalkVisual = wantsCrouchWalkVisual;
        currentLandingStateTime = landingStateTimer;
        currentCrouchTransitionStateTime = Mathf.Max(standToCrouchStateTimer, standUpStateTimer);
        currentMovementState = ResolveMovementState();

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        CacheAnimatorParameters();

        SetFloatIfAvailable(speedHash, currentSpeed, speedDampTime);
        float directionalDampTime = currentIsCrouching && CanUseCrouchWalkClip() ? crouchMovementDampTime : movementDampTime;
        SetFloatIfAvailable(movementXHash, currentMovementX, directionalDampTime);
        SetFloatIfAvailable(movementYHash, currentMovementY, directionalDampTime);
        SetFloatIfAvailable(verticalVelocityHash, currentVerticalVelocity);
        
        bool driveAirborneParameters = airClipPromoted || landClipPromoted;
        SetBoolIfAvailable(isGroundedHash, driveAirborneParameters ? currentIsGrounded : true);
        SetBoolIfAvailable(isSprintingHash, currentIsSprinting);
        SetBoolIfAvailable(isAimingHash, currentIsAiming);
        SetBoolIfAvailable(isJumpingHash, driveAirborneParameters && currentIsJumping);
        SetBoolIfAvailable(isFallingHash, driveAirborneParameters && currentIsFalling);
        SetBoolIfAvailable(isCrouchingHash, currentIsCrouching);
        SetBoolIfAvailable(isSlidingHash, currentIsSliding);

        if (motor.JumpedThisFrame() && jumpClipPromoted)
        {
            SetTriggerIfAvailable(jumpHash);
        }

        if (motor.LandedThisFrame() && landClipPromoted)
        {
            SetTriggerIfAvailable(landHash);
            landingStateTimer = landingStateDuration;
            currentLandingStateTime = landingStateTimer;
        }

        if (driveAnimatorStateMachine)
        {
            UpdateLocomotionState();
        }

        wasCrouchingLastFrame = currentIsCrouching;
        wasSlidingLastFrame = currentIsSliding;
        ApplyCrouchVisualGrounding();
    }

    public void TriggerPrimaryAttack() => SetTriggerIfAvailable(primaryAttackHash);
    public void TriggerAbilityPrimary() => SetTriggerIfAvailable(abilityPrimaryHash);
    public void TriggerAbilitySecondary() => SetTriggerIfAvailable(abilitySecondaryHash);
    public void TriggerUltimate() => SetTriggerIfAvailable(ultimateHash);
    public void TriggerSlide() => SetTriggerIfAvailable(slideHash);
    public string CurrentMovementState => currentMovementState;
    public float CurrentMovementX => currentMovementX;
    public float CurrentMovementY => currentMovementY;
    public bool CurrentUsesCrouchWalkVisual => currentUsesCrouchWalkVisual;
    public bool CurrentUsesRunVisual => currentUsesRunVisual;
    public bool AimStrafeClipPromoted => aimStrafeClipPromoted;
    public float CurrentCrouchTransitionStateTime => currentCrouchTransitionStateTime;
    public float SlideExitCrouchSettleTime => slideExitCrouchSettleTimer;
    public float CurrentVisualGroundOffset => currentVisualGroundOffset;
    public string CurrentAnimatorStatePath => currentAnimatorStatePath;

    private void UpdateLocomotionState()
    {
        string targetState;

        if (currentIsSliding)
        {
            targetState = SlideState;
        }
        else if (!currentIsGrounded && jumpClipPromoted)
        {
            targetState = JumpState;
        }
        else if (landingStateTimer > 0f && landClipPromoted)
        {
            targetState = LandingState;
        }
        else if (!currentIsGrounded && airClipPromoted)
        {
            targetState = AirState;
        }
        else if (wantsCrouchWalkVisual)
        {
            targetState = CrouchWalkState;
        }
        else if (standToCrouchStateTimer > 0f && CanUseStandToCrouchClip())
        {
            targetState = StandToCrouchState;
        }
        else if (standUpStateTimer > 0f && CanUseStandUpClip())
        {
            targetState = StandUpState;
        }
        else if (currentIsCrouching && CanUseCrouchIdleClip())
        {
            targetState = CrouchIdleState;
        }
        else if (currentIsCrouching && CanUseStandToCrouchClip())
        {
            targetState = StandToCrouchState;
        }
        else if (currentIsSprinting && sprintClipPromoted)
        {
            targetState = SprintState;
        }
        else if (ShouldUseWalkState())
        {
            targetState = walkClipPromoted ? WalkState : IdleState;
        }
        else if (ShouldUseRunState())
        {
            targetState = RunState;
        }
        else if (currentSpeed > 0.1f && walkClipPromoted)
        {
            targetState = WalkState;
        }
        else
        {
            targetState = IdleState;
        }

        CrossFadeIfNeeded(targetState, ResolveCrossFadeTime(targetState));
    }

    private float ResolveCrossFadeTime(string targetState)
    {
        if (targetState == SlideState)
        {
            return slideEnterCrossFadeTime;
        }

        if (currentAnimatorStatePath == SlideState && targetState == CrouchIdleState)
        {
            return slideExitToCrouchCrossFadeTime;
        }

        if (slideExitCrouchSettleTimer > 0f && targetState == StandUpState)
        {
            return slideExitToStandCrossFadeTime;
        }

        if (targetState == StandToCrouchState || targetState == StandUpState)
        {
            return crouchTransitionCrossFadeTime;
        }

        if (targetState == JumpState || targetState == AirState || targetState == LandingState)
        {
            return jumpCrossFadeTime;
        }

        if (targetState == CrouchWalkState || currentAnimatorStatePath == CrouchWalkState)
        {
            return crouchWalkCrossFadeTime;
        }

        return locomotionCrossFadeTime;
    }

    private void UpdateLandingStateTimer()
    {
        if (!currentIsGrounded)
        {
            landingStateTimer = 0f;
            return;
        }

        if (landingStateTimer > 0f)
        {
            landingStateTimer = Mathf.Max(0f, landingStateTimer - Time.deltaTime);
        }
    }

    private void UpdateSlideExitCrouchSettleTimer()
    {
        if (!currentIsSliding && wasSlidingLastFrame && currentIsCrouching)
        {
            slideExitCrouchSettleTimer = slideExitCrouchSettleDuration;
            return;
        }

        if (slideExitCrouchSettleTimer > 0f)
        {
            slideExitCrouchSettleTimer = Mathf.Max(0f, slideExitCrouchSettleTimer - Time.deltaTime);
        }
    }

    private void UpdateCrouchTransitionStateTimers()
    {
        if (!currentIsGrounded)
        {
            standToCrouchStateTimer = 0f;
            standUpStateTimer = 0f;
            return;
        }

        if (currentIsCrouching && !wasCrouchingLastFrame && CanUseStandToCrouchClip())
        {
            standToCrouchStateTimer = standToCrouchStateDuration;
            standUpStateTimer = 0f;
            return;
        }

        if (!currentIsCrouching && wasCrouchingLastFrame && CanUseStandUpClip())
        {
            standUpStateTimer = standUpStateDuration;
            standToCrouchStateTimer = 0f;
            return;
        }

        if (!currentIsCrouching)
        {
            standToCrouchStateTimer = 0f;
        }

        if (currentIsCrouching)
        {
            standUpStateTimer = 0f;
        }

        if (standToCrouchStateTimer > 0f)
        {
            standToCrouchStateTimer = Mathf.Max(0f, standToCrouchStateTimer - Time.deltaTime);
        }

        if (standUpStateTimer > 0f)
        {
            standUpStateTimer = Mathf.Max(0f, standUpStateTimer - Time.deltaTime);
        }
    }

    private bool ShouldUseWalkState()
    {
        return currentSpeed > 0.1f
            && walkClipPromoted
            && (currentIsSlowWalking || currentIsAiming || (currentIsCrouching && !CanUseCrouchWalkClip() && !CanUseStandToCrouchClip()));
    }

    private void UpdateCrouchWalkVisualState()
    {
        if (!currentIsCrouching || !CanUseCrouchWalkClip())
        {
            wantsCrouchWalkVisual = false;
            return;
        }

        float inputMagnitude = input != null ? input.MoveInput.magnitude : 0f;
        float enterSpeed = Mathf.Max(0.01f, crouchWalkEnterSpeed);
        float exitSpeed = Mathf.Clamp(crouchWalkExitSpeed, 0f, enterSpeed);

        if (wantsCrouchWalkVisual)
        {
            wantsCrouchWalkVisual = currentSpeed > exitSpeed || inputMagnitude > 0.05f;
            return;
        }

        wantsCrouchWalkVisual = currentSpeed > enterSpeed || inputMagnitude > 0.1f;
    }

    private Vector2 ResolveMovementBlend(Vector3 localHorizontalVelocity)
    {
        if (currentIsCrouching && CanUseCrouchWalkClip() && input != null)
        {
            Vector2 moveInput = Vector2.ClampMagnitude(input.MoveInput, 1f);
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                return moveInput;
            }
        }

        float movementScale = Mathf.Max(motor.GetCurrentSpeed(), 0.01f);
        return new Vector2(
            Mathf.Clamp(localHorizontalVelocity.x / movementScale, -1f, 1f),
            Mathf.Clamp(localHorizontalVelocity.z / movementScale, -1f, 1f));
    }

    private bool CanUseCrouchIdleClip()
    {
        return allowCrouchAnimationClips && crouchIdleClipPromoted;
    }

    private bool CanUseCrouchWalkClip()
    {
        return allowCrouchAnimationClips && (crouchWalkClipPromoted || forceCrouchWalkWhenMoving);
    }

    private bool CanUseStandToCrouchClip()
    {
        return allowCrouchAnimationClips && standToCrouchClipPromoted;
    }

    private bool CanUseStandUpClip()
    {
        return allowCrouchAnimationClips && standUpClipPromoted;
    }

    private bool ShouldUseRunState()
    {
        return currentSpeed > 0.1f
            && runClipPromoted
            && !currentIsAiming
            && !currentIsCrouching
            && !currentIsSliding
            && !currentIsSlowWalking;
    }

    private void CrossFadeIfNeeded(string stateName, float fadeTime)
    {
        if (!driveAnimatorStateMachine) return;
        int targetStateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, targetStateHash)) return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentAnimatorStateHash == targetStateHash)
        {
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                if (nextState.fullPathHash == targetStateHash || nextState.shortNameHash == targetStateHash)
                {
                    return;
                }
            }

            if (currentState.fullPathHash == targetStateHash || currentState.shortNameHash == targetStateHash)
            {
                return;
            }
        }

        if (stateName == CrouchIdleState)
        {
            if (currentAnimatorStatePath == CrouchWalkState || currentAnimatorStatePath == SlideState)
            {
                animator.CrossFade(targetStateHash, Mathf.Max(0.01f, fadeTime), 0, crouchIdleHoldNormalizedTime);
            }
            else
            {
                animator.Play(targetStateHash, 0, crouchIdleHoldNormalizedTime);
                animator.Update(0f);
            }
        }
        else
        {
            animator.CrossFadeInFixedTime(targetStateHash, fadeTime, 0, 0f);
        }

        currentAnimatorStatePath = stateName;
        currentAnimatorStateHash = targetStateHash;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == cachedController) return;

        cachedController = animator.runtimeAnimatorController;
        currentAnimatorStatePath = null;
        currentAnimatorStateHash = 0;
        availableParameterHashes.Clear();

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            availableParameterHashes.Add(parameter.nameHash);
        }
    }

    private void CacheVisualGroundingReferences()
    {
        if (characterVisual == null)
        {
            return;
        }

        visualBaseLocalPosition = characterVisual.localPosition;
        visualRenderers = characterVisual.GetComponentsInChildren<Renderer>(false);
        hasVisualBaseLocalPosition = true;
    }

    private void ApplyCrouchVisualGrounding()
    {
        if (!groundVisualWhenGrounded
            || characterVisual == null
            || !hasVisualBaseLocalPosition)
        {
            return;
        }

        bool shouldGroundVisual = currentIsGrounded
            && !currentIsJumping
            && !currentIsFalling;

        if (!shouldGroundVisual)
        {
            float resetT = 1f - Mathf.Exp(-visualGroundResetSpeed * Time.deltaTime);
            characterVisual.localPosition = Vector3.Lerp(characterVisual.localPosition, visualBaseLocalPosition, resetT);
            currentVisualGroundOffset = characterVisual.localPosition.y - visualBaseLocalPosition.y;
            return;
        }

        if (controller == null)
        {
            return;
        }

        float capsuleFootY = transform.position.y + controller.center.y - controller.height * 0.5f;
        float targetMinY = capsuleFootY + crouchVisualGroundPadding;

        if (preferFootBoneGrounding && TryGetLowestGroundingBoneY(out float lowestBoneY))
        {
            ApplyVisualWorldYOffset(targetMinY - lowestBoneY);
            return;
        }

        if (!TryGetVisualBounds(out Bounds visualBounds))
        {
            return;
        }

        ApplyVisualWorldYOffset(targetMinY - visualBounds.min.y);
    }

    private void ApplyVisualWorldYOffset(float worldYOffset)
    {
        Vector3 localOffset = characterVisual.parent != null
            ? characterVisual.parent.InverseTransformVector(Vector3.up * worldYOffset)
            : Vector3.up * worldYOffset;

        characterVisual.localPosition += localOffset;
        currentVisualGroundOffset = characterVisual.localPosition.y - visualBaseLocalPosition.y;
    }

    private bool TryGetLowestGroundingBoneY(out float lowestBoneY)
    {
        lowestBoneY = float.PositiveInfinity;
        if (animator == null || !animator.isHuman)
        {
            return false;
        }

        bool foundBone = false;
        foreach (HumanBodyBones groundingBone in VisualGroundingBones)
        {
            Transform bone = animator.GetBoneTransform(groundingBone);
            if (bone == null)
            {
                continue;
            }

            lowestBoneY = Mathf.Min(lowestBoneY, bone.position.y);
            foundBone = true;
        }

        return foundBone;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        if (visualRenderers == null || visualRenderers.Length == 0)
        {
            visualRenderers = characterVisual != null
                ? characterVisual.GetComponentsInChildren<Renderer>(false)
                : null;
        }

        if (visualRenderers == null)
        {
            return false;
        }

        bool hasBounds = false;
        foreach (Renderer visualRenderer in visualRenderers)
        {
            if (visualRenderer == null || !visualRenderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = visualRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(visualRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void EnsureNightfallArmatureName(Transform animatorRoot)
    {
        if (animatorRoot == null)
        {
            return;
        }

        if (FindDeepChild(animatorRoot, NightfallExpectedArmatureName) != null)
        {
            return;
        }

        Transform legacyArmature = FindDeepChild(animatorRoot, NightfallLegacyArmatureName);
        if (legacyArmature != null)
        {
            legacyArmature.name = NightfallExpectedArmatureName;
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private bool HasParameter(int hash)
    {
        return availableParameterHashes.Contains(hash);
    }

    private void SetFloatIfAvailable(int hash, float value, float dampTime = 0f)
    {
        if (!HasParameter(hash)) return;

        if (dampTime > 0f)
        {
            animator.SetFloat(hash, value, dampTime, Time.deltaTime);
            return;
        }

        animator.SetFloat(hash, value);
    }

    private void SetBoolIfAvailable(int hash, bool value)
    {
        if (HasParameter(hash))
        {
            animator.SetBool(hash, value);
        }
    }

    private void SetTriggerIfAvailable(int hash)
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            return;
        }

        CacheAnimatorParameters();
        if (HasParameter(hash))
        {
            animator.SetTrigger(hash);
        }
    }

    private string ResolveMovementState()
    {
        if (motor.IsSliding()) return "Sliding";
        if (standToCrouchStateTimer > 0f && CanUseStandToCrouchClip()) return "Stand To Crouch";
        if (standUpStateTimer > 0f && CanUseStandUpClip()) return "Stand Up";
        if (motor.IsCrouching() && CanUseCrouchWalkClip() && currentSpeed > 0.1f) return "Crouch Move";
        if (motor.IsCrouching() && CanUseCrouchIdleClip()) return "Crouch Idle";
        if (motor.IsCrouching() && CanUseStandToCrouchClip())
        {
            return currentSpeed > 0.1f ? "Crouch Move / Held Pose" : "Crouch Hold";
        }

        if (motor.IsCrouching())
        {
            return currentSpeed > 0.1f ? "Crouch Gameplay / Walk Visual" : "Crouch Gameplay / Idle Visual";
        }

        if (currentIsAiming) return currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        if (!currentIsGrounded && jumpClipPromoted) return "Jumping";
        if (landingStateTimer > 0f && landClipPromoted) return "Landing";
        if (!currentIsGrounded) return "Airborne / Locomotion";
        if (currentIsSprinting) return sprintClipPromoted ? "Sprinting" : "Sprint Speed / Run Visual";
        if (currentIsSlowWalking) return "Slow Walking";
        if (currentUsesRunVisual) return "Running";
        if (currentSpeed > 0.1f) return "Walking";
        return "Idle";
    }
}
