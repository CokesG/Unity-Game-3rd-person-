using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NightfallAnimationSandboxDriver : MonoBehaviour
{
    private const string PreferredRigName = "NightfallVanguard_ModelOnly_FullQuality_NoAnimations";
    private const string LegacyPreferredRigName = "NightfallVanguard_LinkedAnimationRig";
    private const string RawPreviewRigName = "NightfallVanguard_RawAnimationPreviewRig";
    private const string ExpectedArmatureName = "NightfallVanguard_FullQuality_Armature";
    private const string LegacyArmatureName = "Armature";

    private static NightfallAnimationSandboxDriver activeDriver;

    [SerializeField] private Animator animator;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewTarget;
    [SerializeField] private float crossFadeTime = 0.12f;
    [SerializeField] private Vector3 previewTargetOffset = new Vector3(0f, 1.05f, 0f);
    [SerializeField] private Vector3 previewCameraOffset = new Vector3(0f, 1.45f, -5.75f);
    [SerializeField] private float previewCameraFieldOfView = 42f;
    [SerializeField] private bool snapPreviewToGround = true;
    [SerializeField] private float previewGroundY = 0.02f;
    [SerializeField] private bool showHud = true;
    [SerializeField] private int currentStateIndex;

    [Header("Crouch Walk Review")]
    [SerializeField] private bool enableCrouchWalkDirectionalReview;
    [SerializeField] private bool autoLoadCrouchWalkReviewClips = true;
    [SerializeField] private AnimationClip[] crouchWalkReviewClips;
    [SerializeField] private int currentCrouchWalkReviewClipIndex;

    private readonly string[] stateNames =
    {
        "Idle",
        "Walk",
        "Run/Jog",
        "Jump Start",
        "Falling / In Air",
        "Landing",
        "Stand To Crouch",
        "Crouch Idle",
        "Crouch Walk",
        "Stand Up",
        "Running Jump",
        "Sprint",
        "Aim Walk / Strafe",
        "Slide",
        "Attack Placeholder",
        "Ability Placeholder"
    };

    private readonly string[] buttonLabels =
    {
        "Idle",
        "Walk",
        "Run/Jog",
        "Jump Start",
        "Air",
        "Land",
        "Stand -> Crouch",
        "Crouched Idle",
        "Crouched Walk",
        "Crouch -> Stand",
        "Running Jump",
        "Sprint",
        "Aim Strafe",
        "Slide",
        "Attack",
        "Ability"
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

    private readonly Key[] numpadKeys =
    {
        Key.Numpad1,
        Key.Numpad2,
        Key.Numpad3,
        Key.Numpad4,
        Key.Numpad5,
        Key.Numpad6,
        Key.Numpad7,
        Key.Numpad8,
        Key.Numpad9,
        Key.Numpad0
    };

    private string currentState = "Idle";
    private string statusText = "Ready";
    private AnimatorOverrideController crouchWalkReviewOverrideController;
    private RuntimeAnimatorController crouchWalkReviewBaseController;
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> crouchWalkClipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

    private void OnEnable()
    {
        ActivatePreferredRigIfPresent();

        if (IsRawPreviewDriver())
        {
            DisableDuplicateDriver();
            return;
        }

        if (activeDriver == null || !activeDriver.isActiveAndEnabled || DriverPriority() > activeDriver.DriverPriority())
        {
            if (activeDriver != null && activeDriver != this)
            {
                activeDriver.DisableDuplicateDriver();
            }

            activeDriver = this;
            return;
        }

        if (activeDriver != this)
        {
            DisableDuplicateDriver();
        }
    }

    private void OnDisable()
    {
        if (activeDriver == this)
        {
            activeDriver = null;
        }
    }

    private void Awake()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ResolveAnimator();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = GameObject.Find("SandboxCamera");
            previewCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
        }

        if (animator != null)
        {
            EnsureNightfallArmatureName(animator.transform.root);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
        }
    }

    private void Start()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            if (ShouldUseCrouchWalkDirectionalReview())
            {
                EnsureCrouchWalkReviewClips();
                ApplyCrouchWalkReviewClip();
            }
        }

        FramePreviewCamera();
        PlayState(currentState);
    }

    private void Update()
    {
        if (activeDriver != this)
        {
            return;
        }

        if (animator == null)
        {
            return;
        }

        int count = Mathf.Min(stateNames.Length, keys.Length);
        for (int i = 0; i < count; i++)
        {
            if (Keyboard.current != null && WasStateKeyPressed(i))
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

        if (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            CycleCrouchWalkReviewClip(-1);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            CycleCrouchWalkReviewClip(1);
        }
    }

    private void PlayState(int index)
    {
        currentStateIndex = Mathf.Clamp(index, 0, stateNames.Length - 1);
        PlayState(stateNames[currentStateIndex]);
    }

    private void PlayState(string stateName)
    {
        if (activeDriver != this)
        {
            return;
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            statusText = "No Animator/controller assigned";
            return;
        }

        if (stateName == "Crouch Walk")
        {
            if (ShouldUseCrouchWalkDirectionalReview())
            {
                EnsureCrouchWalkReviewClips();
                ApplyCrouchWalkReviewClip();
            }
            else
            {
                PlayAnimatorState("Crouch Idle", stateName, "Crouch-walk review disabled: showing crouch hold");
                return;
            }
        }

        PlayAnimatorState(stateName);
    }

    private void PlayAnimatorState(string stateName, string displayStateName = null, string statusOverride = null)
    {
        if (!TryResolveStateHash(stateName, out int stateHash))
        {
            statusText = "Missing Animator state: " + stateName;
            return;
        }

        currentState = displayStateName ?? stateName;
        statusText = statusOverride ?? "Playing: " + stateName;
        animator.CrossFadeInFixedTime(stateHash, crossFadeTime, 0, 0f);
        animator.Update(1f / 60f);
        SnapAnimatorToGround();
    }

    private void EnsureCrouchWalkReviewClips()
    {
        if (!ShouldUseCrouchWalkDirectionalReview() || !autoLoadCrouchWalkReviewClips)
        {
            return;
        }

#if UNITY_EDITOR
        const string folder = "Assets/Animations/NightfallVanguard/UserCrouchWalk";
        string[] directions = { "Forward", "Back", "Left", "Right" };
        List<AnimationClip> clips = new List<AnimationClip>();

        for (int i = 0; i < directions.Length; i++)
        {
            string path = folder + "/User_CrouchWalk_" + directions[i] + ".fbx";
            AnimationClip clip = LoadFirstAnimationClipAtPath(path);
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        crouchWalkReviewClips = clips.ToArray();
#endif
    }

#if UNITY_EDITOR
    private static AnimationClip LoadFirstAnimationClipAtPath(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            {
                return clip;
            }
        }

        return null;
    }
#endif

    private void CycleCrouchWalkReviewClip(int direction)
    {
        if (!ShouldUseCrouchWalkDirectionalReview())
        {
            statusText = "Crouch-walk directional review disabled";
            return;
        }

        EnsureCrouchWalkReviewClips();
        if (crouchWalkReviewClips == null || crouchWalkReviewClips.Length == 0)
        {
            statusText = "No crouch-walk review clips found";
            return;
        }

        currentCrouchWalkReviewClipIndex = (currentCrouchWalkReviewClipIndex + direction + crouchWalkReviewClips.Length) % crouchWalkReviewClips.Length;
        ApplyCrouchWalkReviewClip();

        if (currentState == "Crouch Walk")
        {
            PlayState(currentStateIndex);
        }
    }

    private void ApplyCrouchWalkReviewClip()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (!ShouldUseCrouchWalkDirectionalReview())
        {
            return;
        }

        EnsureCrouchWalkReviewClips();
        AnimationClip selectedClip = GetCurrentCrouchWalkReviewClip();
        if (selectedClip == null)
        {
            return;
        }

        RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
        if (runtimeController is AnimatorOverrideController existingOverride)
        {
            crouchWalkReviewOverrideController = existingOverride;
            crouchWalkReviewBaseController = existingOverride.runtimeAnimatorController;
        }
        else if (crouchWalkReviewOverrideController == null || crouchWalkReviewBaseController != runtimeController)
        {
            crouchWalkReviewBaseController = runtimeController;
            crouchWalkReviewOverrideController = new AnimatorOverrideController(crouchWalkReviewBaseController)
            {
                name = crouchWalkReviewBaseController.name + "_CrouchWalkReview"
            };
            animator.runtimeAnimatorController = crouchWalkReviewOverrideController;
        }

        crouchWalkReviewOverrideController.GetOverrides(crouchWalkClipOverrides);
        for (int i = 0; i < crouchWalkClipOverrides.Count; i++)
        {
            AnimationClip sourceClip = crouchWalkClipOverrides[i].Key;
            if (sourceClip == null || !IsCrouchWalkClipName(sourceClip.name))
            {
                continue;
            }

            crouchWalkClipOverrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(sourceClip, selectedClip);
        }

        crouchWalkReviewOverrideController.ApplyOverrides(crouchWalkClipOverrides);
        statusText = "Reviewing " + ShortClipName(selectedClip.name);
    }

    private AnimationClip GetCurrentCrouchWalkReviewClip()
    {
        if (crouchWalkReviewClips == null || crouchWalkReviewClips.Length == 0)
        {
            return null;
        }

        currentCrouchWalkReviewClipIndex = Mathf.Clamp(currentCrouchWalkReviewClipIndex, 0, crouchWalkReviewClips.Length - 1);
        return crouchWalkReviewClips[currentCrouchWalkReviewClipIndex];
    }

    private static bool IsCrouchWalkClipName(string clipName)
    {
        return clipName.IndexOf("CrouchWalk", System.StringComparison.OrdinalIgnoreCase) >= 0
            || clipName.IndexOf("Crouch_Walk", System.StringComparison.OrdinalIgnoreCase) >= 0
            || clipName.IndexOf("Crouch Walk", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ShortClipName(string clipName)
    {
        return clipName
            .Replace("Nightfall_FullQuality_", string.Empty)
            .Replace("User_", string.Empty)
            .Replace("_Procedural", string.Empty)
            .Replace('_', ' ');
    }

    private bool ShouldUseCrouchWalkDirectionalReview()
    {
        return enableCrouchWalkDirectionalReview;
    }

    private void LateUpdate()
    {
        if (activeDriver == this)
        {
            SnapAnimatorToGround();
        }
    }

    private bool WasStateKeyPressed(int index)
    {
        if (Keyboard.current[keys[index]].wasPressedThisFrame)
        {
            return true;
        }

        return index < numpadKeys.Length && Keyboard.current[numpadKeys[index]].wasPressedThisFrame;
    }

    private bool TryResolveStateHash(string stateName, out int stateHash)
    {
        stateHash = Animator.StringToHash("Base Layer." + stateName);
        if (animator.HasState(0, stateHash))
        {
            return true;
        }

        stateHash = Animator.StringToHash(stateName);
        return animator.HasState(0, stateHash);
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

        Vector3 lookTarget;
        Vector3 cameraPosition;
        if (TryGetPreviewBounds(out Bounds bounds))
        {
            lookTarget = bounds.center + new Vector3(0f, bounds.size.y * 0.08f, 0f);
            float distance = Mathf.Max(5.5f, bounds.size.y * 3.0f);
            cameraPosition = lookTarget + new Vector3(0f, bounds.size.y * 0.22f, -distance);
        }
        else
        {
            Vector3 origin = GetPreviewOrigin();
            lookTarget = origin + previewTargetOffset;
            cameraPosition = origin + previewCameraOffset;
        }

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

    private bool TryGetPreviewBounds(out Bounds bounds)
    {
        Transform root = animator != null ? animator.transform : transform;
        SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(false);
        bool hasBounds = false;
        bounds = new Bounds(GetPreviewOrigin(), Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void SnapAnimatorToGround()
    {
        if (!snapPreviewToGround || animator == null || !TryGetPreviewBounds(out Bounds bounds))
        {
            return;
        }

        float deltaY = previewGroundY - bounds.min.y;
        if (Mathf.Abs(deltaY) > 3f)
        {
            return;
        }

        animator.transform.position += new Vector3(0f, deltaY, 0f);
    }

    private void OnGUI()
    {
        if (!showHud || activeDriver != this)
        {
            return;
        }

        GUI.Box(new Rect(16f, 16f, 704f, 236f), "Nightfall Animation Sandbox");
        GUI.Label(new Rect(28f, 40f, 664f, 22f), "Current: " + currentState + " | " + statusText);
        GUI.Label(new Rect(28f, 62f, 664f, 22f), "Preview only. Use buttons or number keys. WASD movement is tested in SampleScene.");
        GUI.Label(new Rect(28f, 84f, 664f, 22f), GetAnimatorDebugText());
        GUI.Label(new Rect(28f, 106f, 664f, 22f), GetCrouchWalkReviewText());

        const float buttonWidth = 128f;
        const float buttonHeight = 26f;
        const float gap = 6f;
        const int columns = 5;

        for (int i = 0; i < stateNames.Length; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Rect rect = new Rect(28f + column * (buttonWidth + gap), 138f + row * (buttonHeight + gap), buttonWidth, buttonHeight);
            string labelText = i < buttonLabels.Length ? buttonLabels[i] : stateNames[i];
            string label = i < 10 ? GetNumberLabel(i) + " " + labelText : labelText;
            if (GUI.Button(rect, label))
            {
                PlayState(i);
            }
        }
    }

    private string GetNumberLabel(int index)
    {
        return index == 9 ? "0" : (index + 1).ToString();
    }

    private string GetAnimatorDebugText()
    {
        if (animator == null)
        {
            return "Animator: NULL";
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        string clipName = clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : "No active clip";
        string controllerName = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "No controller";
        return "Controller: " + controllerName + " | Clip: " + clipName + " | Time: " + stateInfo.normalizedTime.ToString("0.00");
    }

    private string GetCrouchWalkReviewText()
    {
        if (!ShouldUseCrouchWalkDirectionalReview())
        {
            return "Crouch-walk: review disabled - 9 shows safe crouch hold";
        }

        AnimationClip clip = GetCurrentCrouchWalkReviewClip();
        string clipText = clip != null ? ShortClipName(clip.name) : "no crouch-walk clips loaded";
        return "Crouch-walk review: " + clipText + " | Q/E cycles authored directions";
    }

    private void ResolveAnimator()
    {
        Animator preferredAnimator = FindPreferredAnimator();
        if (preferredAnimator != null)
        {
            animator = preferredAnimator;
            previewTarget = preferredAnimator.transform;
        }
    }

    private static Animator FindPreferredAnimator()
    {
        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            Transform root = candidate.transform.root;
            if (root != null && IsPreferredRigRoot(root.name))
            {
                if (!root.gameObject.activeSelf)
                {
                    root.gameObject.SetActive(true);
                }

                EnsureNightfallArmatureName(root);
                return candidate;
            }
        }

        return null;
    }

    private static void ActivatePreferredRigIfPresent()
    {
        _ = FindPreferredAnimator();
    }

    private bool IsRawPreviewDriver()
    {
        Transform root = transform.root;
        string rootName = root != null ? root.name : string.Empty;
        return gameObject.name.Contains(RawPreviewRigName) || rootName.Contains(RawPreviewRigName);
    }

    private int DriverPriority()
    {
        Transform root = transform.root;
        string rootName = root != null ? root.name : string.Empty;
        if (IsPreferredRigRoot(gameObject.name) || IsPreferredRigRoot(rootName))
        {
            return 100;
        }

        return previewCamera != null ? 20 : 10;
    }

    private static bool IsPreferredRigRoot(string objectName)
    {
        return objectName.Contains(PreferredRigName) || objectName.Contains(LegacyPreferredRigName);
    }

    private static void EnsureNightfallArmatureName(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (root.Find(ExpectedArmatureName) != null)
        {
            return;
        }

        Transform legacyArmature = root.Find(LegacyArmatureName);
        if (legacyArmature != null)
        {
            legacyArmature.name = ExpectedArmatureName;
        }
    }

    private void DisableDuplicateDriver()
    {
        showHud = false;
        enabled = false;
    }
}
