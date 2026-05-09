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

    private const string IdleState = "Base Layer.Idle";
    private const string WalkState = "Base Layer.Walk";
    private const string SprintState = "Base Layer.Sprint";
    private const string JumpState = "Base Layer.Jump Start";

    private string currentAnimatorStatePath;

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

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

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

        UpdateLocomotionState();
    }

    public void TriggerPrimaryAttack() { }
    public void TriggerAbilityPrimary() { }
    public void TriggerAbilitySecondary() { }
    public void TriggerUltimate() { }
    public void TriggerSlide() { }

    private void UpdateLocomotionState()
    {
        string targetState;

        if (currentIsJumping)
        {
            targetState = JumpState;
        }
        else if (currentIsSprinting)
        {
            targetState = SprintState;
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
        if (motor.IsSliding()) return "Sliding";
        if (motor.IsCrouching()) return currentSpeed > 0.1f ? "Crouch Move" : "Crouch Idle";
        if (currentIsAiming) return currentSpeed > 0.1f ? "Aim Move" : "Aim Idle";
        if (currentIsJumping) return "Jumping";
        if (currentIsFalling) return "Falling";
        if (currentIsSprinting) return "Sprinting";
        if (currentSpeed > 3.5f) return "Running";
        if (currentSpeed > 0.1f) return "Walking";
        return "Idle";
    }
}
