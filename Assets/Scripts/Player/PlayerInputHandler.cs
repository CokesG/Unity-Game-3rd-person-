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
    [SerializeField] private string slowWalkActionName = "SlowWalk";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string reloadActionName = "Reload";
    [SerializeField] private string shoulderSwapActionName = "ShoulderSwap";
    [SerializeField] private string crouchActionName = "Crouch";
    [SerializeField] private string crouchFallbackActionName = "Slide";
    [SerializeField] private bool slowWalkFallbackToCtrl = true;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction slowWalkAction;
    private InputAction aimAction;
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction shoulderSwapAction;
    private InputAction crouchAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool SlowWalkPressed { get; private set; }
    public bool AimPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool AttackTriggered { get; private set; }
    public bool ReloadTriggered { get; private set; }
    public bool ShoulderSwapTriggered { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool CrouchTriggered { get; private set; }

    private void OnEnable()
    {
        if (playerControls == null) return;

        moveAction = playerControls.FindAction(moveActionName);
        lookAction = playerControls.FindAction(lookActionName);
        jumpAction = playerControls.FindAction(jumpActionName);
        sprintAction = playerControls.FindAction(sprintActionName);
        slowWalkAction = playerControls.FindAction(slowWalkActionName);
        aimAction = playerControls.FindAction(aimActionName);
        attackAction = playerControls.FindAction(attackActionName);
        reloadAction = playerControls.FindAction(reloadActionName);
        shoulderSwapAction = playerControls.FindAction(shoulderSwapActionName);
        crouchAction = playerControls.FindAction(crouchActionName) ?? playerControls.FindAction(crouchFallbackActionName);

        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        slowWalkAction?.Enable();
        aimAction?.Enable();
        attackAction?.Enable();
        reloadAction?.Enable();
        shoulderSwapAction?.Enable();
        crouchAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        slowWalkAction?.Disable();
        aimAction?.Disable();
        attackAction?.Disable();
        reloadAction?.Disable();
        shoulderSwapAction?.Disable();
        crouchAction?.Disable();
    }

    private void Update()
    {
        if (moveAction != null) MoveInput = moveAction.ReadValue<Vector2>();
        if (lookAction != null) LookInput = lookAction.ReadValue<Vector2>();
        
        if (jumpAction != null) JumpTriggered = jumpAction.triggered;
        if (sprintAction != null) SprintPressed = sprintAction.IsPressed();
        SlowWalkPressed = slowWalkAction != null && slowWalkAction.IsPressed();
        if (!SlowWalkPressed && slowWalkFallbackToCtrl && Keyboard.current != null)
        {
            SlowWalkPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        }

        if (aimAction != null) AimPressed = aimAction.IsPressed();
        if (attackAction != null)
        {
            AttackPressed = attackAction.IsPressed();
            AttackTriggered = attackAction.triggered;
        }
        else if (Mouse.current != null)
        {
            AttackPressed = Mouse.current.leftButton.isPressed;
            AttackTriggered = Mouse.current.leftButton.wasPressedThisFrame;
        }

        ReloadTriggered = reloadAction != null && reloadAction.triggered;
        if (!ReloadTriggered && Keyboard.current != null)
        {
            ReloadTriggered = Keyboard.current.rKey.wasPressedThisFrame;
        }

        ShoulderSwapTriggered = shoulderSwapAction != null && shoulderSwapAction.triggered;
        if (!ShoulderSwapTriggered)
        {
            bool keyboardSwap = Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
            bool mouseSwap = Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
            ShoulderSwapTriggered = keyboardSwap || mouseSwap;
        }

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

    public void ConsumeAttack()
    {
        AttackTriggered = false;
    }

    public void ConsumeReload()
    {
        ReloadTriggered = false;
    }

    public void ConsumeShoulderSwap()
    {
        ShoulderSwapTriggered = false;
    }

    public void ConsumeCrouch()
    {
        CrouchTriggered = false;
    }
}
