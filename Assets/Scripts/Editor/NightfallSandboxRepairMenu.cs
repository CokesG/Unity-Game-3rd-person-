using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NightfallSandboxRepairMenu
{
    private const string ScenePath = "Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity";
    private const string PreferredRigName = "NightfallVanguard_ModelOnly_FullQuality_NoAnimations";
    private const string LegacyPreferredRigName = "NightfallVanguard_LinkedAnimationRig";
    private const string RawRigName = "NightfallVanguard_RawAnimationPreviewRig";
    private const string ExpectedArmatureName = "NightfallVanguard_FullQuality_Armature";
    private const string LegacyArmatureName = "Armature";

    [MenuItem("Tools/TPS/Nightfall/Repair Animation Sandbox")]
    public static void RepairAnimationSandbox()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Repair Animation Sandbox", "Exit Play Mode first, then run this again.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Camera sandboxCamera = FindSceneObjectByName<Camera>(scene, "SandboxCamera");
        Animator preferredAnimator = null;
        int enabledDrivers = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool isPreferredRig = IsPreferredRigRoot(root.name);
            bool isRawRig = root.name.Contains(RawRigName);

            if (isPreferredRig)
            {
                root.SetActive(true);
                EnsureNightfallArmatureName(root.transform);
                preferredAnimator = root.GetComponentInChildren<Animator>(true);
            }
            else if (isRawRig)
            {
                root.SetActive(false);
            }
        }

        if (preferredAnimator == null)
        {
            Debug.LogError("Nightfall sandbox repair failed: preferred rig Animator was not found.");
            return;
        }

        NightfallAnimationSandboxDriver[] drivers = Resources.FindObjectsOfTypeAll<NightfallAnimationSandboxDriver>();
        foreach (NightfallAnimationSandboxDriver driver in drivers)
        {
            if (driver.gameObject.scene != scene)
            {
                continue;
            }

            SerializedObject serializedDriver = new SerializedObject(driver);
            bool isPreferredDriver = IsPreferredRigRoot(driver.transform.root.name);

            serializedDriver.FindProperty("animator").objectReferenceValue = preferredAnimator;
            serializedDriver.FindProperty("previewCamera").objectReferenceValue = sandboxCamera;
            serializedDriver.FindProperty("previewTarget").objectReferenceValue = preferredAnimator.transform;
            serializedDriver.FindProperty("showHud").boolValue = isPreferredDriver;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            driver.enabled = isPreferredDriver;
            if (isPreferredDriver)
            {
                enabledDrivers++;
            }
        }

        EditorUtility.SetDirty(preferredAnimator);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Nightfall sandbox repaired. Enabled drivers: {enabledDrivers}. Active rig: {preferredAnimator.transform.root.name}.");
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

    private static T FindSceneObjectByName<T>(Scene scene, string objectName) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component.gameObject.name == objectName)
                {
                    return component;
                }
            }
        }

        return null;
    }
}
