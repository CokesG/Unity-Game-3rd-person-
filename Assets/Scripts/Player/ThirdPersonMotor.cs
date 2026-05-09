using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class ThirdPersonMotor : MonoBehaviour
{
    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float sprintSpeed = 9.0f;
    [SerializeField] private float aimSpeed = 3.0f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private Vector3 groundCheckOffset = Vector3.zero;
    [SerializeField] private LayerMask groundMask;

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
    private float currentSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
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
        HandleMovement();
        HandleRotation();
        HandleGravityAndJump();
    }

    private void CheckGrounded()
    {
        Vector3 capsuleFoot = transform.position + controller.center + Vector3.down * (controller.height * 0.5f);
        Vector3 spherePos = capsuleFoot + Vector3.up * Mathf.Max(groundCheckRadius, 0.02f) + groundCheckOffset;
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;
        bool sphereHit = Physics.CheckSphere(spherePos, groundCheckRadius, mask, QueryTriggerInteraction.Ignore);
        
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded || sphereHit;
        landedThisFrame = !wasGrounded && isGrounded;
        
        // Reset vertical velocity if grounded
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        float inputMag = input.MoveInput.magnitude;
        isSprinting = !input.AimPressed && input.SprintPressed && input.MoveInput.y > 0.1f && inputMag > 0.01f;
        
        // Determine current speed based on input states
        if (input.AimPressed)
        {
            currentSpeed = aimSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            // Simple walk/run threshold for controllers, or just run for WASD
            currentSpeed = inputMag < 0.5f ? walkSpeed : runSpeed;
        }

        if (inputMag < 0.01f) currentSpeed = 0;

        // Calculate move direction relative to camera
        Vector3 moveInput = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
        
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

        // Move character
        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (mainCamera == null) return;

        if (input.AimPressed)
        {
            // Rotate towards camera direction when aiming
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
            // Rotate towards movement direction when not aiming
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
        if (input.JumpTriggered && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpedThisFrame = true;
            input.ConsumeJump();
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    public bool IsGrounded() => isGrounded;
    public bool IsSprinting() => isSprinting;
    public bool IsAiming() => input != null && input.AimPressed;
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
