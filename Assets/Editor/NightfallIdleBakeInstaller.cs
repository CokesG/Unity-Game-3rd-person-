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
    private const string InstallMarkerPath = "Library/Codex/nightfall_promoted_locomotion_v2.txt";

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

        Undo.RecordObject(controller, "Install promoted Nightfall locomotion");

        foreach (ClipSpec spec in PromotedClips)
        {
            ConfigureBakedClipImporter(spec, targetAvatar);
            AssetDatabase.ImportAsset(spec.BakedPath, ImportAssetOptions.ForceUpdate);

            AnimationClip clip = LoadBakedClip(spec);
            if (clip == null)
            {
                Debug.LogError($"Nightfall promoted locomotion install failed: clip {spec.ClipName} not found in {spec.BakedPath}");
                return;
            }

            AnimatorState state = FindState(controller, spec.StateName);
            if (state == null)
            {
                Debug.LogError($"Nightfall promoted locomotion install failed: state {spec.StateName} not found in PlayerHumanoid.controller");
                return;
            }

            state.motion = clip;
            state.speed = spec.StateSpeed;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        ApplyLiveSceneSettings();

        Directory.CreateDirectory(Path.GetDirectoryName(InstallMarkerPath));
        File.WriteAllText(InstallMarkerPath, string.Join("\n", PromotedClips.Select(spec => spec.ClipName)));
        AssetDatabase.Refresh();

        Debug.Log("Installed promoted Nightfall locomotion clips into PlayerHumanoid.controller.");
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
            SetBool(serializedController, "driveAnimatorStateMachine", true);
            SetBool(serializedController, "walkClipPromoted", true);
            SetBool(serializedController, "runClipPromoted", false);
            SetBool(serializedController, "sprintClipPromoted", false);
            SetBool(serializedController, "jumpClipPromoted", false);
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
            clips[i].loopTime = true;
            clips[i].loopPose = true;
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
        public ClipSpec(string stateName, string bakedPath, string clipName, float stateSpeed)
        {
            StateName = stateName;
            BakedPath = bakedPath;
            ClipName = clipName;
            StateSpeed = stateSpeed;
        }

        public string StateName { get; }
        public string BakedPath { get; }
        public string ClipName { get; }
        public float StateSpeed { get; }
    }
}
