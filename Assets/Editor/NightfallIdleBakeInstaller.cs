using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NightfallIdleBakeInstaller
{
    private const string TargetModelPath = "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx";
    private const string ControllerPath = "Assets/Animations/PlayerHumanoid.controller";
    private const string SandboxControllerPath = "Assets/Animations/NightfallVanguard/Nightfall_GLB_Linked.controller";
    private const string InstallMarkerPath = "Library/Codex/nightfall_promoted_locomotion_v5.txt";

    private static readonly ClipSpec[] PromotedClips =
    {
        new ClipSpec(
            "Idle",
            "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Idle_Baked.fbx",
            "Nightfall_FullQuality_Idle_Baked",
            0.45f),
        new ClipSpec(
            "Walk",
            "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Walk_Baked.fbx",
            "Nightfall_FullQuality_Walk_Baked",
            1.0f),
        new ClipSpec(
            "Run",
            "Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Run_Baked.fbx",
            "Nightfall_FullQuality_Run_Baked",
            1.0f,
            true),
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

            if (PromotedClips.Any(spec => !File.Exists(spec.BakedPath)))
            {
                return;
            }

            Install();
        };
    }

    [MenuItem("Tools/TPS/Nightfall/Install Promoted Locomotion")]
    public static void Install()
    {
        var targetAvatar = LoadAvatar(TargetModelPath);
        if (targetAvatar == null)
        {
            Debug.LogError($"Nightfall promoted locomotion install failed: target Avatar not found at {TargetModelPath}");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"Nightfall promoted locomotion install failed: controller not found at {ControllerPath}");
            return;
        }

        if (!InstallIntoController(controller, targetAvatar))
        {
            return;
        }

        AnimatorController sandboxController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SandboxControllerPath);
        if (sandboxController != null)
        {
            InstallIntoController(sandboxController, targetAvatar);
        }

        EditorUtility.SetDirty(controller);
        if (sandboxController != null)
        {
            EditorUtility.SetDirty(sandboxController);
        }

        AssetDatabase.SaveAssets();
        ApplyLiveSceneSettings();

        Directory.CreateDirectory(Path.GetDirectoryName(InstallMarkerPath));
        File.WriteAllText(InstallMarkerPath, string.Join("\n", PromotedClips.Select(spec => spec.ClipName)));
        AssetDatabase.Refresh();

        Debug.Log("Installed promoted Nightfall locomotion clips into PlayerHumanoid.controller.");
    }

    private static bool InstallIntoController(AnimatorController controller, Avatar targetAvatar)
    {
        Undo.RecordObject(controller, "Install promoted Nightfall locomotion");

        foreach (ClipSpec spec in PromotedClips)
        {
            ConfigureBakedClipImporter(spec, targetAvatar);
            AssetDatabase.ImportAsset(spec.BakedPath, ImportAssetOptions.ForceUpdate);

            AnimationClip clip = LoadBakedClip(spec);
            if (clip == null)
            {
                Debug.LogError($"Nightfall promoted locomotion install failed: clip {spec.ClipName} not found in {spec.BakedPath}");
                return false;
            }

            AnimatorState state = FindOrCreateState(controller, spec.StateName);

            state.motion = clip;
            state.speed = spec.StateSpeed;
        }

        return true;
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
            SetFloat(serializedController, "speedDampTime", 0.14f);
            SetFloat(serializedController, "movementDampTime", 0.1f);
            SetFloat(serializedController, "locomotionCrossFadeTime", 0.18f);
            SetBool(serializedController, "driveAnimatorStateMachine", true);
            SetBool(serializedController, "walkClipPromoted", true);
            SetBool(serializedController, "runClipPromoted", true);
            SetBool(serializedController, "sprintClipPromoted", false);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(animationController);
        }

        GameObject fullQualityVisual = GameObject.Find("NightfallVanguard_FullQuality");
        if (fullQualityVisual != null)
        {
            fullQualityVisual.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            EditorUtility.SetDirty(fullQualityVisual.transform);
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

    private static Avatar LoadAvatar(string modelPath)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault();
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
        Vector3 position = StatePosition(stateName);
        AnimatorState created = stateMachine.AddState(stateName, position);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(created);
        return created;
    }

    private static Vector3 StatePosition(string stateName)
    {
        switch (stateName)
        {
            case "Run":
                return new Vector3(310f, 40f, 0f);
            case "Jump Start":
                return new Vector3(500f, -80f, 0f);
            case "Falling / In Air":
                return new Vector3(500f, 20f, 0f);
            case "Landing":
                return new Vector3(500f, 120f, 0f);
            default:
                return new Vector3(500f, 220f, 0f);
        }
    }

    private static void ConfigureBakedClipImporter(ClipSpec spec, Avatar targetAvatar)
    {
        var importer = AssetImporter.GetAtPath(spec.BakedPath) as ModelImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(spec.BakedPath, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(spec.BakedPath) as ModelImporter;
        }

        if (importer == null)
        {
            Debug.LogError($"Nightfall promoted locomotion install failed: ModelImporter not available for {spec.BakedPath}");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = targetAvatar;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].name = spec.ClipName;
            clips[i].loopTime = spec.LoopTime;
            clips[i].loopPose = spec.LoopTime;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
            clips[i].keepOriginalOrientation = false;
            clips[i].keepOriginalPositionY = false;
            clips[i].keepOriginalPositionXZ = false;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip LoadBakedClip(ClipSpec spec)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(spec.BakedPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            .OrderByDescending(clip => clip.name == spec.ClipName)
            .ThenByDescending(clip => clip.name.Contains(spec.StateName))
            .FirstOrDefault();
    }

    private sealed class ClipSpec
    {
        public ClipSpec(string stateName, string bakedPath, string clipName, float stateSpeed, bool loopTime = true)
        {
            StateName = stateName;
            BakedPath = bakedPath;
            ClipName = clipName;
            StateSpeed = stateSpeed;
            LoopTime = loopTime;
        }

        public string StateName { get; }
        public string BakedPath { get; }
        public string ClipName { get; }
        public float StateSpeed { get; }
        public bool LoopTime { get; }
    }
}
