using UnityEngine;

[RequireComponent(typeof(ThirdPersonMotor))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform characterVisual;

    [Header("Damping")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float movementDampTime = 0.08f;
    [SerializeField] private float locomotionCrossFadeTime = 0.12f;
    [SerializeField] private float jumpCrossFadeTime = 0.06f;
    [SerializeField] private float actionLockSeconds = 0.8f;

    [Header("Debug Readout")]
    [SerializeField] private string currentMovementState;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float currentMovementX;
    [SerializeField] private float currentMovementY;
    [SerializeField] private float currentVerticalVelocity;
    [SerializeField] private bool currentIsGrounded;
    [SerializeField] private bool currentIsAiming;
    [SerializeField] private bool currentIsSprinting;
    [SerializeField] private bool currentIsJumping;
    [SerializeField] private bool currentIsFalling;

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
    private int verticalVelocityHash;
    private int jumpHash;
    private int landHash;
    private int primaryAttackHash;
    private int abilityPrimaryHash;
    private int abilitySecondaryHash;
    private int ultimateHash;

    private const string IdleState = "Base Layer.Idle";
    private const string WalkState = "Base Layer.Walk";
    private const string RunState = "Base Layer.Run/Jog";
    private const string JumpState = "Base Layer.Jump Start";
    private const string FallingState = "Base Layer.Falling / In Air";
    private const string AimIdleState = "Base Layer.Aim Idle";
    private const string AimMoveState = "Base Layer.Aim Walk / Strafe";
    private const string PrimaryAttackState = "Base Layer.Attack Placeholder";
    private const string AbilityPrimaryState = "Base Layer.Ability Placeholder";
    private const string AbilitySecondaryState = "Base Layer.Ability Secondary Placeholder";
    private const string UltimateState = "Base Layer.Ultimate Placeholder";
    private const string SlideState = "Base Layer.Slide Placeholder";
    private const float ActionCrossFadeTime = 0.04f;

    private string currentAnimatorStatePath;
    private float actionLockUntil;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (characterVisual == null && animator != null)
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
        verticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        jumpHash = Animator.StringToHash("Jump");
        landHash = Animator.StringToHash("Land");
        primaryAttackHash = Animator.StringToHash("PrimaryAttack");
        abilityPrimaryHash = Animator.StringToHash("AbilityPrimary");
        abilitySecondaryHash = Animator.StringToHash("AbilitySecondary");
        ultimateHash = Animator.StringToHash("Ultimate");

        if (animator != null)
        {
            animator.applyRootMotion = false;
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
        currentIsJumping = motor.IsJumping();
        currentIsFalling = motor.IsFalling();
        currentMovementState = ResolveMovementState();

        animator.SetFloat(speedHash, currentSpeed, speedDampTime, Time.deltaTime);
        animator.SetFloat(movementXHash, currentMovementX, movementDampTime, Time.deltaTime);
        animator.SetFloat(movementYHash, currentMovementY, movementDampTime, Time.deltaTime);
        animator.SetFloat(verticalVelocityHash, currentVerticalVelocity);
        
        animator.SetBool(isGroundedHash, currentIsGrounded);
        animator.SetBool(isSprintingHash, currentIsSprinting);
        animator.SetBool(isAimingHash, currentIsAiming);
        animator.SetBool(isJumpingHash, currentIsJumping);
        animator.SetBool(isFallingHash, currentIsFalling);

        if (motor.JumpedThisFrame())
        {
            animator.SetTrigger(jumpHash);
            CrossFadeIfNeeded(JumpState, jumpCrossFadeTime);
            return;
        }

        if (motor.LandedThisFrame())
        {
            animator.SetTrigger(landHash);
        }

        if (Time.time < actionLockUntil) return;

        UpdateLocomotionState();
    }

    public void TriggerPrimaryAttack() => TriggerAction(primaryAttackHash, PrimaryAttackState);
    public void TriggerAbilityPrimary() => TriggerAction(abilityPrimaryHash, AbilityPrimaryState);
    public void TriggerAbilitySecondary() => TriggerAction(abilitySecondaryHash, AbilitySecondaryState);
    public void TriggerUltimate() => TriggerAction(ultimateHash, UltimateState);
    public void TriggerSlide() => TriggerAction(0, SlideState);

    private void TriggerAction(int triggerHash, string stateName)
    {
        if (animator == null) return;

        if (triggerHash != 0)
        {
            animator.SetTrigger(triggerHash);
        }

        animator.CrossFadeInFixedTime(stateName, ActionCrossFadeTime, 0, 0f);
        currentAnimatorStatePath = stateName;
        actionLockUntil = Time.time + actionLockSeconds;
    }

    private void UpdateLocomotionState()
    {
        string targetState;

        if (currentIsJumping)
        {
            targetState = JumpState;
        }
        else if (currentIsFalling)
        {
            targetState = FallingState;
        }
        else if (currentIsAiming)
        {
            targetState = currentSpeed > 0.1f ? AimMoveState : AimIdleState;
        }
        else if (currentIsSprinting)
        {
            targetState = RunState;
        }
        else if (currentSpeed > 0.1f)
        {
            targetState = WalkState;
        }
        else
        {
            targetState = IdleState;
        }

        CrossFadeIfNeeded(targetState, locomotionCrossFadeTime);
    }

    private void CrossFadeIfNeeded(string stateName, float fadeTime)
    {
        if (currentAnimatorStatePath == stateName) return;

        animator.CrossFadeInFixedTime(stateName, fadeTime, 0, 0f);
        currentAnimatorStatePath = stateName;
    }

    private string ResolveMovementState()
    {
        if (currentIsAiming) return currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        if (currentIsJumping) return "Jumping";
        if (currentIsFalling) return "Falling";
        if (currentIsSprinting) return "Sprinting";
        if (currentSpeed > 3.5f) return "Running";
        if (currentSpeed > 0.1f) return "Walking";
        return "Idle";
    }
}
