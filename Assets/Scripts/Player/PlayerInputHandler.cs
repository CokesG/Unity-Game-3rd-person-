using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Names")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string crouchActionName = "Crouch";
    [SerializeField] private string crouchFallbackActionName = "Slide";

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction aimAction;
    private InputAction crouchAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool AimPressed { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool CrouchTriggered { get; private set; }

    private void OnEnable()
    {
        if (playerControls == null) return;

        moveAction = playerControls.FindAction(moveActionName);
        lookAction = playerControls.FindAction(lookActionName);
        jumpAction = playerControls.FindAction(jumpActionName);
        sprintAction = playerControls.FindAction(sprintActionName);
        aimAction = playerControls.FindAction(aimActionName);
        crouchAction = playerControls.FindAction(crouchActionName) ?? playerControls.FindAction(crouchFallbackActionName);

        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        aimAction?.Enable();
        crouchAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        aimAction?.Disable();
        crouchAction?.Disable();
    }

    private void Update()
    {
        if (moveAction != null) MoveInput = moveAction.ReadValue<Vector2>();
        if (lookAction != null) LookInput = lookAction.ReadValue<Vector2>();
        
        if (jumpAction != null) JumpTriggered = jumpAction.triggered;
        if (sprintAction != null) SprintPressed = sprintAction.IsPressed();
        if (aimAction != null) AimPressed = aimAction.IsPressed();
        if (crouchAction != null)
        {
            CrouchPressed = crouchAction.IsPressed();
            CrouchTriggered = crouchAction.triggered;
        }
    }

    public void ConsumeJump()
    {
        JumpTriggered = false;
    }

    public void ConsumeCrouch()
    {
        CrouchTriggered = false;
    }
}
