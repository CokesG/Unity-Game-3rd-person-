using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonMotor))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform characterVisual;

    [Header("Damping")]
    [SerializeField] private float speedDampTime = 0.14f;
    [SerializeField] private float movementDampTime = 0.1f;
    [SerializeField] private float locomotionCrossFadeTime = 0.18f;
    [SerializeField] private float jumpCrossFadeTime = 0.06f;
    [SerializeField] private bool driveAnimatorStateMachine;
    [SerializeField] private bool walkClipPromoted = true;
    [SerializeField] private bool runClipPromoted;
    [SerializeField] private bool sprintClipPromoted;
    [SerializeField] private bool jumpClipPromoted;

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

    private ThirdPersonMotor motor;
    private PlayerInputHandler input;

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
    private const string RunState = "Base Layer.Run/Jog";
    private const string SprintState = "Base Layer.Sprint";
    private const string JumpState = "Base Layer.Jump Start";

    private string currentAnimatorStatePath;
    private readonly HashSet<int> availableParameterHashes = new HashSet<int>();
    private RuntimeAnimatorController cachedController;

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
            animator.applyRootMotion = false;
            CacheAnimatorParameters();
        }
    }

    private void LateUpdate()
    {
        if (animator == null) return;

        Vector3 localHorizontalVelocity = transform.InverseTransformDirection(motor.GetHorizontalVelocity());
        currentSpeed = new Vector2(localHorizontalVelocity.x, localHorizontalVelocity.z).magnitude;
        currentMovementX = Mathf.Clamp(localHorizontalVelocity.x / Mathf.Max(motor.GetCurrentSpeed(), 0.01f), -1f, 1f);
        currentMovementY = Mathf.Clamp(localHorizontalVelocity.z / Mathf.Max(motor.GetCurrentSpeed(), 0.01f), -1f, 1f);
        currentVerticalVelocity = motor.GetVerticalVelocity().y;
        currentIsGrounded = motor.IsGrounded();
        currentIsAiming = input.AimPressed;
        currentIsSprinting = motor.IsSprinting();
        currentIsSlowWalking = motor.IsSlowWalking();
        currentIsJumping = motor.IsJumping();
        currentIsFalling = motor.IsFalling();
        currentIsCrouching = motor.IsCrouching();
        currentIsSliding = motor.IsSliding();
        currentUsesRunVisual = ShouldUseRunState();
        currentMovementState = ResolveMovementState();

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        CacheAnimatorParameters();

        SetFloatIfAvailable(speedHash, currentSpeed, speedDampTime);
        SetFloatIfAvailable(movementXHash, currentMovementX, movementDampTime);
        SetFloatIfAvailable(movementYHash, currentMovementY, movementDampTime);
        SetFloatIfAvailable(verticalVelocityHash, currentVerticalVelocity);
        
        SetBoolIfAvailable(isGroundedHash, currentIsGrounded);
        SetBoolIfAvailable(isSprintingHash, currentIsSprinting);
        SetBoolIfAvailable(isAimingHash, currentIsAiming);
        SetBoolIfAvailable(isJumpingHash, currentIsJumping);
        SetBoolIfAvailable(isFallingHash, currentIsFalling);
        SetBoolIfAvailable(isCrouchingHash, currentIsCrouching);
        SetBoolIfAvailable(isSlidingHash, currentIsSliding);

        if (motor.JumpedThisFrame())
        {
            SetTriggerIfAvailable(jumpHash);
            if (jumpClipPromoted)
            {
                CrossFadeIfNeeded(JumpState, jumpCrossFadeTime);
                return;
            }
        }

        if (motor.LandedThisFrame())
        {
            SetTriggerIfAvailable(landHash);
        }

        if (driveAnimatorStateMachine)
        {
            UpdateLocomotionState();
        }
    }

    public void TriggerPrimaryAttack() => SetTriggerIfAvailable(primaryAttackHash);
    public void TriggerAbilityPrimary() => SetTriggerIfAvailable(abilityPrimaryHash);
    public void TriggerAbilitySecondary() => SetTriggerIfAvailable(abilitySecondaryHash);
    public void TriggerUltimate() => SetTriggerIfAvailable(ultimateHash);
    public void TriggerSlide() => SetTriggerIfAvailable(slideHash);

    private void UpdateLocomotionState()
    {
        string targetState;

        if (currentIsJumping && jumpClipPromoted)
        {
            targetState = JumpState;
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

        CrossFadeIfNeeded(targetState, locomotionCrossFadeTime);
    }

    private bool ShouldUseWalkState()
    {
        return currentSpeed > 0.1f
            && walkClipPromoted
            && (currentIsSlowWalking || currentIsAiming || currentIsCrouching);
    }

    private bool ShouldUseRunState()
    {
        return currentSpeed > 0.1f
            && runClipPromoted
            && currentIsGrounded
            && !currentIsAiming
            && !currentIsCrouching
            && !currentIsSliding
            && !currentIsSlowWalking;
    }

    private void CrossFadeIfNeeded(string stateName, float fadeTime)
    {
        if (!driveAnimatorStateMachine) return;
        if (currentAnimatorStatePath == stateName) return;
        if (!animator.HasState(0, Animator.StringToHash(stateName))) return;

        animator.CrossFadeInFixedTime(stateName, fadeTime, 0, 0f);
        currentAnimatorStatePath = stateName;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == cachedController) return;

        cachedController = animator.runtimeAnimatorController;
        availableParameterHashes.Clear();

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            availableParameterHashes.Add(parameter.nameHash);
        }
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
        if (motor.IsCrouching()) return currentSpeed > 0.1f ? "Crouch Move" : "Crouch Idle";
        if (currentIsAiming) return currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        if (currentIsJumping) return "Jumping";
        if (currentIsFalling) return "Falling";
        if (currentIsSprinting) return sprintClipPromoted ? "Sprinting" : "Sprint Speed / Run Visual";
        if (currentIsSlowWalking) return "Slow Walking";
        if (currentUsesRunVisual) return "Running";
        if (currentSpeed > 0.1f) return "Walking";
        return "Idle";
    }
}
