using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatHooks : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    private InputAction attackAction;
    private InputAction ability1Action;
    private InputAction ability2Action;
    private InputAction ultimateAction;
    private InputAction interactAction;
    private PlayerAnimationController animationController;

    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationController>();
    }

    private void OnEnable()
    {
        if (playerControls == null) return;

        attackAction = playerControls.FindAction("Attack");
        ability1Action = playerControls.FindAction("Ability1");
        ability2Action = playerControls.FindAction("Ability2");
        ultimateAction = playerControls.FindAction("Ultimate");
        interactAction = playerControls.FindAction("Interact");

        if (attackAction != null) { attackAction.started += OnAttack; attackAction.Enable(); }
        if (ability1Action != null) { ability1Action.started += OnAbility1; ability1Action.Enable(); }
        if (ability2Action != null) { ability2Action.started += OnAbility2; ability2Action.Enable(); }
        if (ultimateAction != null) { ultimateAction.started += OnUltimate; ultimateAction.Enable(); }
        if (interactAction != null) { interactAction.started += OnInteract; interactAction.Enable(); }
    }

    private void OnDisable()
    {
        if (attackAction != null) attackAction.started -= OnAttack;
        if (ability1Action != null) ability1Action.started -= OnAbility1;
        if (ability2Action != null) ability2Action.started -= OnAbility2;
        if (ultimateAction != null) ultimateAction.started -= OnUltimate;
        if (interactAction != null) interactAction.started -= OnInteract;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        animationController?.TriggerPrimaryAttack();
        Debug.Log("Attack Triggered");
    }

    private void OnAbility1(InputAction.CallbackContext context)
    {
        animationController?.TriggerAbilityPrimary();
        Debug.Log("Ability 1 Triggered");
    }

    private void OnAbility2(InputAction.CallbackContext context)
    {
        animationController?.TriggerAbilitySecondary();
        Debug.Log("Ability 2 Triggered");
    }

    private void OnUltimate(InputAction.CallbackContext context)
    {
        animationController?.TriggerUltimate();
        Debug.Log("Ultimate Triggered");
    }
    private void OnInteract(InputAction.CallbackContext context) => Debug.Log("Interact Triggered");
}
