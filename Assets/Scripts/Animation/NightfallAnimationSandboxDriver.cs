using UnityEngine;
using UnityEngine.InputSystem;

public class NightfallAnimationSandboxDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeTime = 0.12f;
    [SerializeField] private bool showHud = true;

    private readonly string[] stateNames =
    {
        "Idle",
        "Walk",
        "Run/Jog",
        "Sprint",
        "Jump Start",
        "Aim Walk / Strafe",
        "Slide",
        "Attack Placeholder",
        "Ability Placeholder",
        "Roll Dodge"
    };

    private readonly Key[] keys =
    {
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4,
        Key.Digit5,
        Key.Digit6,
        Key.Digit7,
        Key.Digit8,
        Key.Digit9,
        Key.Digit0
    };

    private string currentState = "Idle";

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        PlayState(currentState);
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        int count = Mathf.Min(stateNames.Length, keys.Length);
        for (int i = 0; i < count; i++)
        {
            if (Keyboard.current != null && Keyboard.current[keys[i]].wasPressedThisFrame)
            {
                PlayState(stateNames[i]);
            }
        }
    }

    private void PlayState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        currentState = stateName;
        animator.CrossFadeInFixedTime(stateHash, crossFadeTime, 0, 0f);
    }

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        GUI.Label(new Rect(24f, 24f, 520f, 24f), "Nightfall Animation Sandbox");
        GUI.Label(new Rect(24f, 48f, 520f, 24f), "1 Idle | 2 Walk | 3 Run | 4 Sprint | 5 Jump | 6 Aim | 7 Slide | 8 Attack | 9 Ability | 0 Roll");
        GUI.Label(new Rect(24f, 72f, 520f, 24f), "Current: " + currentState);
    }
}
