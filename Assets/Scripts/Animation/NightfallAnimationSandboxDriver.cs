using UnityEngine;
using UnityEngine.InputSystem;

public class NightfallAnimationSandboxDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewTarget;
    [SerializeField] private float crossFadeTime = 0.12f;
    [SerializeField] private Vector3 previewTargetOffset = new Vector3(0f, 1.05f, 0f);
    [SerializeField] private Vector3 previewCameraOffset = new Vector3(0f, 1.35f, -4.25f);
    [SerializeField] private float previewCameraFieldOfView = 35f;
    [SerializeField] private bool showHud = true;
    [SerializeField] private int currentStateIndex;

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

        if (previewCamera == null)
        {
            GameObject cameraObject = GameObject.Find("SandboxCamera");
            previewCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
        }
    }

    private void Start()
    {
        FramePreviewCamera();
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
                PlayState(i);
            }
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            PlayState((currentStateIndex + 1) % stateNames.Length);
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            PlayState((currentStateIndex - 1 + stateNames.Length) % stateNames.Length);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            FramePreviewCamera();
        }
    }

    private void PlayState(int index)
    {
        currentStateIndex = Mathf.Clamp(index, 0, stateNames.Length - 1);
        PlayState(stateNames[currentStateIndex]);
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

    private void FramePreviewCamera()
    {
        if (previewCamera == null)
        {
            return;
        }

        previewCamera.enabled = true;
        previewCamera.depth = 100f;
        previewCamera.fieldOfView = previewCameraFieldOfView;

        Vector3 origin = GetPreviewOrigin();
        Vector3 lookTarget = origin + previewTargetOffset;
        Vector3 cameraPosition = origin + previewCameraOffset;
        Vector3 lookDirection = lookTarget - cameraPosition;

        previewCamera.transform.position = cameraPosition;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            previewCamera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    private Vector3 GetPreviewOrigin()
    {
        if (previewTarget != null)
        {
            return previewTarget.position;
        }

        return animator != null ? animator.transform.position : transform.position;
    }

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        GUI.Label(new Rect(24f, 24f, 520f, 24f), "Nightfall Animation Sandbox");
        GUI.Label(new Rect(24f, 48f, 780f, 24f), "1 Idle | 2 Walk | 3 Run | 4 Sprint | 5 Jump | 6 Aim | 7 Slide | 8 Attack | 9 Ability | 0 Roll");
        GUI.Label(new Rect(24f, 72f, 780f, 24f), "Tab / Space / Right Arrow next | Left Arrow previous | F reset camera");
        GUI.Label(new Rect(24f, 96f, 520f, 24f), "Current: " + currentState);
    }
}
