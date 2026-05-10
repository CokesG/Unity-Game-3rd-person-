using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NightfallMixamoInstaller
{
    private const string TargetModelPath = "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx";
    private const string ControllerPath = "Assets/Animations/PlayerHumanoid.controller";
    private const string SandboxControllerPath = "Assets/Animations/NightfallVanguard/Nightfall_GLB_Linked.controller";
    private const string InstallMarkerPath = "Library/Codex/nightfall_mixamo_v3.txt";

    private static readonly ClipSpec[] MixamoClips =
    {
        new ClipSpec(
            "Jump Start",
            "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_JumpSafe_Baked.fbx",
            "Nightfall_FullQuality_JumpSafe_Baked",
            1f,
            17f,
            false,
            1.15f),
        new ClipSpec(
            "Crouch Idle",
            "Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_CrouchIdle_Baked.fbx",
            "Nightfall_Mixamo_CrouchIdle_Baked",
            1f,
            76f,
            true,
            1.0f),
        new ClipSpec(
            "Crouch Walk",
            "Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_CrouchWalk_Baked.fbx",
            "Nightfall_Mixamo_CrouchWalk_Baked",
            1f,
            36f,
            true,
            1.0f),
        new ClipSpec(
            "Stand Up",
            "Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_StandUp_Baked.fbx",
            "Nightfall_Mixamo_StandUp_Baked",
            1f,
            20f,
            false,
            1.15f),
    };

    [InitializeOnLoadMethod]
    private static void InstallWhenReady()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(InstallMarkerPath))
            {
                return;
            }

            if (MixamoClips.Any(spec => !File.Exists(spec.AssetPath)))
            {
                return;
            }

            Install();
        };
    }

    [MenuItem("Tools/TPS/Nightfall/Install Mixamo Animation Clips")]
    public static void Install()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"Nightfall Mixamo install failed: controller not found at {ControllerPath}");
            return;
        }

        Avatar targetAvatar = LoadAvatar(TargetModelPath);
        if (targetAvatar == null)
        {
            Debug.LogError($"Nightfall Mixamo install failed: target Avatar not found at {TargetModelPath}");
            return;
        }

        ConfigureMixamoImporters(targetAvatar);

        if (!InstallIntoController(controller))
        {
            return;
        }

        AnimatorController sandboxController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SandboxControllerPath);
        if (sandboxController != null)
        {
            InstallIntoController(sandboxController);
            EditorUtility.SetDirty(sandboxController);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        ApplyLiveSceneSettings();

        Directory.CreateDirectory(Path.GetDirectoryName(InstallMarkerPath));
        File.WriteAllText(InstallMarkerPath, string.Join("\n", MixamoClips.Select(spec => spec.ClipName)));
        AssetDatabase.Refresh();

        Debug.Log("Installed Mixamo jump and crouch clips into Nightfall animation controllers.");
    }

    private static void ConfigureMixamoImporters(Avatar targetAvatar)
    {
        foreach (IGrouping<string, ClipSpec> group in MixamoClips.GroupBy(spec => spec.AssetPath))
        {
            var importer = AssetImporter.GetAtPath(group.Key) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(group.Key, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(group.Key) as ModelImporter;
            }

            if (importer == null)
            {
                Debug.LogError($"Nightfall Mixamo install failed: ModelImporter not available for {group.Key}");
                continue;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = targetAvatar;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.SaveAndReimport();

            var clips = new List<ModelImporterClipAnimation>();
            foreach (ClipSpec spec in group)
            {
                var clip = new ModelImporterClipAnimation
                {
                    name = spec.ClipName,
                    takeName = "mixamo.com",
                    firstFrame = spec.FirstFrame,
                    lastFrame = spec.LastFrame,
                    loopTime = spec.LoopTime,
                    loopPose = spec.LoopTime,
                    lockRootRotation = true,
                    lockRootHeightY = true,
                    lockRootPositionXZ = true,
                    keepOriginalOrientation = false,
                    keepOriginalPositionY = false,
                    keepOriginalPositionXZ = false,
                    heightFromFeet = true,
                    wrapMode = spec.LoopTime ? WrapMode.Loop : WrapMode.Once,
                };
                clips.Add(clip);
            }

            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
        }
    }

    private static Avatar LoadAvatar(string modelPath)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault();
    }

    private static bool InstallIntoController(AnimatorController controller)
    {
        Undo.RecordObject(controller, "Install Nightfall Mixamo clips");

        foreach (ClipSpec spec in MixamoClips)
        {
            AnimationClip clip = LoadClip(spec);
            if (clip == null)
            {
                Debug.LogError($"Nightfall Mixamo install failed: clip {spec.ClipName} not found in {spec.AssetPath}");
                return false;
            }

            AnimatorState state = FindOrCreateState(controller, spec.StateName);
            state.motion = clip;
            state.speed = spec.StateSpeed;
            EditorUtility.SetDirty(state);
        }

        return true;
    }

    private static AnimationClip LoadClip(ClipSpec spec)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(spec.AssetPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            .FirstOrDefault(clip => clip.name == spec.ClipName);
    }

    private static AnimatorState FindState(AnimatorController controller, string stateName)
    {
        return controller.layers
            .SelectMany(layer => layer.stateMachine.states)
            .Select(childState => childState.state)
            .FirstOrDefault(state => state.name == stateName);
    }

    private static AnimatorState FindOrCreateState(AnimatorController controller, string stateName)
    {
        AnimatorState existing = FindState(controller, stateName);
        if (existing != null)
        {
            return existing;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState created = stateMachine.AddState(stateName, StatePosition(stateName));
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(created);
        return created;
    }

    private static Vector3 StatePosition(string stateName)
    {
        switch (stateName)
        {
            case "Jump Start":
                return new Vector3(500f, -120f, 0f);
            case "Falling / In Air":
                return new Vector3(500f, -40f, 0f);
            case "Landing":
                return new Vector3(500f, 40f, 0f);
            case "Running Jump":
                return new Vector3(500f, 120f, 0f);
            case "Crouch Idle":
                return new Vector3(740f, -80f, 0f);
            case "Crouch Walk":
                return new Vector3(740f, 0f, 0f);
            case "Stand Up":
                return new Vector3(740f, 80f, 0f);
            default:
                return new Vector3(740f, 160f, 0f);
        }
    }

    private static void ApplyLiveSceneSettings()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player != null && player.TryGetComponent(out PlayerAnimationController animationController))
        {
            var serializedController = new SerializedObject(animationController);
            SetBool(serializedController, "jumpClipPromoted", true);
            SetBool(serializedController, "airClipPromoted", false);
            SetBool(serializedController, "landClipPromoted", false);
            SetBool(serializedController, "crouchIdleClipPromoted", true);
            SetBool(serializedController, "crouchWalkClipPromoted", true);
            SetBool(serializedController, "standUpClipPromoted", true);
            SetFloat(serializedController, "landingStateDuration", 0.1f);
            SetFloat(serializedController, "standUpStateDuration", 0.28f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(animationController);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private sealed class ClipSpec
    {
        public ClipSpec(
            string stateName,
            string assetPath,
            string clipName,
            float firstFrame,
            float lastFrame,
            bool loopTime,
            float stateSpeed)
        {
            StateName = stateName;
            AssetPath = assetPath;
            ClipName = clipName;
            FirstFrame = firstFrame;
            LastFrame = lastFrame;
            LoopTime = loopTime;
            StateSpeed = stateSpeed;
        }

        public string StateName { get; }
        public string AssetPath { get; }
        public string ClipName { get; }
        public float FirstFrame { get; }
        public float LastFrame { get; }
        public bool LoopTime { get; }
        public float StateSpeed { get; }
    }
}
